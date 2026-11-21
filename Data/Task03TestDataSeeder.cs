using ConstructionProjectManagement.Models;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace ConstructionProjectManagement.Data;

public static class Task03TestDataSeeder
{
    private const string FakePrefix = "[FAKE-TASK03]";

    public static bool IsEnabled(IConfiguration configuration)
        => bool.TryParse(configuration["CPMS_SEED_TASK03_TEST_DATA"], out var enabled) && enabled;

    public static async Task SeedAsync(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        foreach (var roleName in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole(roleName));
        }

        var users = new[]
        {
            ("fake.task03.admin@example.local", "Admin", "Quản trị"),
            ("fake.task03.director@example.local", "Project Director", "Giám đốc dự án"),
            ("fake.task03.manager@example.local", "Project Manager", "Quản lý dự án"),
            ("fake.task03.site@example.local", "Site Manager", "Quản lý hiện trường"),
            ("fake.task03.engineer@example.local", "Supervising Engineer", "Giám sát"),
            ("fake.task03.materials@example.local", "Materials Staff", "Vật tư"),
            ("fake.task03.worker@example.local", "Worker", "Công nhân"),
            ("fake.task03.client.match@example.local", "Client Representative", "Chủ đầu tư khớp"),
            ("fake.task03.client.mismatch@example.local", "Client Representative", "Chủ đầu tư lệch")
        };
        var createdUsers = new Dictionary<string, ApplicationUser>();
        foreach (var (email, role, name) in users)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = $"{FakePrefix} {name}",
                    IsActive = true,
                    OrganizationName = email.Contains("client.match")
                        ? "FAKE Chủ đầu tư A"
                        : email.Contains("client.mismatch")
                            ? "Tổ chức không khớp"
                            : null
                };
                var result = await userManager.CreateAsync(user, "Fake@Task03#123");
                if (!result.Succeeded)
                    throw new InvalidOperationException("Không thể tạo user fake TASK-03: " +
                        string.Join("; ", result.Errors.Select(error => error.Description)));
            }
            else
            {
                user.FullName = $"{FakePrefix} {name}";
                user.OrganizationName = email.Contains("client.match")
                    ? "FAKE Chủ đầu tư A"
                    : email.Contains("client.mismatch")
                        ? "Tổ chức không khớp"
                        : null;
                user.IsActive = true;
                await userManager.UpdateAsync(user);
                await userManager.RemovePasswordAsync(user);
                await userManager.AddPasswordAsync(user, "Fake@Task03#123");
            }
            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
            createdUsers[email] = user;
        }

        var activeProject = await dbContext.Projects.FirstOrDefaultAsync(
            item => item.ProjectCode == "FAKE-TASK03-P01", cancellationToken);
        if (activeProject is null)
        {
            activeProject = new Project
            {
                ProjectCode = "FAKE-TASK03-P01",
                Name = $"{FakePrefix} Dự án thi công A",
                Location = "Hà Nội",
                ClientName = "FAKE Chủ đầu tư A",
                StartDate = DateTime.UtcNow.Date.AddMonths(-2),
                ExpectedEndDate = DateTime.UtcNow.Date.AddMonths(8),
                Budget = 2_500_000_000m,
                Status = ProjectStatus.InProgress
            };
            dbContext.Projects.Add(activeProject);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var secondProject = await dbContext.Projects.FirstOrDefaultAsync(
            item => item.ProjectCode == "FAKE-TASK03-P02", cancellationToken);
        if (secondProject is null)
        {
            secondProject = new Project
            {
                ProjectCode = "FAKE-TASK03-P02",
                Name = $"{FakePrefix} Dự án liên kết B",
                Location = "Đà Nẵng",
                ClientName = "FAKE Chủ đầu tư B",
                StartDate = DateTime.UtcNow.Date,
                ExpectedEndDate = DateTime.UtcNow.Date.AddMonths(10),
                Budget = 1_800_000_000m,
                Status = ProjectStatus.InProgress
            };
            dbContext.Projects.Add(secondProject);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var (email, _, _) in users.Where(user => user.Item2 != "Admin"))
        {
            var user = createdUsers[email];
            foreach (var project in new[] { activeProject, secondProject })
            {
                var exists = await dbContext.ProjectMembers.AnyAsync(
                    item => item.ProjectId == project.Id && item.UserId == user.Id,
                    cancellationToken);
                if (!exists)
                    dbContext.ProjectMembers.Add(new ProjectMember
                    {
                        ProjectId = project.Id,
                        UserId = user.Id,
                        AssignmentRole = email.Contains("client") ? "ClientRepresentative" : "TASK03",
                        IsActive = true
                    });
            }
        }

        var inactiveWorker = createdUsers["fake.task03.worker@example.local"];
        var oldMember = await dbContext.ProjectMembers.FirstOrDefaultAsync(
            item => item.ProjectId == secondProject.Id && item.UserId == inactiveWorker.Id,
            cancellationToken);
        if (oldMember is not null)
            oldMember.IsActive = false;

        var inactiveCategory = await dbContext.DocumentCategories.FirstOrDefaultAsync(
            item => item.Code == "FAKE-TASK03-LEGACY", cancellationToken);
        if (inactiveCategory is null)
        {
            inactiveCategory = new DocumentCategory
            {
                Code = "FAKE-TASK03-LEGACY",
                Name = "Danh mục giả lập ngừng kích hoạt",
                Description = FakePrefix,
                ApprovalLevels = 1,
                IsActive = false,
                DisplayOrder = 99
            };
            dbContext.DocumentCategories.Add(inactiveCategory);
        }

        var clientParty = await EnsurePartyAsync(dbContext, "FAKE-CLIENT-A", "FAKE Chủ đầu tư A", ContractPartyRole.Client, cancellationToken);
        var contractorParty = await EnsurePartyAsync(dbContext, "FAKE-CONTRACTOR", "FAKE Nhà thầu XYZ", ContractPartyRole.Contractor, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var director = createdUsers["fake.task03.director@example.local"];
        var manager = createdUsers["fake.task03.manager@example.local"];
        var now = DateTime.UtcNow;
        var statusList = Enum.GetValues<ContractStatus>();
        var statusNames = new Dictionary<ContractStatus, string>
        {
            [ContractStatus.Draft] = "Nháp",
            [ContractStatus.PendingApproval] = "Chờ duyệt",
            [ContractStatus.InProgress] = "Đang thực hiện",
            [ContractStatus.Suspended] = "Tạm dừng",
            [ContractStatus.Completed] = "Hoàn thành",
            [ContractStatus.Terminated] = "Chấm dứt",
            [ContractStatus.Liquidated] = "Thanh lý"
        };
        var statuses = statusNames.Keys.ToList();

        foreach (var status in statuses)
        {
            var number = $"HD-FAKE-TASK03-{(int)status + 1:D2}";
            var contract = await dbContext.Contracts.FirstOrDefaultAsync(
                item => item.ContractNumber == number, cancellationToken);
            if (contract is not null) continue;
            contract = new Contract
            {
                ContractNumber = number,
                Title = $"{FakePrefix} Hợp đồng {statusNames[status]}",
                ProjectId = activeProject.Id,
                ContractType = status == ContractStatus.Draft ? ContractType.Construction : ContractType.MaterialSupply,
                ClientPartyId = clientParty.Id,
                ContractorPartyId = contractorParty.Id,
                ClientName = clientParty.Name,
                ContractorName = contractorParty.Name,
                OriginalValue = 100_000_000m + (int)status * 10_000_000m,
                CurrentValue = 100_000_000m + (int)status * 10_000_000m,
                ContractValue = 100_000_000m + (int)status * 10_000_000m,
                Currency = "VND",
                ScopeOfWork = $"{FakePrefix} Phạm vi thử nghiệm",
                SignedDate = now.Date.AddMonths(-1),
                EffectiveDate = status == ContractStatus.InProgress
                    ? now.Date.AddDays(-5)
                    : now.Date.AddDays((int)status),
                EndDate = status == ContractStatus.InProgress
                    ? now.Date.AddDays(20)
                    : now.Date.AddDays((int)status + 45),
                ProgressPercentage = (int)status * 10,
                Status = status,
                ApprovalStep = status is ContractStatus.PendingApproval ? 1 : status == ContractStatus.InProgress ? 3 : 0,
                CreatedById = status == ContractStatus.InProgress ? manager.Id : director.Id,
                IsArchived = status == ContractStatus.Liquidated
            };
            dbContext.Contracts.Add(contract);
            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.ContractProjects.Add(new ContractProject
            {
                ContractId = contract.Id,
                ProjectId = activeProject.Id,
                AllocatedValue = contract.CurrentValue
            });
            if (status is ContractStatus.InProgress or ContractStatus.Suspended or ContractStatus.Completed)
            {
                dbContext.ContractProjects.Add(new ContractProject
                {
                    ContractId = contract.Id,
                    ProjectId = secondProject.Id,
                    AllocatedValue = contract.CurrentValue * 0.25m,
                    ScopeNote = FakePrefix
                });
            }

            var amendment = new ContractAmendment
            {
                ContractId = contract.Id,
                AmendmentNumber = $"{contract.ContractNumber}-PL01",
                Title = $"{FakePrefix} Phụ lục điều chỉnh",
                SignedDate = now.Date,
                EffectiveDate = now.Date,
                ChangeDescription = "Tăng giá trị và gia hạn thử nghiệm",
                ValueAdjustment = status == ContractStatus.Terminated ? -5_000_000m : 5_000_000m,
                NewExpirationDate = now.Date.AddDays(60),
                ApprovalStatus = status == ContractStatus.InProgress
                    ? AmendmentApprovalStatus.Approved
                    : AmendmentApprovalStatus.Draft,
                CreatedById = manager.Id,
                IsApplied = status == ContractStatus.InProgress
            };
            dbContext.ContractAmendments.Add(amendment);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (amendment.IsApplied)
            {
                dbContext.ContractValueHistories.Add(new ContractValueHistory
                {
                    ContractId = contract.Id,
                    AmendmentId = amendment.Id,
                    PreviousValue = contract.CurrentValue,
                    AdjustmentAmount = amendment.ValueAdjustment,
                    NewValue = contract.CurrentValue + amendment.ValueAdjustment,
                    RecordedById = director.Id,
                    Note = FakePrefix
                });
                contract.CurrentValue += amendment.ValueAdjustment;
                contract.EndDate = amendment.NewExpirationDate;
                amendment.ApprovedById = director.Id;
                amendment.ApprovedAt = now;
            }
        }

        CreateSampleFiles();
        await SeedDocumentsAsync(dbContext, activeProject, secondProject, director, manager, cancellationToken);
        await RepairFakeDocumentFilesAsync(dbContext, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Đã tạo bộ fake data TASK-03 với tiền tố {Prefix}.", FakePrefix);
    }

    private static async Task<ContractParty> EnsurePartyAsync(
        ApplicationDbContext dbContext,
        string code,
        string name,
        ContractPartyRole role,
        CancellationToken cancellationToken)
    {
        var party = await dbContext.ContractParties.FirstOrDefaultAsync(
            item => item.Code == code, cancellationToken);
        if (party is not null) return party;
        party = new ContractParty
        {
            Code = code,
            Name = name,
            PartyRole = role,
            TaxCode = $"FAKE-{code}",
            Address = "Địa chỉ fake",
            IsActive = true
        };
        dbContext.ContractParties.Add(party);
        await dbContext.SaveChangesAsync(cancellationToken);
        return party;
    }

    private static async Task SeedDocumentsAsync(
        ApplicationDbContext dbContext,
        Project activeProject,
        Project secondProject,
        ApplicationUser director,
        ApplicationUser manager,
        CancellationToken cancellationToken)
    {
        var approvedCategory = await dbContext.DocumentCategories.SingleAsync(
            item => item.Code == "Minutes", cancellationToken);
        var reviewCategory = await dbContext.DocumentCategories.SingleAsync(
            item => item.Code == "Design", cancellationToken);
        var oneLevelCategory = await dbContext.DocumentCategories.SingleAsync(
            item => item.Code == "Safety", cancellationToken);
        var contract = await dbContext.Contracts
            .FirstAsync(item => item.ContractNumber.StartsWith("HD-FAKE-TASK03"), cancellationToken);

        var documents = new[]
        {
            (code: "DOC-FAKE-TASK03-P01-0001", title: "Tài liệu được duyệt", project: activeProject, category: approvedCategory, status: DocumentApprovalStatus.Approved, systemWide: false),
            (code: "DOC-FAKE-TASK03-SYS-0001", title: "Hướng dẫn chung hệ thống", project: (Project?)null, category: approvedCategory, status: DocumentApprovalStatus.Approved, systemWide: true),
            (code: "DOC-FAKE-TASK03-P01-0002", title: "Tài liệu chờ kiểm tra cấp 1", project: activeProject, category: reviewCategory, status: DocumentApprovalStatus.PendingReview, systemWide: false),
            (code: "DOC-FAKE-TASK03-P01-0003", title: "Tài liệu chờ duyệt cấp 2", project: activeProject, category: reviewCategory, status: DocumentApprovalStatus.PendingApproval, systemWide: false),
            (code: "DOC-FAKE-TASK03-P01-0004", title: "Tài liệu bị từ chối", project: activeProject, category: oneLevelCategory, status: DocumentApprovalStatus.Rejected, systemWide: false),
            (code: "DOC-FAKE-TASK03-P02-0001", title: "Tài liệu đã lưu trữ", project: secondProject, category: approvedCategory, status: DocumentApprovalStatus.Approved, systemWide: false)
        };

        foreach (var item in documents)
        {
            if (await dbContext.ProjectDocuments.AnyAsync(document => document.DocumentCode == item.code, cancellationToken))
                continue;
            var document = new ProjectDocument
            {
                DocumentCode = item.code,
                ProjectId = item.project?.Id,
                ContractId = item.title.Contains("đã lưu trữ") ? contract.Id : null,
                CategoryId = item.category.Id,
                LegacyCategory = item.category.Code,
                Category = item.category.Code,
                Title = item.title,
                Description = FakePrefix,
                IsSystemWide = item.systemWide,
                FileName = $"{item.code}.pdf",
                StoredFileName = $"{Guid.NewGuid():N}.pdf",
                ContentType = "application/pdf",
                FileSize = 1024,
                UploadedById = item.status == DocumentApprovalStatus.Rejected ? director.Id : manager.Id,
                CreatedByUserId = item.status == DocumentApprovalStatus.Rejected ? director.Id : manager.Id,
                UploadedAt = nowUtc(),
                CurrentVersionNumber = item.code.EndsWith("0001") ? 2 : 1,
                CurrentApprovalStep = item.status switch
                {
                    DocumentApprovalStatus.PendingReview => 1,
                    DocumentApprovalStatus.PendingApproval => 2,
                    DocumentApprovalStatus.Rejected => 1,
                    _ => 0
                },
                SubmittedByUserId = item.status is DocumentApprovalStatus.PendingReview or
                    DocumentApprovalStatus.PendingApproval or DocumentApprovalStatus.Rejected
                    ? (item.status == DocumentApprovalStatus.Rejected ? director.Id : manager.Id)
                    : null,
                ApprovalStatus = item.status,
                RejectionReason = item.status == DocumentApprovalStatus.Rejected
                    ? "FAKE: Nội dung cần bổ sung"
                    : null,
                IsArchived = item.title.Contains("lưu trữ"),
                ArchivedAt = item.title.Contains("lưu trữ") ? nowUtc() : null,
                ArchivedById = item.title.Contains("lưu trữ") ? director.Id : null,
                ArchiveReason = item.title.Contains("lưu trữ") ? "FAKE-TASK03: kiểm thử lưu trữ" : null
            };
            dbContext.ProjectDocuments.Add(document);
            await dbContext.SaveChangesAsync(cancellationToken);
            WriteFakeDocumentFile(document.StoredFileName);
            dbContext.DocumentVersions.Add(new DocumentVersion
            {
                DocumentId = document.Id,
                VersionNumber = 1,
                OriginalFileName = document.FileName,
                StoredFileName = document.StoredFileName,
                RelativeStoragePath = $"Documents/fake-task03/{document.StoredFileName}",
                FileExtension = ".pdf",
                ContentType = document.ContentType,
                FileSize = document.FileSize,
                FileSizeBytes = document.FileSize,
                Sha256Hash = HashFakePdf(),
                ApprovalStatus = item.status,
                UploadedById = document.UploadedById,
                CreatedByUserId = document.CreatedByUserId,
                UploadedAt = document.UploadedAt,
                CreatedAt = document.UploadedAt,
                IsCurrent = document.CurrentVersionNumber == 1
            });
            if (document.CurrentVersionNumber == 2)
            {
                var newName = $"{Guid.NewGuid():N}.pdf";
                WriteFakeDocumentFile(newName);
                document.FileName = "v2-" + document.FileName;
                document.StoredFileName = newName;
                dbContext.DocumentVersions.Add(new DocumentVersion
                {
                    DocumentId = document.Id,
                    VersionNumber = 2,
                    OriginalFileName = "v2-" + document.FileName,
                    StoredFileName = newName,
                    RelativeStoragePath = $"Documents/fake-task03/{newName}",
                    FileExtension = ".pdf",
                    ContentType = "application/pdf",
                    FileSize = 2048,
                    FileSizeBytes = 2048,
                    Sha256Hash = HashFakePdf(),
                    ChangeNote = "FAKE: Phiên bản thử nghiệm",
                    VersionNote = "FAKE: Phiên bản thử nghiệm",
                    ApprovalStatus = DocumentApprovalStatus.Approved,
                    UploadedById = manager.Id,
                    CreatedByUserId = manager.Id,
                    UploadedAt = nowUtc(),
                    CreatedAt = nowUtc(),
                    IsCurrent = true
                });
            }
        }
    }

    private static async Task RepairFakeDocumentFilesAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var documents = await dbContext.ProjectDocuments
            .Include(item => item.Versions)
            .Where(item => item.DocumentCode.StartsWith("DOC-FAKE-TASK03-"))
            .ToListAsync(cancellationToken);

        foreach (var document in documents)
        {
            if (string.IsNullOrWhiteSpace(document.CreatedByUserId))
                document.CreatedByUserId = document.UploadedById;
            foreach (var version in document.Versions)
            {
                if (string.IsNullOrWhiteSpace(version.CreatedByUserId))
                    version.CreatedByUserId = version.UploadedById;
                if (version.CreatedAt == default)
                    version.CreatedAt = version.UploadedAt;
                if (version.FileSizeBytes == 0)
                    version.FileSizeBytes = version.FileSize;
                if (string.IsNullOrWhiteSpace(version.FileExtension))
                    version.FileExtension = ".pdf";
                if (string.IsNullOrWhiteSpace(version.Sha256Hash))
                    version.Sha256Hash = HashFakePdf();

                const string relativePrefix = "Documents/fake-task03/";
                if (!version.RelativeStoragePath.StartsWith(relativePrefix, StringComparison.Ordinal) ||
                    Path.GetFileName(version.RelativeStoragePath) != version.RelativeStoragePath[relativePrefix.Length..])
                    continue;
                WriteFakeDocumentFile(Path.GetFileName(version.RelativeStoragePath));
            }
        }
    }

    private static void WriteFakeDocumentFile(string fileName)
    {
        var directory = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Documents", "fake-task03");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        if (!File.Exists(path))
            File.WriteAllBytes(path, "%PDF-1.4\n%FAKE-TASK03\n%%EOF"u8.ToArray());
    }

    private static string HashFakePdf()
        => Convert.ToHexString(SHA256.HashData("%PDF-1.4\n%FAKE-TASK03\n%%EOF"u8.ToArray())).ToLowerInvariant();

    private static DateTime nowUtc() => DateTime.UtcNow;

    private static void CreateSampleFiles()
    {
        var directory = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Documents", "fake-task03");
        Directory.CreateDirectory(directory);

        File.WriteAllBytes(Path.Combine(directory, "fake-valid.pdf"),
            "%PDF-1.4\n%FAKE-TASK03\n%%EOF"u8.ToArray());
        File.WriteAllBytes(Path.Combine(directory, "fake-valid.png"),
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D]);
        CreateZipSample(Path.Combine(directory, "fake-valid.docx"), "word/document.xml");
        CreateZipSample(Path.Combine(directory, "fake-valid.xlsx"), "xl/workbook.xml");
        File.WriteAllText(Path.Combine(directory, "fake-invalid.pdf"),
            "FAKE-TASK03: nội dung không phải PDF", System.Text.Encoding.UTF8);
        using var oversized = File.Create(Path.Combine(directory, "fake-oversized.pdf"));
        oversized.SetLength(FileValidationSize + 1);
    }

    private const long FileValidationSize = 25 * 1024 * 1024;

    private static void CreateZipSample(string path, string entryName)
    {
        using var archiveStream = File.Create(path);
        using var archive = new System.IO.Compression.ZipArchive(archiveStream, System.IO.Compression.ZipArchiveMode.Create);
        var contentType = entryName.StartsWith("word/", StringComparison.Ordinal)
            ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml";
        var contentTypes = archive.CreateEntry("[Content_Types].xml");
        using (var contentTypesWriter = new StreamWriter(contentTypes.Open()))
        {
            contentTypesWriter.Write(
                $"<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/{entryName}\" ContentType=\"{contentType}\"/></Types>");
        }
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write("<root>[FAKE-TASK03]</root>");
    }
}
