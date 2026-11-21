using ConstructionProjectManagement.Data;
using ConstructionProjectManagement.Models;
using ConstructionProjectManagement.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace ConstructionProjectManagement.Services;

public sealed class EmployeeService(ApplicationDbContext dbContext) : IEmployeeService
{
    public async Task<IReadOnlyCollection<Employee>> GetEmployeesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Employees
            .AsNoTracking()
            .Include(employee => employee.User)
            .Include(employee => employee.Department)
            .OrderBy(employee => employee.EmployeeCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Succeeded, string? Error)> SaveAsync(
        Employee employee,
        CancellationToken cancellationToken = default)
    {
        employee.EmployeeCode = employee.EmployeeCode.Trim();
        employee.Position = employee.Position?.Trim();
        employee.Phone = employee.Phone?.Trim();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var existing = employee.Id == 0
            ? null
            : await dbContext.Employees.SingleOrDefaultAsync(
                item => item.Id == employee.Id,
                cancellationToken);
        if (employee.Id != 0 && existing is null)
        {
            return (false, "Không tìm thấy nhân viên.");
        }

        var duplicateCode = await dbContext.Employees.AnyAsync(
            item => item.EmployeeCode == employee.EmployeeCode && item.Id != employee.Id,
            cancellationToken);
        if (duplicateCode)
        {
            return (false, "Mã nhân viên đã tồn tại.");
        }

        var duplicateUser = await dbContext.Employees.AnyAsync(
            item => item.UserId == employee.UserId && item.Id != employee.Id,
            cancellationToken);
        if (duplicateUser)
        {
            return (false, "Tài khoản đã được liên kết với một hồ sơ nhân viên khác.");
        }

        var userExists = await dbContext.Users.AnyAsync(
            item => item.Id == employee.UserId,
            cancellationToken);
        if (!userExists)
        {
            return (false, "Không tìm thấy tài khoản Identity được chọn.");
        }

        if (existing is not null &&
            existing.UserId != employee.UserId &&
            await dbContext.ProjectMembers.AnyAsync(
                member => member.EmployeeId == existing.Id || member.UserId == existing.UserId,
                cancellationToken))
        {
            return (false, "Không thể đổi tài khoản của nhân viên đã có lịch sử tham gia dự án.");
        }

        var employeeTerminated = existing is { IsActive: true } && !employee.IsActive;
        if (existing is null)
        {
            dbContext.Employees.Add(employee);
        }
        else
        {
            existing.EmployeeCode = employee.EmployeeCode;
            existing.UserId = employee.UserId;
            existing.DepartmentId = employee.DepartmentId;
            existing.Position = employee.Position;
            existing.Phone = employee.Phone;
            existing.HireDate = employee.HireDate;
            existing.IsActive = employee.IsActive;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            if (employeeTerminated && existing is not null)
            {
                await EndAssignmentsForTerminatedEmployeeAsync(existing, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, null);
        }
        catch (DbUpdateException exception) when (IsSqliteConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "Không thể lưu hồ sơ do mã nhân viên, tài khoản hoặc quan hệ dữ liệu đã trùng/không hợp lệ.");
        }
    }

    public async Task<IReadOnlyCollection<Department>> GetDepartmentsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Departments
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .ToListAsync(cancellationToken);
    }

    private async Task EndAssignmentsForTerminatedEmployeeAsync(
        Employee employee,
        CancellationToken cancellationToken)
    {
        var members = await dbContext.ProjectMembers
            .Include(member => member.RoleAssignments)
            .Where(member =>
                member.EmployeeId == employee.Id ||
                (member.EmployeeId == null && member.UserId == employee.UserId))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var assignment in members
                     .SelectMany(member => member.RoleAssignments)
                     .Where(assignment => assignment.EndedAt is null))
        {
            assignment.EndedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var member in members)
        {
            member.IsActive = false;
        }

        var managedProjects = await dbContext.Projects
            .Where(project => project.ProjectManagerId == employee.UserId)
            .ToListAsync(cancellationToken);
        foreach (var project in managedProjects)
        {
            project.ProjectManagerId = null;
            project.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var project in managedProjects)
        {
            var history = await dbContext.ProjectManagerHistories.SingleOrDefaultAsync(
                item => item.ProjectId == project.Id && item.EndedAt == null,
                cancellationToken);
            if (history is not null)
            {
                history.ChangeReason = "Nhân viên nghỉ việc; đã kết thúc các phân công đang hoạt động.";
            }
        }

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
