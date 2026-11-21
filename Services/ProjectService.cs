using ConstructionProjectManagement.Data;
using ConstructionProjectManagement.Models;
using ConstructionProjectManagement.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace ConstructionProjectManagement.Services;

public sealed class ProjectService(ApplicationDbContext dbContext) : IProjectService
{
    public async Task<IReadOnlyCollection<Project>> GetVisibleProjectsAsync(
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Project> query = dbContext.Projects
            .AsNoTracking()
            .Where(project => !project.IsArchived)
            .Include(project => project.ProjectManager);

        if (!isAdmin)
        {
            query = query.Where(project =>
                project.ProjectManagerId == userId ||
                project.Members.Any(member => member.UserId == userId && member.IsActive));
        }

        return await query
            .OrderByDescending(project => project.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Project?> GetByIdAsync(
        int id,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Projects
            .AsNoTracking()
            .Include(project => project.ProjectManager)
            .Include(project => project.Members.Where(member => member.IsActive))
                .ThenInclude(member => member.User)
            .Include(project => project.Members.Where(member => member.IsActive))
                .ThenInclude(member => member.Employee)
            .Include(project => project.Members.Where(member => member.IsActive))
                .ThenInclude(member => member.RoleAssignments.Where(assignment => assignment.EndedAt == null))
                    .ThenInclude(assignment => assignment.ProjectRole)
            .Where(project => project.Id == id && !project.IsArchived);

        if (!isAdmin)
        {
            query = query.Where(project =>
                project.ProjectManagerId == userId ||
                project.Members.Any(member => member.UserId == userId && member.IsActive));
        }

        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Project?> GetProjectMembersAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .Include(project => project.ProjectManager)
            .Include(project => project.Members)
                .ThenInclude(member => member.User)
            .Include(project => project.Members)
                .ThenInclude(member => member.Employee)
            .Include(project => project.Members)
                .ThenInclude(member => member.RoleAssignments)
                    .ThenInclude(assignment => assignment.ProjectRole)
            .SingleOrDefaultAsync(
                project => project.Id == projectId && !project.IsArchived,
                cancellationToken);
    }

    public Task<bool> CanManageMembersAsync(
        int projectId,
        string userId,
        bool canManageAllProjects,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Projects.AnyAsync(
            project => project.Id == projectId &&
                       !project.IsArchived &&
                       (canManageAllProjects || project.ProjectManagerId == userId),
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<Employee>> GetActiveEmployeesAsync(
        string? includeUserId = null,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Employees
            .AsNoTracking()
            .Include(employee => employee.User)
            .Where(employee => employee.IsActive || employee.UserId == includeUserId)
            .OrderBy(employee => employee.EmployeeCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Employee>> GetAssignableEmployeesAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Employees
            .AsNoTracking()
            .Include(employee => employee.User)
            .Where(employee => employee.IsActive)
            .OrderBy(employee => employee.EmployeeCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProjectRole>> GetProjectRolesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ProjectRoles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Succeeded, string? Error, Project? Project)> CreateAsync(
        Project project,
        string? managerId,
        string? managerChangeReason,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var validationError = await ValidateProjectAsync(project, managerId, null, cancellationToken);
        if (validationError is not null)
        {
            return (false, validationError, null);
        }

        var manager = await FindEmployeeByUserIdAsync(managerId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(managerId) && (manager is null || !manager.IsActive))
        {
            return (false, "Project Manager phải là hồ sơ nhân viên đang hoạt động.", null);
        }

        var now = DateTime.UtcNow;
        project.ProjectCode = project.ProjectCode.Trim();
        project.Name = project.Name.Trim();
        project.ProjectManagerId = manager?.UserId;
        project.CreatedAt = now;
        project.UpdatedAt = now;

        if (manager is not null)
        {
            project.Members.Add(new ProjectMember
            {
                UserId = manager.UserId,
                EmployeeId = manager.Id,
                Employee = manager,
                JoinedAt = now,
                IsActive = true,
                RoleAssignments =
                [
                    new ProjectMemberRoleAssignment
                    {
                        ProjectRoleCode = ProjectRoleCode.ProjectManager,
                        StartedAt = now
                    }
                ]
            });
        }

        dbContext.Projects.Add(project);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            if (manager is not null && !string.IsNullOrWhiteSpace(managerChangeReason))
            {
                await SetCurrentManagerReasonAsync(project.Id, managerChangeReason, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, null, project);
        }
        catch (DbUpdateException exception) when (IsSqliteConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "Không thể tạo dự án: mã dự án hoặc dữ liệu liên quan đã trùng hay không hợp lệ.", null);
        }
    }

    public async Task<(bool Succeeded, string? Error)> UpdateAsync(
        Project project,
        string? managerId,
        string? managerChangeReason,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var existing = await dbContext.Projects
            .Include(item => item.Members)
                .ThenInclude(member => member.RoleAssignments)
            .SingleOrDefaultAsync(
                item => item.Id == project.Id && !item.IsArchived,
                cancellationToken);
        if (existing is null)
        {
            return (false, "Không tìm thấy dự án.");
        }

        var validationError = await ValidateProjectAsync(
            project,
            managerId,
            existing.ProjectManagerId,
            cancellationToken,
            project.Id);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        var duplicateCode = await dbContext.Projects.AnyAsync(
            item => item.ProjectCode == project.ProjectCode.Trim() && item.Id != project.Id,
            cancellationToken);
        if (duplicateCode)
        {
            return (false, "Mã dự án đã tồn tại, kể cả trên dự án đã lưu trữ.");
        }

        var managerChanged = !string.Equals(
            existing.ProjectManagerId,
            string.IsNullOrWhiteSpace(managerId) ? null : managerId,
            StringComparison.Ordinal);
        Employee? manager = null;
        if (!string.IsNullOrWhiteSpace(managerId))
        {
            manager = await FindEmployeeByUserIdAsync(managerId, cancellationToken);
            if (manager is null && managerChanged)
            {
                return (false, "Project Manager phải có hồ sơ nhân viên.");
            }

            if (manager is { IsActive: false } && managerChanged)
            {
                return (false, "Không thể bổ nhiệm nhân viên đã nghỉ việc làm Project Manager.");
            }
        }

        try
        {
            if (managerChanged)
            {
                await EndPrimaryManagerRoleAsync(existing, cancellationToken);
            }

            existing.ProjectCode = project.ProjectCode.Trim();
            existing.Name = project.Name.Trim();
            existing.Location = project.Location?.Trim();
            existing.ClientName = project.ClientName?.Trim();
            existing.StartDate = project.StartDate;
            existing.ExpectedEndDate = project.ExpectedEndDate;
            existing.Budget = project.Budget;
            existing.Status = project.Status;
            existing.UpdatedAt = DateTime.UtcNow;

            if (managerChanged)
            {
                existing.ProjectManagerId = manager?.UserId;
            }

            ProjectMember? managerMember = null;
            if (manager is { IsActive: true })
            {
                managerMember = FindMember(existing, manager);
                if (managerMember is null)
                {
                    managerMember = new ProjectMember
                    {
                        Project = existing,
                        ProjectId = existing.Id,
                        UserId = manager.UserId,
                        EmployeeId = manager.Id,
                        Employee = manager,
                        JoinedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    existing.Members.Add(managerMember);
                }
                else
                {
                    if (managerMember.EmployeeId is not null && managerMember.EmployeeId != manager.Id)
                    {
                        return (false, "Thành viên hiện có đang liên kết với hồ sơ nhân viên khác.");
                    }

                    if (managerMember.UserId != manager.UserId)
                    {
                        return (false, "Không thể thay đổi tài khoản gắn với lịch sử thành viên hiện có.");
                    }

                    managerMember.EmployeeId = manager.Id;
                    managerMember.Employee = manager;
                    managerMember.IsActive = true;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            if (managerMember is not null &&
                !managerMember.RoleAssignments.Any(assignment =>
                    assignment.ProjectRoleCode == ProjectRoleCode.ProjectManager &&
                    assignment.EndedAt is null))
            {
                managerMember.RoleAssignments.Add(new ProjectMemberRoleAssignment
                {
                    ProjectMember = managerMember,
                    ProjectMemberId = managerMember.Id,
                    ProjectRoleCode = ProjectRoleCode.ProjectManager,
                    StartedAt = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            if (managerChanged && !string.IsNullOrWhiteSpace(managerChangeReason))
            {
                await SetCurrentManagerReasonAsync(existing.Id, managerChangeReason, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, null);
        }
        catch (DbUpdateException exception) when (IsSqliteConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "Không thể cập nhật dự án do mã dự án hoặc dữ liệu phân công đã trùng hay không hợp lệ.");
        }
    }

    public async Task<(bool Succeeded, string? Error)> AddProjectMemberAsync(
        int projectId,
        int employeeId,
        IReadOnlyCollection<string> roleCodes,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var project = await dbContext.Projects
            .Include(item => item.Members)
                .ThenInclude(member => member.RoleAssignments)
            .SingleOrDefaultAsync(
                item => item.Id == projectId && !item.IsArchived,
                cancellationToken);
        if (project is null)
        {
            return (false, "Không tìm thấy dự án đang hoạt động.");
        }

        var employee = await dbContext.Employees
            .SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);
        if (employee is null || !employee.IsActive)
        {
            return (false, "Chỉ có thể phân công hồ sơ nhân viên đang hoạt động.");
        }

        var normalizedRoleCodes = roleCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedRoleCodes.Length == 0)
        {
            return (false, "Vui lòng chọn ít nhất một vai trò.");
        }

        var roles = await dbContext.ProjectRoles
            .Where(role => normalizedRoleCodes.Contains(role.Code))
            .ToListAsync(cancellationToken);
        if (roles.Count != normalizedRoleCodes.Length)
        {
            return (false, "Danh sách có vai trò không hợp lệ.");
        }

        if (normalizedRoleCodes.Contains(ProjectRoleCode.ProjectManager, StringComparer.Ordinal) &&
            project.ProjectManagerId != employee.UserId)
        {
            return (false, "Hãy bổ nhiệm Project Manager chính tại chức năng cập nhật dự án.");
        }

        foreach (var role in roles.Where(role => role.MaximumConcurrentProjects.HasValue))
        {
            var activeProjectCount = await dbContext.ProjectMemberRoleAssignments
                .Where(assignment =>
                    assignment.ProjectRoleCode == role.Code &&
                    assignment.EndedAt == null &&
                    assignment.ProjectMember.EmployeeId == employee.Id &&
                    assignment.ProjectMember.ProjectId != projectId)
                .Select(assignment => assignment.ProjectMember.ProjectId)
                .Distinct()
                .CountAsync(cancellationToken);
            if (activeProjectCount >= role.MaximumConcurrentProjects)
            {
                return (false, $"Nhân viên đã đạt giới hạn số dự án đang hoạt động cho vai trò {role.Name}.");
            }
        }

        var member = FindMember(project, employee);
        if (member is null && project.Members.Any(item => item.UserId == employee.UserId))
        {
            return (false, "Tài khoản đã có membership được liên kết với hồ sơ nhân viên khác.");
        }

        if (member is null)
        {
            member = new ProjectMember
            {
                Project = project,
                ProjectId = project.Id,
                UserId = employee.UserId,
                EmployeeId = employee.Id,
                Employee = employee,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
            project.Members.Add(member);
        }
        else
        {
            if (member.EmployeeId is not null && member.EmployeeId != employee.Id)
            {
                return (false, "Thành viên hiện có đang liên kết với hồ sơ nhân viên khác.");
            }

            if (member.UserId != employee.UserId)
            {
                return (false, "Hồ sơ nhân viên không khớp với tài khoản của thành viên hiện có.");
            }

            member.EmployeeId = employee.Id;
            member.Employee = employee;
            member.IsActive = true;
        }

        var activeCodes = member.RoleAssignments
            .Where(assignment => assignment.EndedAt is null)
            .Select(assignment => assignment.ProjectRoleCode)
            .ToHashSet(StringComparer.Ordinal);
        if (normalizedRoleCodes.Any(activeCodes.Contains))
        {
            var duplicateRole = normalizedRoleCodes.First(activeCodes.Contains);
            var roleName = roles.Single(role => role.Code == duplicateRole).Name;
            return (false, $"Nhân viên đã có vai trò {roleName} đang hoạt động trong dự án.");
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var roleCode in normalizedRoleCodes)
            {
                member.RoleAssignments.Add(new ProjectMemberRoleAssignment
                {
                    ProjectMember = member,
                    ProjectMemberId = member.Id,
                    ProjectRoleCode = roleCode,
                    StartedAt = DateTime.UtcNow
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (true, null);
        }
        catch (DbUpdateException exception) when (IsSqliteConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "Không thể thêm vai trò: thành viên đã có phân công trùng hoặc vi phạm giới hạn vai trò.");
        }
    }

    public async Task<(bool Succeeded, string? Error)> EndRoleAsync(
        int projectId,
        int memberId,
        int assignmentId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var project = await LoadProjectWithAssignmentsAsync(projectId, cancellationToken);
        if (project is null)
        {
            return (false, "Không tìm thấy dự án đang hoạt động.");
        }

        var member = project.Members.SingleOrDefault(item => item.Id == memberId);
        var assignment = member?.RoleAssignments.SingleOrDefault(item => item.Id == assignmentId);
        if (member is null || assignment is null || assignment.EndedAt is not null)
        {
            return (false, "Không tìm thấy phân công vai trò đang hoạt động.");
        }

        if (assignment.ProjectRoleCode == ProjectRoleCode.ProjectManager &&
            project.ProjectManagerId == member.UserId)
        {
            return (false, "Hãy thay đổi Project Manager chính tại chức năng cập nhật dự án.");
        }

        assignment.EndedAt = DateTime.UtcNow;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            member.IsActive = member.RoleAssignments.Any(item => item.EndedAt is null);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (true, null);
        }
        catch (DbUpdateException exception) when (IsSqliteConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "Không thể kết thúc vai trò do trạng thái dữ liệu đã thay đổi.");
        }
    }

    public async Task<(bool Succeeded, string? Error)> EndMembershipAsync(
        int projectId,
        int memberId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var project = await LoadProjectWithAssignmentsAsync(projectId, cancellationToken);
        if (project is null)
        {
            return (false, "Không tìm thấy dự án đang hoạt động.");
        }

        var member = project.Members.SingleOrDefault(item => item.Id == memberId);
        if (member is null || !member.IsActive)
        {
            return (false, "Không tìm thấy thành viên đang hoạt động.");
        }

        var now = DateTime.UtcNow;
        foreach (var assignment in member.RoleAssignments.Where(item => item.EndedAt is null))
        {
            assignment.EndedAt = now;
        }

        var isPrimaryManager = project.ProjectManagerId == member.UserId;
        if (isPrimaryManager)
        {
            project.ProjectManagerId = null;
            project.UpdatedAt = now;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            member.IsActive = false;
            await dbContext.SaveChangesAsync(cancellationToken);

            if (isPrimaryManager)
            {
                await SetCurrentManagerReasonAsync(
                    project.Id,
                    "Kết thúc toàn bộ phân công của thành viên đang là Project Manager chính.",
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, null);
        }
        catch (DbUpdateException exception) when (IsSqliteConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "Không thể kết thúc tư cách thành viên do trạng thái dữ liệu đã thay đổi.");
        }
    }

    private async Task<string?> ValidateProjectAsync(
        Project project,
        string? managerId,
        string? currentManagerId,
        CancellationToken cancellationToken,
        int? currentProjectId = null)
    {
        if (project.ExpectedEndDate < project.StartDate)
        {
            return "Ngày kết thúc dự kiến không được trước ngày bắt đầu.";
        }

        if (project.Budget < 0)
        {
            return "Ngân sách không được âm.";
        }

        var normalizedCode = project.ProjectCode.Trim();
        var duplicateCode = await dbContext.Projects.AnyAsync(
            item => item.ProjectCode == normalizedCode && item.Id != currentProjectId,
            cancellationToken);
        if (duplicateCode)
        {
            return "Mã dự án đã tồn tại, kể cả trên dự án đã lưu trữ.";
        }

        if (!string.IsNullOrWhiteSpace(managerId) &&
            !string.Equals(managerId, currentManagerId, StringComparison.Ordinal))
        {
            var activeEmployeeExists = await dbContext.Employees.AnyAsync(
                employee => employee.UserId == managerId && employee.IsActive,
                cancellationToken);
            if (!activeEmployeeExists)
            {
                return "Project Manager phải là hồ sơ nhân viên đang hoạt động.";
            }
        }

        return null;
    }

    private Task<Employee?> FindEmployeeByUserIdAsync(
        string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult<Employee?>(null);
        }

        return dbContext.Employees.SingleOrDefaultAsync(
            employee => employee.UserId == userId,
            cancellationToken);
    }

    private static ProjectMember? FindMember(Project project, Employee employee)
    {
        return project.Members.SingleOrDefault(member =>
            member.EmployeeId == employee.Id ||
            (member.EmployeeId is null && member.UserId == employee.UserId));
    }

    private async Task EndPrimaryManagerRoleAsync(
        Project project,
        CancellationToken cancellationToken)
    {
        if (project.ProjectManagerId is null)
        {
            return;
        }

        var oldManager = project.Members.SingleOrDefault(
            member => member.UserId == project.ProjectManagerId);
        if (oldManager is null)
        {
            return;
        }

        var activeManagerAssignments = oldManager.RoleAssignments
            .Where(assignment =>
                assignment.ProjectRoleCode == ProjectRoleCode.ProjectManager &&
                assignment.EndedAt is null)
            .ToArray();
        if (activeManagerAssignments.Length > 0)
        {
            var now = DateTime.UtcNow;
            foreach (var assignment in activeManagerAssignments)
            {
                assignment.EndedAt = now;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!oldManager.RoleAssignments.Any(assignment => assignment.EndedAt is null) &&
            oldManager.IsActive)
        {
            oldManager.IsActive = false;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private Task<Project?> LoadProjectWithAssignmentsAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        return dbContext.Projects
            .Include(project => project.Members)
                .ThenInclude(member => member.RoleAssignments)
            .SingleOrDefaultAsync(
                project => project.Id == projectId && !project.IsArchived,
                cancellationToken);
    }

    private async Task SetCurrentManagerReasonAsync(
        int projectId,
        string reason,
        CancellationToken cancellationToken)
    {
        var currentHistory = await dbContext.ProjectManagerHistories.SingleOrDefaultAsync(
            history => history.ProjectId == projectId && history.EndedAt == null,
            cancellationToken);
        if (currentHistory is null)
        {
            return;
        }

        currentHistory.ChangeReason = reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsSqliteConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqliteException { SqliteErrorCode: 19 })
            {
                return true;
            }
        }

        return false;
    }
}
