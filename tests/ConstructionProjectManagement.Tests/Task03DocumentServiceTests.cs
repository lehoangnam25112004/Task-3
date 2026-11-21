using System.Text;
using System.IO.Compression;
using ConstructionProjectManagement.Data;
using ConstructionProjectManagement.Helpers;
using ConstructionProjectManagement.Models;
using ConstructionProjectManagement.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Task = System.Threading.Tasks.Task;

namespace ConstructionProjectManagement.Tests;

public sealed class Task03DocumentServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly string storageRoot = Path.Combine(Path.GetTempPath(), $"task03-docs-{Guid.NewGuid():N}");
    private ApplicationDbContext db = null!;

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await db.DisposeAsync();
        await connection.DisposeAsync();
        if (Directory.Exists(storageRoot))
            Directory.Delete(storageRoot, recursive: true);
    }

    [Fact]
    public void FileValidationHelper_RejectsInvalidMagicBytesAndOversize()
    {
        var fakePdf = CreateFile("NOT-A-PDF"u8.ToArray(), "test.pdf");
        var signature = FileValidationHelper.Validate(fakePdf);
        Assert.False(signature.Valid);

        var largeBytes = new byte[25 * 1024 * 1024 + 1];
        var largeFile = CreateFile(largeBytes, "large.pdf");
        var size = FileValidationHelper.Validate(largeFile);
        Assert.False(size.Valid);
    }

    [Fact]
    public void FileValidationHelper_AcceptsLegacyOfficeAndValidPresentationFiles()
    {
        var legacyOfficeSignature = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
        Assert.True(FileValidationHelper.Validate(
            CreateFile(legacyOfficeSignature, "legacy.doc")).Valid);

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            archive.CreateEntry("[Content_Types].xml");
            archive.CreateEntry("ppt/presentation.xml");
        }
        Assert.True(FileValidationHelper.Validate(
            CreateFile(stream.ToArray(), "presentation.pptx")).Valid);
        Assert.False(FileValidationHelper.Validate(
            CreateFile(new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00 }, "fake.pptx")).Valid);
    }

    [Fact]
    public async Task Visibility_RequiresActiveMembershipAndApprovedStatusForWorker()
    {
        var project = await CreateProjectAsync();
        var manager = await CreateUserAsync("manager", "Project Manager", project);
        var worker = await CreateUserAsync("worker", "Worker", project);
        var approved = await CreateDocumentAsync(manager.Id, project.Id, "Other", "Approved document");
        var draft = await CreateDocumentAsync(manager.Id, project.Id, "Legal", "Pending document");
        var service = CreateService();

        var workerDocuments = await service.GetVisibleAsync(worker.Id, false);
        Assert.Contains(workerDocuments, item => item.Id == approved.Id);
        Assert.DoesNotContain(workerDocuments, item => item.Id == draft.Id);

        var workerCreate = await service.CreateAsync(
            project.Id, null, null, await CategoryIdAsync("Other"), false,
            "Worker upload", null, CreateFile(PdfBytes(), "worker.pdf"), worker.Id, false);
        Assert.False(workerCreate.Succeeded);

        var membership = await db.ProjectMembers.SingleAsync(
            item => item.ProjectId == project.Id && item.UserId == worker.Id);
        membership.IsActive = false;
        await db.SaveChangesAsync();
        Assert.Empty(await service.GetVisibleAsync(worker.Id, false));
    }

    [Fact]
    public async Task ClientRepresentative_RequiresActiveMembershipAndMatchingOrganization()
    {
        var project = await CreateProjectAsync(clientName: "Owner Organization");
        var manager = await CreateUserAsync("manager", "Project Manager", project);
        var matched = await CreateUserAsync(
            "client-matched", "Client Representative", project, "Owner Organization");
        var mismatched = await CreateUserAsync(
            "client-mismatched", "Client Representative", project, "Different Organization");
        var approved = await CreateDocumentAsync(manager.Id, project.Id, "Other", "Approved document");
        var pending = await CreateDocumentAsync(manager.Id, project.Id, "Legal", "Pending document");
        var service = CreateService();

        var matchedDocuments = await service.GetVisibleAsync(matched.Id, false);
        Assert.Contains(matchedDocuments, item => item.Id == approved.Id);
        Assert.DoesNotContain(matchedDocuments, item => item.Id == pending.Id);
        Assert.Empty(await service.GetVisibleAsync(mismatched.Id, false));
    }

    [Fact]
    public async Task AdminCanViewButCannotCreateOrEditBusinessDocuments()
    {
        var project = await CreateProjectAsync();
        var manager = await CreateUserAsync("manager", "Project Manager", project);
        var admin = await CreateUserAsync("admin", "Admin", project);
        var document = await CreateDocumentAsync(manager.Id, project.Id, "Design");
        var service = CreateService();

        var create = await service.CreateAsync(
            project.Id, null, null, await CategoryIdAsync("Design"), false,
            "Admin upload", null, CreateFile(PdfBytes(), "admin.pdf"), admin.Id, true);
        Assert.False(create.Succeeded);

        var permissions = await service.GetPermissionsAsync(document.Id, admin.Id, true);
        Assert.False(permissions.CanEdit);
        Assert.False(permissions.CanReview);
        Assert.False(permissions.CanApprove);
        Assert.True(permissions.CanArchive);
    }

    [Fact]
    public async Task SearchFiltersRespectUploaderAndUploadDateRange()
    {
        var project = await CreateProjectAsync();
        var manager = await CreateUserAsync("manager", "Project Manager", project);
        var document = await CreateDocumentAsync(manager.Id, project.Id, "Other");
        var service = CreateService();

        var page = await service.GetPageAsync(manager.Id, false, new DocumentSearchOptions
        {
            UploadedById = manager.Id,
            UploadedFrom = document.UploadedAt.Date,
            UploadedTo = document.UploadedAt.Date
        });

        Assert.Contains(page.Documents, item => item.Id == document.Id);
        Assert.Equal(1, page.TotalItems);
    }

    [Fact]
    public async Task EditLock_ExcludesOtherUsersAndCanBeTakenAfterExpiry()
    {
        var project = await CreateProjectAsync();
        var firstUser = await CreateUserAsync("manager-one", "Project Manager", project);
        var secondUser = await CreateUserAsync("manager-two", "Project Manager", project);
        var document = await CreateDocumentAsync(firstUser.Id, project.Id, "Other");
        var service = CreateService();

        var first = await service.AcquireLockAsync(document.Id, firstUser.Id, false);
        Assert.True(first.Succeeded);
        var second = await service.AcquireLockAsync(document.Id, secondUser.Id, false);
        Assert.False(second.Succeeded);

        var editLock = await db.DocumentEditLocks.SingleAsync(item => item.DocumentId == document.Id);
        Assert.Equal(firstUser.Id, editLock.LockedByUserId);
        Assert.Equal(TimeSpan.FromMinutes(15), editLock.ExpiresAt - editLock.AcquiredAt);
        editLock.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var afterExpiry = await service.AcquireLockAsync(document.Id, secondUser.Id, false);
        Assert.True(afterExpiry.Succeeded);
        Assert.Equal(secondUser.Id, editLock.LockedByUserId);
    }

    [Fact]
    public async Task MetadataUpdateDoesNotCreateVersionAndFileUploadDoes()
    {
        var project = await CreateProjectAsync();
        var manager = await CreateUserAsync("manager", "Project Manager", project);
        var document = await CreateDocumentAsync(manager.Id, project.Id, "Other");
        var service = CreateService();
        var versionsBefore = await db.DocumentVersions.CountAsync(item => item.DocumentId == document.Id);

        Assert.True((await service.AcquireLockAsync(document.Id, manager.Id, false)).Succeeded);
        var updatedDocument = new ProjectDocument
        {
            Id = document.Id,
            Title = "Metadata-only update",
            Description = "Metadata changed",
            CategoryId = document.CategoryId,
            ProjectId = project.Id,
            DocumentCode = "",
            FileName = "",
            StoredFileName = "",
            ContentType = "",
            Category = ""
        };
        var update = await service.UpdateMetadataAsync(updatedDocument, manager.Id, false);
        Assert.True(update.Succeeded, update.Error);
        Assert.Equal(versionsBefore, await db.DocumentVersions.CountAsync(item => item.DocumentId == document.Id));

        Assert.True((await service.AcquireLockAsync(document.Id, manager.Id, false)).Succeeded);
        var upload = await service.UploadVersionAsync(
            document.Id, CreateFile(PdfBytes("revision"), "revision.pdf"),
            "Bản sửa đổi", manager.Id, false);
        Assert.True(upload.Succeeded, upload.Error);
        Assert.Equal(versionsBefore + 1, await db.DocumentVersions.CountAsync(item => item.DocumentId == document.Id));
        Assert.Equal(2, (await db.ProjectDocuments.SingleAsync(item => item.Id == document.Id)).CurrentVersionNumber);
    }

    [Fact]
    public async Task TwoStepApprovalRejectsSelfApprovalAndRequiresRejectionReason()
    {
        var project = await CreateProjectAsync();
        var creator = await CreateUserAsync("creator", "Project Manager", project);
        var reviewer = await CreateUserAsync("reviewer", "Supervising Engineer", project);
        var approver = await CreateUserAsync("approver", "Project Director", project);
        var document = await CreateDocumentAsync(creator.Id, project.Id, "Design");
        var service = CreateService();

        var submit = await service.SubmitAsync(document.Id, creator.Id, false);
        Assert.True(submit.Succeeded, submit.Error);
        Assert.False((await service.AcquireLockAsync(document.Id, creator.Id, false)).Succeeded);
        var selfReview = await service.ReviewAsync(document.Id, creator.Id, false);
        Assert.False(selfReview.Succeeded);
        var review = await service.ReviewAsync(document.Id, reviewer.Id, false);
        Assert.True(review.Succeeded, review.Error);
        var selfApprove = await service.ApproveAsync(document.Id, creator.Id, false);
        Assert.False(selfApprove.Succeeded);
        var approve = await service.ApproveAsync(document.Id, approver.Id, false);
        Assert.True(approve.Succeeded, approve.Error);
        Assert.Equal(
            DocumentApprovalStatus.Approved,
            (await db.ProjectDocuments.SingleAsync(item => item.Id == document.Id)).ApprovalStatus);
        Assert.Equal(3, await db.DocumentApprovalLogs.CountAsync(item => item.DocumentId == document.Id));

        var rejected = await service.RejectAsync(document.Id, " ", approver.Id, false);
        Assert.False(rejected.Succeeded);
    }

    [Fact]
    public async Task ArchiveKeepsHistoryAndCanBeRestoredByProjectManager()
    {
        var project = await CreateProjectAsync();
        var manager = await CreateUserAsync("manager", "Project Manager", project);
        var document = await CreateDocumentAsync(manager.Id, project.Id, "Other");
        var service = CreateService();

        Assert.True((await service.ArchiveAsync(document.Id, "Archive test", manager.Id, false)).Succeeded);
        Assert.Empty(await service.GetVisibleAsync(manager.Id, false));
        var archived = await service.GetByIdAsync(document.Id, manager.Id, false);
        Assert.NotNull(archived);
        Assert.True(archived.IsArchived);
        Assert.NotEmpty(archived.AuditLogs);

        Assert.True((await service.RestoreAsync(document.Id, manager.Id, false)).Succeeded);
        var restored = await service.GetByIdAsync(document.Id, manager.Id, false);
        Assert.NotNull(restored);
        Assert.False(restored.IsArchived);
        Assert.Contains(restored.AuditLogs, item => item.Action == "Restored");
    }

    [Fact]
    public async Task SpecialistsCanViewActiveDocumentsButNotArchivedDocuments()
    {
        var project = await CreateProjectAsync();
        var manager = await CreateUserAsync("manager", "Project Manager", project);
        var engineer = await CreateUserAsync("engineer", "Supervising Engineer", project);
        var document = await CreateDocumentAsync(manager.Id, project.Id, "Design");
        var service = CreateService();

        Assert.NotNull(await service.GetByIdAsync(document.Id, engineer.Id, false));
        Assert.True((await service.ArchiveAsync(
            document.Id, "Archive test", manager.Id, false)).Succeeded);

        Assert.Null(await service.GetByIdAsync(document.Id, engineer.Id, false));
    }

    [Fact]
    public async Task AuditAndApprovalHistoryCannotBeModifiedOrDeleted()
    {
        var project = await CreateProjectAsync();
        var manager = await CreateUserAsync("manager", "Project Manager", project);
        var document = await CreateDocumentAsync(manager.Id, project.Id, "Other");
        var audit = await db.DocumentAuditLogs.SingleAsync(item => item.DocumentId == document.Id);
        db.DocumentAuditLogs.Remove(audit);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Contains("Không được sửa hoặc xóa lịch sử", error.Message);
    }

    private ProjectDocumentService CreateService()
        => new(db, new TestWebHostEnvironment { ContentRootPath = storageRoot });

    private async Task<Project> CreateProjectAsync(string clientName = "Test Owner")
    {
        var project = new Project
        {
            ProjectCode = $"DOC-{Guid.NewGuid():N}"[..12],
            Name = "Dự án kiểm thử tài liệu",
            ClientName = clientName,
            StartDate = DateTime.UtcNow.Date,
            ExpectedEndDate = DateTime.UtcNow.Date.AddDays(30),
            Status = ProjectStatus.InProgress
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    private async Task<ApplicationUser> CreateUserAsync(
        string id,
        string role,
        Project project,
        string? organizationName = null)
    {
        var user = new ApplicationUser
        {
            Id = id,
            UserName = id,
            NormalizedUserName = id.ToUpperInvariant(),
            Email = $"{id}@test.invalid",
            NormalizedEmail = $"{id}@test.invalid".ToUpperInvariant(),
            FullName = id,
            OrganizationName = organizationName,
            IsActive = true
        };
        db.Users.Add(user);
        var identityRole = await db.Roles.SingleOrDefaultAsync(item => item.Name == role);
        if (identityRole is null)
        {
            identityRole = new IdentityRole
            {
                Id = $"role-{role.Replace(" ", string.Empty, StringComparison.Ordinal)}",
                Name = role,
                NormalizedName = role.ToUpperInvariant()
            };
            db.Roles.Add(identityRole);
        }
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = id, RoleId = identityRole.Id });
        db.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = project.Id,
            UserId = id,
            AssignmentRole = role,
            IsActive = true
        });
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<ProjectDocument> CreateDocumentAsync(
        string userId,
        int projectId,
        string categoryCode,
        string title = "Test document")
    {
        var categoryId = await CategoryIdAsync(categoryCode);
        var result = await CreateService().CreateAsync(
            projectId,
            null,
            null,
            categoryId,
            false,
            title,
            "Test description",
            CreateFile(PdfBytes(), $"{title.Replace(' ', '-')}.pdf"),
            userId,
            false);
        Assert.True(result.Succeeded, result.Error);
        return result.Document!;
    }

    private Task<int> CategoryIdAsync(string code)
        => db.DocumentCategories.Where(item => item.Code == code)
            .Select(item => item.Id)
            .SingleAsync();

    private static byte[] PdfBytes(string suffix = "test")
        => Encoding.ASCII.GetBytes($"%PDF-1.7\n{suffix}");

    private static FormFile CreateFile(byte[] bytes, string fileName)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName);
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
