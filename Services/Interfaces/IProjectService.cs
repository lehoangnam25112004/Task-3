using ConstructionProjectManagement.Models;

namespace ConstructionProjectManagement.Services.Interfaces;

public interface IProjectService
{
    Task<IReadOnlyCollection<Project>> GetVisibleProjectsAsync(
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<Project?> GetByIdAsync(
        int id,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<Project?> GetProjectMembersAsync(
        int projectId,
        CancellationToken cancellationToken = default);

    Task<bool> CanManageMembersAsync(
        int projectId,
        string userId,
        bool canManageAllProjects,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Employee>> GetActiveEmployeesAsync(
        string? includeUserId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Employee>> GetAssignableEmployeesAsync(
        int projectId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProjectRole>> GetProjectRolesAsync(
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error, Project? Project)> CreateAsync(
        Project project,
        string? managerId,
        string? managerChangeReason,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> UpdateAsync(
        Project project,
        string? managerId,
        string? managerChangeReason,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> AddProjectMemberAsync(
        int projectId,
        int employeeId,
        IReadOnlyCollection<string> roleCodes,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> EndRoleAsync(
        int projectId,
        int memberId,
        int assignmentId,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> EndMembershipAsync(
        int projectId,
        int memberId,
        CancellationToken cancellationToken = default);
}
