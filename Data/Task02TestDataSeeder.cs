using ConstructionProjectManagement.Models;
using ConstructionProjectManagement.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace ConstructionProjectManagement.Data;

// Dữ liệu kiểm thử TASK-02: chỉ chạy ở Development khi bật CPMS_SEED_TASK02_TEST_DATA=true.
// Nhận diện bằng: phòng ban "[TEST] ...", mã nhân viên "TEST-NV-xxx", email "...@test.invalid", dự án "TEST-PRJ-002".
public static class Task02TestDataSeeder
{
    public const string DepartmentPrefix = "[TEST] ";
    public const string EmployeePrefix = "TEST-NV-";
    public const string EmailDomain = "@test.invalid";
    public const string SecondProjectCode = "TEST-PRJ-002";
    public const string ExistingProjectCode = "CPMS-2026-001";

    private static readonly string[] DepartmentNames =
    [
        "Ban chỉ huy công trường",
        "Phòng kỹ thuật",
        "Phòng giám sát chất lượng",
        "Phòng hành chính nhân sự"
    ];

    // Mã, họ tên giả lập, chức danh, phòng ban (chỉ số), có hoạt động hay không
    private static readonly (string Code, string Name, string Position, int Dept)[] Employees =
    [
        ("001", "Test Nhân Viên 001 (PM)", "Quản lý dự án", 0),
        ("002", "Test Nhân Viên 002 (Phó PM)", "Phó quản lý dự án", 0),
        ("003", "Test Nhân Viên 003 (Chỉ huy trưởng)", "Quản lý công trường", 0),
        ("004", "Test Nhân Viên 004 (Kỹ sư giám sát)", "Kỹ sư giám sát", 2),
        ("005", "Test Nhân Viên 005 (Nhiều vai trò)", "Kỹ sư hiện trường", 1),
        ("006", "Test Nhân Viên 006 (Đổi vai trò)", "Kỹ sư giám sát", 2),
        ("007", "Test Nhân Viên 007 (Đã nghỉ việc)", "Kỹ sư kỹ thuật", 1),
        ("008", "Test Nhân Viên 008 (Sẵn sàng thêm)", "Kỹ sư kỹ thuật", 3)
    ];

    public static bool IsEnabled(IConfiguration configuration, string key) =>
        bool.TryParse(configuration[key], out var enabled) && enabled;

    public static async Task SeedAsync(
        ApplicationDbContext db,
        IProjectService projects,
        IEmployeeService employees,
        ILogger logger,
        CancellationToken ct = default)
    {
        var existingProject = await db.Projects.AsNoTracking()
            .SingleOrDefaultAsync(p => p.ProjectCode == ExistingProjectCode, ct);
        if (existingProject is null)
        {
            logger.LogWarning("Bỏ qua seed TASK-02: chưa có dự án {Code}.", ExistingProjectCode);
            return;
        }

        var departments = await EnsureDepartmentsAsync(db, ct);
        var emp = await EnsureEmployeesAsync(db, employees, departments, ct);
        var projectB = await EnsureSecondProjectAsync(db, projects, ct);

        await EnsurePmHistoryAsync(db, projects, existingProject.Id, emp, ct);
        var a = existingProject.Id;
        var b = projectB.Id;

        // Dự án A: PM đã bổ nhiệm; các vai trò còn lại phân công qua service.
        await EnsureRolesAsync(db, projects, a, emp["002"], [ProjectRoleCode.DeputyProjectManager], ct);
        await EnsureRolesAsync(db, projects, a, emp["003"], [ProjectRoleCode.SiteManager], ct);
        await EnsureRolesAsync(db, projects, a, emp["004"], [ProjectRoleCode.SupervisingEngineer], ct);
        await EnsureRolesAsync(db, projects, a, emp["005"],
            [ProjectRoleCode.DeputyProjectManager, ProjectRoleCode.SiteManager], ct);

        // 005 giữ Site Manager nhưng 003 cũng là Site Manager ở A: không vi phạm (giới hạn theo số dự án).
        await EnsureRoleChangeAsync(db, projects, a, emp["006"], ct);
        await EnsureDepartedAsync(db, projects, employees, a, emp["007"], ct);

        // Dự án B: chưa có PM chính; PM/Phó PM không bị giới hạn nhiều dự án.
        await EnsureRolesAsync(db, projects, b, emp["001"], [ProjectRoleCode.DeputyProjectManager], ct);
        await EnsureRolesAsync(db, projects, b, emp["002"], [ProjectRoleCode.DeputyProjectManager], ct);
    }

    private static async Task<Dictionary<string, Department>> EnsureDepartmentsAsync(
        ApplicationDbContext db, CancellationToken ct)
    {
        foreach (var name in DepartmentNames)
        {
            var full = DepartmentPrefix + name;
            if (!await db.Departments.AnyAsync(d => d.Name == full, ct))
            {
                db.Departments.Add(new Department { Name = full });
            }
        }

        await db.SaveChangesAsync(ct);
        return await db.Departments
            .Where(d => d.Name.StartsWith(DepartmentPrefix))
            .ToDictionaryAsync(d => d.Name, ct);
    }

    private static async Task<Dictionary<string, Employee>> EnsureEmployeesAsync(
        ApplicationDbContext db,
        IEmployeeService employees,
        Dictionary<string, Department> departments,
        CancellationToken ct)
    {
        foreach (var item in Employees)
        {
            var code = EmployeePrefix + item.Code;
            if (await db.Employees.AnyAsync(e => e.EmployeeCode == code, ct))
            {
                continue;
            }

            var email = $"test.nv{item.Code}{EmailDomain}";
            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);
            if (user is null)
            {
                // Tài khoản không có mật khẩu nên không thể đăng nhập; chỉ để thỏa liên kết Employee.UserId.
                user = new ApplicationUser
                {
                    UserName = email,
                    NormalizedUserName = email.ToUpperInvariant(),
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    FullName = item.Name,
                    IsActive = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                };
                db.Users.Add(user);
                await db.SaveChangesAsync(ct);
            }

            var department = departments[DepartmentPrefix + DepartmentNames[item.Dept]];
            var result = await employees.SaveAsync(new Employee
            {
                EmployeeCode = code,
                UserId = user.Id,
                DepartmentId = department.Id,
                Position = item.Position,
                Phone = "0900000" + item.Code,
                HireDate = new DateTime(2024, 1, 1),
                IsActive = true
            }, ct);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Seed nhân viên {code} thất bại: {result.Error}");
            }
        }

        return await db.Employees
            .Where(e => e.EmployeeCode.StartsWith(EmployeePrefix))
            .ToDictionaryAsync(e => e.EmployeeCode[EmployeePrefix.Length..], ct);
    }

    private static async Task<Project> EnsureSecondProjectAsync(
        ApplicationDbContext db, IProjectService projects, CancellationToken ct)
    {
        var existing = await db.Projects.AsNoTracking()
            .SingleOrDefaultAsync(p => p.ProjectCode == SecondProjectCode, ct);
        if (existing is not null)
        {
            return existing;
        }

        var result = await projects.CreateAsync(new Project
        {
            ProjectCode = SecondProjectCode,
            Name = "[TEST] Dự án kiểm thử giới hạn phân công",
            Location = "Địa điểm giả lập",
            ClientName = "Khách hàng giả lập",
            StartDate = new DateTime(2026, 12, 1),
            ExpectedEndDate = new DateTime(2027, 6, 1),
            Budget = 10_000_000_000m
        }, null, null, ct);
        if (!result.Succeeded || result.Project is null)
        {
            throw new InvalidOperationException($"Seed dự án {SecondProjectCode} thất bại: {result.Error}");
        }

        return result.Project;
    }

    // Lịch sử PM: bổ nhiệm 002 trước, sau đó chuyển cho 001. Chỉ chạy khi dự án chưa có PM và chưa có lịch sử test.
    private static async Task EnsurePmHistoryAsync(
        ApplicationDbContext db,
        IProjectService projects,
        int projectId,
        Dictionary<string, Employee> emp,
        CancellationToken ct)
    {
        var testUserIds = emp.Values.Select(e => e.UserId).ToList();
        var seeded = await db.ProjectManagerHistories.AnyAsync(
            h => h.ProjectId == projectId &&
                 h.NewProjectManagerId != null &&
                 testUserIds.Contains(h.NewProjectManagerId), ct);
        var project = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == projectId, ct);
        if (seeded || project.ProjectManagerId is not null)
        {
            return;
        }

        var first = await projects.UpdateAsync(Clone(project), emp["002"].UserId, "[TEST] Bổ nhiệm PM ban đầu", ct);
        Require(first, "bổ nhiệm PM ban đầu");
        db.ChangeTracker.Clear();
        project = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == projectId, ct);
        var second = await projects.UpdateAsync(Clone(project), emp["001"].UserId, "[TEST] Luân chuyển PM chính", ct);
        Require(second, "thay PM chính");
        db.ChangeTracker.Clear();
    }

    private static Project Clone(Project p) => new()
    {
        Id = p.Id,
        ProjectCode = p.ProjectCode,
        Name = p.Name,
        Location = p.Location,
        ClientName = p.ClientName,
        StartDate = p.StartDate,
        ExpectedEndDate = p.ExpectedEndDate,
        Budget = p.Budget,
        Status = p.Status
    };

    private static async Task EnsureRolesAsync(
        ApplicationDbContext db,
        IProjectService projects,
        int projectId,
        Employee employee,
        string[] roleCodes,
        CancellationToken ct)
    {
        var missing = new List<string>();
        foreach (var code in roleCodes)
        {
            if (!await HasEverHeldAsync(db, projectId, employee.Id, code, ct))
            {
                missing.Add(code);
            }
        }

        if (missing.Count == 0)
        {
            return;
        }

        Require(await projects.AddProjectMemberAsync(projectId, employee.Id, missing, ct),
            $"phân công {employee.EmployeeCode}");
        db.ChangeTracker.Clear();
    }

    // 006: Kỹ sư giám sát -> kết thúc -> thêm Phó PM -> phân công lại Kỹ sư giám sát (bản ghi mới).
    private static async Task EnsureRoleChangeAsync(
        ApplicationDbContext db,
        IProjectService projects,
        int projectId,
        Employee employee,
        CancellationToken ct)
    {
        if (await HasEverHeldAsync(db, projectId, employee.Id, ProjectRoleCode.SupervisingEngineer, ct))
        {
            return;
        }

        Require(await projects.AddProjectMemberAsync(
            projectId, employee.Id, [ProjectRoleCode.SupervisingEngineer], ct), "006 nhận vai trò");
        var (memberId, assignmentId) = await FindActiveAssignmentAsync(
            db, projectId, employee.Id, ProjectRoleCode.SupervisingEngineer, ct);
        Require(await projects.EndRoleAsync(projectId, memberId, assignmentId, ct), "006 kết thúc vai trò");
        Require(await projects.AddProjectMemberAsync(
            projectId, employee.Id, [ProjectRoleCode.DeputyProjectManager], ct), "006 đổi sang Phó PM");
        Require(await projects.AddProjectMemberAsync(
            projectId, employee.Id, [ProjectRoleCode.SupervisingEngineer], ct), "006 phân công lại");
        db.ChangeTracker.Clear();
    }

    // 007: được phân công rồi nghỉ việc thật (Employee.IsActive=false) -> service tự kết thúc phân công.
    private static async Task EnsureDepartedAsync(
        ApplicationDbContext db,
        IProjectService projects,
        IEmployeeService employees,
        int projectId,
        Employee employee,
        CancellationToken ct)
    {
        if (!await HasEverHeldAsync(db, projectId, employee.Id, ProjectRoleCode.DeputyProjectManager, ct) &&
            employee.IsActive)
        {
            Require(await projects.AddProjectMemberAsync(
                projectId, employee.Id, [ProjectRoleCode.DeputyProjectManager], ct), "007 nhận vai trò");
            db.ChangeTracker.Clear();
        }

        // Truyền bản sao tách rời để service nhận biết IsActive thay đổi và tự kết thúc phân công.
        var current = await db.Employees.AsNoTracking().SingleAsync(e => e.Id == employee.Id, ct);
        if (current.IsActive)
        {
            current.IsActive = false;
            Require(await employees.SaveAsync(current, ct), "007 nghỉ việc");
        }

        db.ChangeTracker.Clear();

        // Sửa dữ liệu seed dở: nhân viên đã nghỉ nhưng vẫn còn phân công đang hiệu lực.
        var memberId = await db.ProjectMembers
            .Where(m => m.ProjectId == projectId && m.EmployeeId == employee.Id &&
                        m.RoleAssignments.Any(a => a.EndedAt == null))
            .Select(m => (int?)m.Id)
            .SingleOrDefaultAsync(ct);
        if (memberId is not null)
        {
            Require(await projects.EndMembershipAsync(projectId, memberId.Value, ct), "007 kết thúc thành viên");
        }

        db.ChangeTracker.Clear();
    }

    private static Task<bool> HasEverHeldAsync(
        ApplicationDbContext db, int projectId, int employeeId, string roleCode, CancellationToken ct) =>
        db.ProjectMemberRoleAssignments.AnyAsync(
            a => a.ProjectRoleCode == roleCode &&
                 a.ProjectMember.ProjectId == projectId &&
                 a.ProjectMember.EmployeeId == employeeId, ct);

    private static async Task<(int MemberId, int AssignmentId)> FindActiveAssignmentAsync(
        ApplicationDbContext db, int projectId, int employeeId, string roleCode, CancellationToken ct)
    {
        var item = await db.ProjectMemberRoleAssignments
            .Where(a => a.ProjectRoleCode == roleCode && a.EndedAt == null &&
                        a.ProjectMember.ProjectId == projectId && a.ProjectMember.EmployeeId == employeeId)
            .Select(a => new { a.ProjectMemberId, a.Id })
            .SingleAsync(ct);
        return (item.ProjectMemberId, item.Id);
    }

    private static void Require((bool Succeeded, string? Error) result, string step)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Seed TASK-02 thất bại ở bước '{step}': {result.Error}");
        }
    }

    // Chỉ xóa bản ghi có dấu hiệu kiểm thử. Dự án CPMS-2026-001 được giữ lại, chỉ gỡ PM/thành viên test.
    public static async Task RemoveAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var like = EmployeePrefix + "%";
        var emailLike = "test.nv%" + EmailDomain;
        const string testUsers = "SELECT UserId FROM Employees WHERE EmployeeCode LIKE {0}";
        const string secondProject = "SELECT Id FROM Projects WHERE ProjectCode = {1}";
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM ProjectMemberRoleAssignments WHERE ProjectMemberId IN (" +
            "SELECT pm.Id FROM ProjectMembers pm WHERE pm.EmployeeId IN (SELECT Id FROM Employees WHERE EmployeeCode LIKE {0}) " +
            "OR pm.ProjectId IN (" + secondProject + "))",
            new object[] { like, SecondProjectCode });
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM ProjectMembers WHERE EmployeeId IN (SELECT Id FROM Employees WHERE EmployeeCode LIKE {0}) " +
            "OR ProjectId IN (" + secondProject + ")",
            new object[] { like, SecondProjectCode });

        // G? PM test kh?i d? ?n th?t; trigger ghi th?m 1 d?ng l?ch s? v? d?ng n?y b? x?a ngay b?n d??i.
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE Projects SET ProjectManagerId = NULL WHERE ProjectManagerId IN (" + testUsers + ")", new object[] { like });
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM ProjectManagerHistories WHERE ProjectId IN (" + secondProject + ") " +
            "OR PreviousProjectManagerId IN (" + testUsers + ") OR NewProjectManagerId IN (" + testUsers + ")",
            new object[] { like, SecondProjectCode });
        // M? l?i d?ng l?ch s? m?i nh?t c?a d? ?n ?ang kh?ng c?n d?ng hi?u l?c.
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE ProjectManagerHistories SET EndedAt = NULL WHERE Id IN (" +
            "SELECT MAX(h.Id) FROM ProjectManagerHistories h JOIN Projects p ON p.Id = h.ProjectId " +
            "WHERE p.ProjectManagerId IS NULL GROUP BY h.ProjectId " +
            "HAVING SUM(CASE WHEN h.EndedAt IS NULL THEN 1 ELSE 0 END) = 0)");

        await db.Database.ExecuteSqlRawAsync("DELETE FROM Projects WHERE ProjectCode = {0}", new object[] { SecondProjectCode });
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Employees WHERE EmployeeCode LIKE {0}", new object[] { like });
        await db.Database.ExecuteSqlRawAsync("DELETE FROM AspNetUsers WHERE Email LIKE {0}", new object[] { emailLike });
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM Departments WHERE Name LIKE {0} AND Id NOT IN " +
            "(SELECT DepartmentId FROM Employees WHERE DepartmentId IS NOT NULL)",
            new object[] { DepartmentPrefix + "%" });

        await tx.CommitAsync(ct);
        logger.LogInformation("?? x?a d? li?u ki?m th? TASK-02.");
    }
}

