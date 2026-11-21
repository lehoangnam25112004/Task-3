using ConstructionProjectManagement.Models;
using ConstructionProjectManagement.Services.Interfaces;
using ConstructionProjectManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionProjectManagement.Controllers;

[Authorize(Roles = "Admin,Project Director,Project Manager")]
public class ProjectMembersController(
    IProjectService projectService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(int projectId, CancellationToken cancellationToken)
    {
        var authorization = await AuthorizeProjectAsync(projectId, cancellationToken);
        if (authorization is not null)
        {
            return authorization;
        }

        var model = await BuildViewModelAsync(
            projectId,
            new ProjectMemberViewModel { ProjectId = projectId },
            cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        [Bind(Prefix = "NewMember")] ProjectMemberViewModel model,
        CancellationToken cancellationToken)
    {
        var authorization = await AuthorizeProjectAsync(model.ProjectId, cancellationToken);
        if (authorization is not null)
        {
            return authorization;
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildViewModelAsync(model.ProjectId, model, cancellationToken);
            return invalidModel is null ? NotFound() : View("Index", invalidModel);
        }

        var result = await projectService.AddProjectMemberAsync(
            model.ProjectId,
            model.EmployeeId!.Value,
            model.RoleCodes,
            cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã thêm hoặc cập nhật phân công thành viên." : result.Error;
        return RedirectToAction(nameof(Index), new { projectId = model.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndRole(
        int projectId,
        int memberId,
        int assignmentId,
        CancellationToken cancellationToken)
    {
        var authorization = await AuthorizeProjectAsync(projectId, cancellationToken);
        if (authorization is not null)
        {
            return authorization;
        }

        var result = await projectService.EndRoleAsync(
            projectId,
            memberId,
            assignmentId,
            cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã kết thúc vai trò; lịch sử được giữ nguyên." : result.Error;
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndMembership(
        int projectId,
        int memberId,
        CancellationToken cancellationToken)
    {
        var authorization = await AuthorizeProjectAsync(projectId, cancellationToken);
        if (authorization is not null)
        {
            return authorization;
        }

        var result = await projectService.EndMembershipAsync(
            projectId,
            memberId,
            cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã kết thúc toàn bộ phân công của thành viên." : result.Error;
        return RedirectToAction(nameof(Index), new { projectId });
    }

    private async Task<IActionResult?> AuthorizeProjectAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        var canManageAllProjects =
            User.IsInRole("Admin") || User.IsInRole("Project Director");
        var canManage = await projectService.CanManageMembersAsync(
            projectId,
            currentUser.Id,
            canManageAllProjects,
            cancellationToken);
        return canManage ? null : Forbid();
    }

    private async Task<ProjectMemberManagementViewModel?> BuildViewModelAsync(
        int projectId,
        ProjectMemberViewModel newMember,
        CancellationToken cancellationToken)
    {
        var project = await projectService.GetProjectMembersAsync(projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        return new ProjectMemberManagementViewModel
        {
            Project = project,
            AvailableEmployees = await projectService.GetAssignableEmployeesAsync(
                projectId,
                cancellationToken),
            AvailableRoles = await projectService.GetProjectRolesAsync(cancellationToken),
            NewMember = newMember
        };
    }
}
