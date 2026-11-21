using ConstructionProjectManagement.Data;
using ConstructionProjectManagement.Models;
using ConstructionProjectManagement.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace ConstructionProjectManagement.Tests;

public sealed class Task02ProjectServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private ApplicationDbContext _db = null!;
    private ProjectService _projects = null!;
    private EmployeeService _employees = null!;
    private Employee _employee1 = null!;
    private Employee _employee2 = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new ApplicationDbContext(options);
        await _db.Database.MigrateAsync();
        _projects = new ProjectService(_db);
        _employees = new EmployeeService(_db);

        var user1 = NewUser("qa-employee-1");
        var user2 = NewUser("qa-employee-2");
        _db.Users.AddRange(user1, user2);
        var department = new Department { Name = "QA Department" };
        _db.Departments.Add(department);
        await _db.SaveChangesAsync();

        _employee1 = await AddEmployeeAsync("QA-EMP-001", user1.Id, department.Id);
        _employee2 = await AddEmployeeAsync("QA-EMP-002", user2.Id, department.Id);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_AllowsProjectWithoutPrimaryManager()
    {
        var result = await _projects.CreateAsync(NewProject("QA-P-001"), null, null);

        Assert.True(result.Succeeded, result.Error);
        Assert.NotNull(result.Project);
        Assert.Null(result.Project.ProjectManagerId);
        var history = await _db.ProjectManagerHistories
            .SingleAsync(item => item.ProjectId == result.Project.Id);
        Assert.Null(history.NewProjectManagerId);
        Assert.Null(history.EndedAt);
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidValuesUnknownManagerAndArchivedDuplicateCode()
    {
        var invalidDates = NewProject("QA-P-VALIDATION-DATE");
        invalidDates.ExpectedEndDate = invalidDates.StartDate.AddDays(-1);
        Assert.False((await _projects.CreateAsync(invalidDates, null, null)).Succeeded);

        var negativeBudget = NewProject("QA-P-VALIDATION-BUDGET");
        negativeBudget.Budget = -1;
        Assert.False((await _projects.CreateAsync(negativeBudget, null, null)).Succeeded);

        Assert.False((await _projects.CreateAsync(
            NewProject("QA-P-VALIDATION-MANAGER"),
            "unknown-identity-user",
            null)).Succeeded);

        var original = await CreateProjectAsync("QA-P-ARCHIVED");
        var archived = await _db.Projects.SingleAsync(item => item.Id == original.Id);
        archived.IsArchived = true;
        await _db.SaveChangesAsync();

        var duplicate = await _projects.CreateAsync(NewProject("QA-P-ARCHIVED"), null, null);
        Assert.False(duplicate.Succeeded);
        Assert.Equal(1, await _db.Projects.CountAsync(item => item.ProjectCode == "QA-P-ARCHIVED"));
    }

    [Fact]
    public async Task ManagerChanges_SynchronizeMemberAndPreserveHistory()
    {
        var project = await CreateProjectAsync("QA-P-002");
        var added = await _projects.AddProjectMemberAsync(
            project.Id,
            _employee1.Id,
            [ProjectRoleCode.DeputyProjectManager]);
        Assert.True(added.Succeeded, added.Error);

        var firstAppointment = await _projects.UpdateAsync(
            await ReloadProjectAsync(project.Id),
            _employee1.UserId,
            "QA appointment");
        Assert.True(firstAppointment.Succeeded, firstAppointment.Error);

        Assert.Equal(1, await _db.ProjectMembers.CountAsync(
            item => item.ProjectId == project.Id && item.EmployeeId == _employee1.Id));
        Assert.Equal(1, await _db.ProjectMemberRoleAssignments.CountAsync(
            item => item.ProjectMember.ProjectId == project.Id &&
                    item.ProjectMember.EmployeeId == _employee1.Id &&
                    item.ProjectRoleCode == ProjectRoleCode.ProjectManager &&
                    item.EndedAt == null));

        var replacement = await _projects.UpdateAsync(
            await ReloadProjectAsync(project.Id),
            _employee2.UserId,
            "QA replacement");
        Assert.True(replacement.Succeeded, replacement.Error);

        var history = await _db.ProjectManagerHistories
            .AsNoTracking()
            .Where(item => item.ProjectId == project.Id)
            .OrderBy(item => item.Id)
            .ToListAsync();
        Assert.Equal(3, history.Count);
        Assert.NotNull(history[1].EndedAt);
        Assert.Equal("QA appointment", history[1].ChangeReason);
        Assert.Equal("QA replacement", history[2].ChangeReason);

        var oldManager = await _db.ProjectMembers.SingleAsync(
            item => item.ProjectId == project.Id && item.EmployeeId == _employee1.Id);
        Assert.True(oldManager.IsActive);
        Assert.Contains(oldManager.RoleAssignments, item =>
            item.ProjectRoleCode == ProjectRoleCode.DeputyProjectManager && item.EndedAt == null);
        Assert.DoesNotContain(oldManager.RoleAssignments, item =>
            item.ProjectRoleCode == ProjectRoleCode.ProjectManager && item.EndedAt == null);
    }

    [Fact]
    public async Task AddProjectMemberAsync_RejectsDuplicateRoleAndUnknownRole()
    {
        var project = await CreateProjectAsync("QA-P-003");
        var first = await _projects.AddProjectMemberAsync(
            project.Id,
            _employee1.Id,
            [ProjectRoleCode.DeputyProjectManager]);
        Assert.True(first.Succeeded, first.Error);

        var duplicate = await _projects.AddProjectMemberAsync(
            project.Id,
            _employee1.Id,
            [ProjectRoleCode.DeputyProjectManager]);
        var invalidRole = await _projects.AddProjectMemberAsync(
            project.Id,
            _employee1.Id,
            ["QA_UNKNOWN_ROLE"]);

        Assert.False(duplicate.Succeeded);
        Assert.False(invalidRole.Succeeded);
        Assert.Equal(1, await _db.ProjectMembers.CountAsync(
            item => item.ProjectId == project.Id && item.EmployeeId == _employee1.Id));
        Assert.Equal(1, await _db.ProjectMemberRoleAssignments.CountAsync(
            item => item.ProjectMember.ProjectId == project.Id &&
                    item.ProjectMember.EmployeeId == _employee1.Id &&
                    item.EndedAt == null));
    }

    [Fact]
    public async Task AddProjectMemberAsync_DistinguishesEmploymentFromLoginAccountState()
    {
        var project = await CreateProjectAsync("QA-P-EMPLOYMENT");
        var loginDisabledUser = await _db.Users.SingleAsync(item => item.Id == _employee2.UserId);
        loginDisabledUser.IsActive = false;
        await _db.SaveChangesAsync();

        var accepted = await _projects.AddProjectMemberAsync(
            project.Id,
            _employee2.Id,
            [ProjectRoleCode.DeputyProjectManager]);
        Assert.True(accepted.Succeeded, accepted.Error);

        var employee = await _db.Employees.SingleAsync(item => item.Id == _employee1.Id);
        employee.IsActive = false;
        await _db.SaveChangesAsync();
        var rejected = await _projects.AddProjectMemberAsync(
            project.Id,
            _employee1.Id,
            [ProjectRoleCode.DeputyProjectManager]);
        Assert.False(rejected.Succeeded);
    }

    [Fact]
    public async Task SiteManagerLimit_AllowsReassignmentOnlyAfterEndingOldAssignment()
    {
        var firstProject = await CreateProjectAsync("QA-P-004");
        var secondProject = await CreateProjectAsync("QA-P-005");
        var first = await _projects.AddProjectMemberAsync(
            firstProject.Id,
            _employee1.Id,
            [ProjectRoleCode.SiteManager]);
        var blocked = await _projects.AddProjectMemberAsync(
            secondProject.Id,
            _employee1.Id,
            [ProjectRoleCode.SiteManager]);

        Assert.True(first.Succeeded, first.Error);
        Assert.False(blocked.Succeeded);

        var activeAssignment = await _db.ProjectMemberRoleAssignments.SingleAsync(
            item => item.ProjectMember.ProjectId == firstProject.Id &&
                    item.ProjectMember.EmployeeId == _employee1.Id &&
                    item.ProjectRoleCode == ProjectRoleCode.SiteManager &&
                    item.EndedAt == null);
        var ended = await _projects.EndRoleAsync(
            firstProject.Id,
            activeAssignment.ProjectMemberId,
            activeAssignment.Id);
        Assert.True(ended.Succeeded, ended.Error);

        var reassigned = await _projects.AddProjectMemberAsync(
            secondProject.Id,
            _employee1.Id,
            [ProjectRoleCode.SiteManager]);
        Assert.True(reassigned.Succeeded, reassigned.Error);
        Assert.Equal(2, await _db.ProjectMemberRoleAssignments.CountAsync(
            item => item.ProjectMember.EmployeeId == _employee1.Id &&
                    item.ProjectRoleCode == ProjectRoleCode.SiteManager));
    }

    [Fact]
    public async Task EndingMembership_PreservesHistoryAndReusesMemberOnReassignment()
    {
        var project = await CreateProjectAsync("QA-P-006");
        var added = await _projects.AddProjectMemberAsync(
            project.Id,
            _employee1.Id,
            [ProjectRoleCode.DeputyProjectManager, ProjectRoleCode.SupervisingEngineer]);
        Assert.True(added.Succeeded, added.Error);

        var member = await _db.ProjectMembers.SingleAsync(
            item => item.ProjectId == project.Id && item.EmployeeId == _employee1.Id);
        var ended = await _projects.EndMembershipAsync(project.Id, member.Id);
        Assert.True(ended.Succeeded, ended.Error);

        var oldAssignmentIds = await _db.ProjectMemberRoleAssignments
            .Where(item => item.ProjectMemberId == member.Id)
            .Select(item => item.Id)
            .ToListAsync();
        Assert.False(member.IsActive);
        Assert.Equal(2, oldAssignmentIds.Count);
        Assert.Equal(0, await _db.ProjectMemberRoleAssignments.CountAsync(
            item => item.ProjectMemberId == member.Id && item.EndedAt == null));

        var reactivated = await _projects.AddProjectMemberAsync(
            project.Id,
            _employee1.Id,
            [ProjectRoleCode.SupervisingEngineer]);
        Assert.True(reactivated.Succeeded, reactivated.Error);

        var members = await _db.ProjectMembers
            .Where(item => item.ProjectId == project.Id && item.EmployeeId == _employee1.Id)
            .ToListAsync();
        Assert.Single(members);
        Assert.True(members[0].IsActive);
        Assert.Equal(3, await _db.ProjectMemberRoleAssignments.CountAsync(
            item => item.ProjectMemberId == member.Id));
        Assert.Equal(0, await _db.ProjectMemberRoleAssignments.CountAsync(
            item => oldAssignmentIds.Contains(item.Id) && item.EndedAt == null));
    }

    [Fact]
    public async Task TerminatingEmployee_EndsAssignmentsAndUnsetsPrimaryManager()
    {
        var project = await CreateProjectAsync("QA-P-007", _employee1.UserId);
        var employee = await _db.Employees.AsNoTracking()
            .SingleAsync(item => item.Id == _employee1.Id);
        employee.IsActive = false;

        var saved = await _employees.SaveAsync(employee);

        Assert.True(saved.Succeeded, saved.Error);
        Assert.False((await _db.Employees.SingleAsync(item => item.Id == employee.Id)).IsActive);
        Assert.Null((await _db.Projects.SingleAsync(item => item.Id == project.Id)).ProjectManagerId);
        Assert.Equal(0, await _db.ProjectMemberRoleAssignments.CountAsync(
            item => item.ProjectMember.EmployeeId == employee.Id && item.EndedAt == null));
        Assert.Contains(await _db.ProjectManagerHistories
            .Where(item => item.ProjectId == project.Id)
            .Select(item => item.ChangeReason)
            .ToListAsync(), reason => reason != null && reason.Contains("nghỉ việc"));
    }

    private async Task<Project> CreateProjectAsync(string code, string? managerId = null)
    {
        var result = await _projects.CreateAsync(NewProject(code), managerId, null);
        Assert.True(result.Succeeded, result.Error);
        return result.Project!;
    }

    private async Task<Employee> AddEmployeeAsync(string code, string userId, int departmentId)
    {
        var employee = new Employee
        {
            EmployeeCode = code,
            UserId = userId,
            DepartmentId = departmentId,
            IsActive = true
        };
        var result = await _employees.SaveAsync(employee);
        Assert.True(result.Succeeded, result.Error);
        return employee;
    }

    private async Task<Project> ReloadProjectAsync(int id) =>
        await _db.Projects.AsNoTracking().SingleAsync(item => item.Id == id);

    private static ApplicationUser NewUser(string userName) => new()
    {
        Id = Guid.NewGuid().ToString(),
        UserName = userName,
        NormalizedUserName = userName.ToUpperInvariant(),
        Email = $"{userName}@test.invalid",
        NormalizedEmail = $"{userName}@test.invalid".ToUpperInvariant(),
        SecurityStamp = Guid.NewGuid().ToString(),
        IsActive = true
    };

    private static Project NewProject(string code) => new()
    {
        ProjectCode = code,
        Name = $"Test project {code}",
        StartDate = new DateTime(2026, 1, 1),
        ExpectedEndDate = new DateTime(2027, 1, 1),
        Budget = 0
    };
}
