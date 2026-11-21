using ConstructionProjectManagement.Data;
using ConstructionProjectManagement.Models;
using ConstructionProjectManagement.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace ConstructionProjectManagement.Tests;

public sealed class Task03ContractServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private ApplicationDbContext _db = null!;
    private ContractService _service = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
        _db = new ApplicationDbContext(options);
        await _db.Database.MigrateAsync();
        _service = new ContractService(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task ApproveAmendment_AppliesValueAndExpirationExactlyOnce()
    {
        var project = await AddProjectAsync();
        var creator = new ApplicationUser { Id = "creator", UserName = "creator", OrganizationName = "Owner" };
        var director = new ApplicationUser { Id = "director", UserName = "director" };
        _db.Users.AddRange(creator, director);
        await AddRoleAsync(director.Id, "Project Director");
        await _db.SaveChangesAsync();

        var contract = new Contract
        {
            ContractNumber = "HD-TEST-0001",
            Title = "Hợp đồng thử nghiệm",
            ProjectId = project.Id,
            OriginalValue = 100,
            CurrentValue = 100,
            ContractValue = 100,
            EffectiveDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
            CreatedById = creator.Id,
            Status = ContractStatus.InProgress
        };
        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();
        _db.ContractProjects.Add(new ContractProject { ContractId = contract.Id, ProjectId = project.Id, AllocatedValue = 100 });
        _db.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = director.Id, IsActive = true });
        var amendment = new ContractAmendment
        {
            ContractId = contract.Id,
            AmendmentNumber = "HD-TEST-0001-PL01",
            Title = "Điều chỉnh",
            EffectiveDate = DateTime.UtcNow.Date,
            ChangeDescription = "Tăng giá trị và gia hạn",
            ValueAdjustment = 25,
            NewExpirationDate = DateTime.UtcNow.Date.AddDays(60),
            ApprovalStatus = AmendmentApprovalStatus.PendingApproval,
            CreatedById = creator.Id
        };
        _db.ContractAmendments.Add(amendment);
        await _db.SaveChangesAsync();

        var result = await _service.ApproveAmendmentAsync(amendment.Id, director.Id, false);
        Assert.True(result.Succeeded, result.Error);
        await _db.Entry(contract).ReloadAsync();
        Assert.Equal(125m, contract.CurrentValue);
        Assert.Equal(DateTime.UtcNow.Date.AddDays(60), contract.EndDate!.Value);
        Assert.Single(await _db.ContractValueHistories.Where(item => item.AmendmentId == amendment.Id).ToListAsync());

        var repeated = await _service.ApproveAmendmentAsync(amendment.Id, director.Id, false);
        Assert.False(repeated.Succeeded);
    }

    [Fact]
    public async Task CreateAsync_AllowsMultipleProjectAllocations()
    {
        var project1 = await AddProjectAsync();
        var project2 = await AddProjectAsync();
        var manager = new ApplicationUser { Id = "manager-ui", UserName = "manager-ui" };
        _db.Users.Add(manager);
        await AddRoleAsync(manager.Id, "Project Manager");
        _db.ProjectMembers.AddRange(
            new ProjectMember { ProjectId = project1.Id, UserId = manager.Id, IsActive = true },
            new ProjectMember { ProjectId = project2.Id, UserId = manager.Id, IsActive = true });
        await _db.SaveChangesAsync();

        var contract = new Contract
        {
            ContractNumber = "TAM",
            Title = "[FAKE-TASK03] Hợp đồng nhiều dự án",
            ProjectId = project1.Id,
            OriginalValue = 123456,
            CurrentValue = 123456,
            ContractValue = 123456,
            EffectiveDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddMonths(6),
            Status = ContractStatus.Draft,
            ContractProjects =
            [
                new ContractProject { ProjectId = project1.Id, AllocatedValue = 80000 },
                new ContractProject { ProjectId = project2.Id, AllocatedValue = 43456 }
            ]
        };
        var result = await _service.CreateAsync(contract, manager.Id, false);
        Assert.True(result.Succeeded, result.Error);
        Assert.StartsWith("HD-", contract.ContractNumber);
        Assert.Equal(2, await _db.ContractProjects.CountAsync(item => item.ContractId == contract.Id));
    }

    [Fact]
    public async Task SelfApproval_IsRejected()
    {
        var project = await AddProjectAsync();
        var user = new ApplicationUser { Id = "user-self", UserName = "user-self" };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        var contract = new Contract
        {
            ContractNumber = "HD-TEST-SELF",
            Title = "Tự duyệt",
            ProjectId = project.Id,
            OriginalValue = 10,
            CurrentValue = 10,
            ContractValue = 10,
            CreatedById = user.Id,
            Status = ContractStatus.PendingApproval,
            ApprovalStep = 2
        };
        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();
        var result = await _service.ApproveAsync(contract.Id, user.Id, false);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ArchivedContract_RemainsVisibleInArchiveViewAndCanBeRestoredInScope()
    {
        var project = await AddProjectAsync();
        var manager = new ApplicationUser
        {
            Id = "manager-archive",
            UserName = "manager-archive",
            FullName = "Quản lý kiểm thử"
        };
        _db.Users.Add(manager);
        await AddRoleAsync(manager.Id, "Project Manager");
        _db.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = project.Id,
            UserId = manager.Id,
            IsActive = true
        });
        await _db.SaveChangesAsync();

        var contract = new Contract
        {
            ContractNumber = "HD-TEST-ARCHIVED",
            Title = "Hợp đồng đã lưu trữ",
            ProjectId = project.Id,
            OriginalValue = 100,
            CurrentValue = 100,
            ContractValue = 100,
            CreatedById = manager.Id,
            IsArchived = true,
            ArchiveReason = "Kiểm thử archive",
            ArchivedAt = DateTime.UtcNow,
            ContractProjects =
            [
                new ContractProject { ProjectId = project.Id, AllocatedValue = 100 }
            ],
            AuditLogs =
            [
                new ContractAuditLog
                {
                    Action = "Archived",
                    PerformedById = manager.Id,
                    Comment = "Kiểm thử archive"
                }
            ]
        };
        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();

        Assert.Empty(await _service.GetVisibleAsync(manager.Id, false));
        var archivedContracts = await _service.GetVisibleAsync(
            manager.Id, false, CancellationToken.None, includeArchived: true);
        Assert.Contains(archivedContracts, item => item.Id == contract.Id && item.IsArchived);

        var details = await _service.GetByIdAsync(contract.Id, manager.Id, false);
        Assert.NotNull(details);
        Assert.True(details.IsArchived);
        Assert.Equal("Kiểm thử archive", Assert.Single(details.AuditLogs).Comment);

        var restore = await _service.RestoreAsync(contract.Id, manager.Id, false);

        Assert.True(restore.Succeeded, restore.Error);
        await _db.Entry(contract).ReloadAsync();
        Assert.False(contract.IsArchived);
        Assert.Null(contract.ArchivedAt);
        Assert.Null(contract.ArchiveReason);
        Assert.Contains(
            await _db.ContractAuditLogs.Where(item => item.ContractId == contract.Id).ToListAsync(),
            item => item.Action == "Restored" && item.PerformedById == manager.Id);
    }

    [Fact]
    public async Task ArchivedContract_CannotBeViewedOrRestoredOutsideProjectScope()
    {
        var project = await AddProjectAsync();
        var manager = new ApplicationUser { Id = "manager-outside", UserName = "manager-outside" };
        _db.Users.Add(manager);
        await AddRoleAsync(manager.Id, "Project Manager");
        await _db.SaveChangesAsync();

        var contract = new Contract
        {
            ContractNumber = "HD-TEST-OUTSIDE",
            Title = "Ngoài phạm vi",
            ProjectId = project.Id,
            OriginalValue = 100,
            CurrentValue = 100,
            ContractValue = 100,
            IsArchived = true,
            ContractProjects =
            [
                new ContractProject { ProjectId = project.Id, AllocatedValue = 100 }
            ]
        };
        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();

        Assert.Null(await _service.GetByIdAsync(contract.Id, manager.Id, false));
        Assert.True(await _service.ExistsAsync(contract.Id));
        var restore = await _service.RestoreAsync(contract.Id, manager.Id, false);

        Assert.False(restore.Succeeded);
        Assert.True((await _db.Contracts.SingleAsync(item => item.Id == contract.Id)).IsArchived);
        Assert.Empty(await _db.ContractAuditLogs.Where(item => item.ContractId == contract.Id).ToListAsync());
    }

    private async Task<Project> AddProjectAsync()
    {
        var project = new Project
        {
            ProjectCode = "CP-" + Guid.NewGuid().ToString("N")[..6],
            Name = "Dự án thử nghiệm",
            StartDate = DateTime.UtcNow,
            ExpectedEndDate = DateTime.UtcNow.AddDays(90)
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return project;
    }

    private async Task AddRoleAsync(string userId, string roleName)
    {
        var role = await _db.Roles.SingleOrDefaultAsync(item => item.Name == roleName);
        if (role is null)
        {
            role = new IdentityRole(roleName) { Id = Guid.NewGuid().ToString("N") };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();
        }
        _db.UserRoles.Add(new IdentityUserRole<string> { UserId = userId, RoleId = role.Id });
        await _db.SaveChangesAsync();
    }
}
