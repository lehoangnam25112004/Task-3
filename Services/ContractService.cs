using ConstructionProjectManagement.Data;
using ConstructionProjectManagement.Models;
using ConstructionProjectManagement.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConstructionProjectManagement.Services;

public sealed class ContractService(ApplicationDbContext dbContext) : IContractService
{
    public async Task<IReadOnlyCollection<Contract>> GetVisibleAsync(
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default,
        bool includeArchived = false)
    {
        var query = VisibleQuery(userId, isAdmin, includeArchived: true);
        if (!includeArchived)
        {
            query = query.Where(contract => !contract.IsArchived);
        }

        return await query.Include(contract => contract.ContractProjects).ThenInclude(cp => cp.Project)
            .Include(contract => contract.Amendments)
            .OrderByDescending(contract => contract.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<Contract?> GetByIdAsync(
        int id, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        return VisibleQuery(userId, isAdmin, includeArchived: true)
            .Include(contract => contract.ContractProjects).ThenInclude(cp => cp.Project)
            .Include(contract => contract.Documents)
            .Include(contract => contract.Amendments)
            .Include(contract => contract.ClientParty)
            .Include(contract => contract.ContractorParty)
            .Include(contract => contract.AuditLogs)
                .ThenInclude(log => log.PerformedBy)
            .Include(contract => contract.ValueHistories)
                .ThenInclude(history => history.RecordedBy)
            .SingleOrDefaultAsync(contract => contract.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.Contracts.AnyAsync(contract => contract.Id == id, cancellationToken);
    }

    public async Task<(bool Succeeded, string? Error, Contract? Contract)> CreateAsync(
        Contract contract, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (isAdmin) return (false, "Admin không có quyền tạo hợp đồng.", null);
        if (contract.OriginalValue < 0 || contract.CurrentValue < 0)
            return (false, "Giá trị hợp đồng không được âm.", null);
        if (contract.EndDate.HasValue && contract.EffectiveDate.HasValue && contract.EndDate < contract.EffectiveDate)
            return (false, "Ngày kết thúc không được trước ngày hiệu lực.", null);
        if (contract.ContractProjects.Count == 0)
            return (false, "Hợp đồng phải gắn ít nhất một dự án.", null);

        foreach (var link in contract.ContractProjects)
        {
            var active = await dbContext.ProjectMembers.AnyAsync(
                member => member.ProjectId == link.ProjectId && member.UserId == userId && member.IsActive,
                cancellationToken);
            if (!active) return (false, "Bạn không có quyền trên một hoặc nhiều dự án đã chọn.", null);
        }

        var number = await GenerateContractNumberAsync(cancellationToken);
        contract.ContractNumber = number;
        contract.ContractValue = contract.OriginalValue;
        contract.CurrentValue = contract.OriginalValue;
        contract.CreatedById = userId;
        contract.CreatedAt = DateTime.UtcNow;
        contract.UpdatedAt = DateTime.UtcNow;
        contract.Status = ContractStatus.Draft;
        contract.ApprovalStep = 0;

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Contracts.Add(contract);
        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return (true, null, contract);
    }

    public async Task<(bool Succeeded, string? Error)> UpdateAsync(
        Contract contract, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (isAdmin) return (false, "Admin không có quyền sửa hợp đồng.");
        var existing = await dbContext.Contracts
            .Include(item => item.ContractProjects)
            .SingleOrDefaultAsync(item => item.Id == contract.Id && !item.IsArchived, cancellationToken);
        if (existing is null) return (false, "Không tìm thấy hợp đồng.");
        if (existing.CreatedById != userId && existing.Status != ContractStatus.Draft)
            return (false, "Chỉ người tạo có thể sửa hợp đồng ở trạng thái Nháp.");
        if (existing.Status is ContractStatus.PendingApproval or ContractStatus.Completed or
            ContractStatus.Terminated or ContractStatus.Liquidated)
            return (false, "Hợp đồng đang bị khóa ở trạng thái hiện tại.");

        if (existing.Status is ContractStatus.InProgress or ContractStatus.Suspended)
        {
            existing.ProgressPercentage = contract.ProgressPercentage;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existing.Title = contract.Title;
            existing.ContractType = contract.ContractType;
            existing.ClientPartyId = contract.ClientPartyId;
            existing.ContractorPartyId = contract.ContractorPartyId;
            existing.OriginalValue = contract.OriginalValue;
            existing.CurrentValue = contract.OriginalValue;
            existing.ContractValue = contract.OriginalValue;
            existing.Currency = contract.Currency;
            existing.ScopeOfWork = contract.ScopeOfWork;
            existing.ProgressPercentage = contract.ProgressPercentage;
            existing.SignedDate = contract.SignedDate;
            existing.EffectiveDate = contract.EffectiveDate;
            existing.EndDate = contract.EndDate;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        dbContext.ContractAuditLogs.Add(new ContractAuditLog
        {
            ContractId = existing.Id,
            Action = "Updated",
            PerformedById = userId,
            Comment = "Cập nhật thông tin hợp đồng"
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> SubmitAsync(
        int contractId, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var contract = await dbContext.Contracts.SingleOrDefaultAsync(item => item.Id == contractId && !item.IsArchived, cancellationToken);
        if (contract is null) return (false, "Không tìm thấy hợp đồng.");
        if (contract.CreatedById != userId) return (false, "Chỉ người tạo hợp đồng được gửi duyệt.");
        if (contract.Status != ContractStatus.Draft) return (false, "Chỉ hợp đồng ở trạng thái Nháp mới được gửi duyệt.");
        contract.Status = ContractStatus.PendingApproval;
        contract.ApprovalStep = 1;
        contract.RejectionReason = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> ReviewAsync(
        int contractId, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var contract = await dbContext.Contracts.Include(item => item.ContractProjects)
            .SingleOrDefaultAsync(item => item.Id == contractId && !item.IsArchived, cancellationToken);
        if (contract is null) return (false, "Không tìm thấy hợp đồng.");
        if (contract.CreatedById == userId) return (false, "Người tạo không được tự kiểm tra hợp đồng.");
        if (contract.Status != ContractStatus.PendingApproval || contract.ApprovalStep != 1)
            return (false, "Hợp đồng chưa ở bước kiểm tra.");
        contract.ApprovalStep = 2;
        contract.ReviewedById = userId;
        contract.ReviewedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> ApproveAsync(
        int contractId, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var contract = await dbContext.Contracts.Include(item => item.ContractProjects)
            .SingleOrDefaultAsync(item => item.Id == contractId && !item.IsArchived, cancellationToken);
        if (contract is null) return (false, "Không tìm thấy hợp đồng.");
        if (contract.CreatedById == userId) return (false, "Người tạo không được tự phê duyệt hợp đồng.");
        if (contract.Status != ContractStatus.PendingApproval || contract.ApprovalStep != 2)
            return (false, "Hợp đồng chưa hoàn tất bước kiểm tra.");
        var roles = await dbContext.UserRoles.Where(ur => ur.UserId == userId)
            .Join(dbContext.Roles, ur => ur.RoleId, role => role.Id, (_, role) => role.Name)
            .ToListAsync(cancellationToken);
        if (!roles.Contains("Project Director")) return (false, "Chỉ Project Director được phê duyệt cuối.");
        contract.Status = ContractStatus.InProgress;
        contract.ApprovalStep = 3;
        contract.ApprovedById = userId;
        contract.ApprovedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> RejectAsync(
        int contractId, string reason, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return (false, "Vui lòng nhập lý do từ chối.");
        var contract = await dbContext.Contracts.SingleOrDefaultAsync(item => item.Id == contractId && !item.IsArchived, cancellationToken);
        if (contract is null) return (false, "Không tìm thấy hợp đồng.");
        if (contract.CreatedById == userId) return (false, "Người tạo không được tự từ chối hợp đồng.");
        contract.Status = ContractStatus.Draft;
        contract.ApprovalStep = 0;
        contract.RejectionReason = reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> ArchiveAsync(
        int contractId, string reason, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (!await HasAnyRoleAsync(userId, cancellationToken, "Project Director", "Project Manager"))
            return (false, "Bạn không có quyền lưu trữ hợp đồng.");

        var contract = await VisibleQuery(userId, isAdmin, includeArchived: true, tracking: true)
            .SingleOrDefaultAsync(item => item.Id == contractId, cancellationToken);
        if (contract is null) return (false, "Không tìm thấy hợp đồng.");
        if (contract.IsArchived) return (false, "Hợp đồng đã được lưu trữ.");

        contract.IsArchived = true;
        contract.ArchiveReason = reason?.Trim();
        contract.ArchivedById = userId;
        contract.ArchivedAt = DateTime.UtcNow;
        dbContext.ContractAuditLogs.Add(new ContractAuditLog
        {
            ContractId = contract.Id,
            Action = "Archived",
            PerformedById = userId,
            Comment = contract.ArchiveReason
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> RestoreAsync(
        int contractId, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (!await HasAnyRoleAsync(
                userId, cancellationToken, "Admin", "Project Director", "Project Manager"))
            return (false, "Bạn không có quyền khôi phục hợp đồng.");

        var contract = await VisibleQuery(userId, isAdmin, includeArchived: true, tracking: true)
            .SingleOrDefaultAsync(item => item.Id == contractId, cancellationToken);
        if (contract is null) return (false, "Không tìm thấy hợp đồng.");
        if (!contract.IsArchived) return (false, "Hợp đồng chưa được lưu trữ.");

        contract.IsArchived = false;
        contract.ArchivedById = null;
        contract.ArchivedAt = null;
        contract.ArchiveReason = null;
        dbContext.ContractAuditLogs.Add(new ContractAuditLog
        {
            ContractId = contract.Id,
            Action = "Restored",
            PerformedById = userId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error, ContractAmendment? Amendment)> CreateAmendmentAsync(
        ContractAmendment amendment, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var contract = await dbContext.Contracts
            .Include(item => item.ContractProjects)
            .SingleOrDefaultAsync(item => item.Id == amendment.ContractId && !item.IsArchived, cancellationToken);
        if (contract is null) return (false, "Không tìm thấy hợp đồng.", null);
        if (contract.Status is ContractStatus.PendingApproval or ContractStatus.Completed or
            ContractStatus.Terminated or ContractStatus.Liquidated)
            return (false, "Không thể tạo phụ lục ở trạng thái hợp đồng hiện tại.", null);
        var count = await dbContext.ContractAmendments.CountAsync(
            item => item.ContractId == contract.Id, cancellationToken);
        amendment.AmendmentNumber = $"{contract.ContractNumber}-PL{count + 1:D2}";
        amendment.CreatedById = userId;
        amendment.CreatedAt = DateTime.UtcNow;
        amendment.ApprovalStatus = AmendmentApprovalStatus.Draft;
        dbContext.ContractAmendments.Add(amendment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null, amendment);
    }

    public async Task<(bool Succeeded, string? Error)> RejectAmendmentAsync(
        int amendmentId, string reason, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return (false, "Vui lòng nhập lý do từ chối.");
        var amendment = await dbContext.ContractAmendments
            .SingleOrDefaultAsync(item => item.Id == amendmentId, cancellationToken);
        if (amendment is null) return (false, "Không tìm thấy phụ lục.");
        if (amendment.CreatedById == userId) return (false, "Người tạo không được tự từ chối phụ lục.");
        amendment.ApprovalStatus = AmendmentApprovalStatus.Draft;
        amendment.RejectionReason = reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> SubmitAmendmentAsync(
        int amendmentId, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var amendment = await dbContext.ContractAmendments
            .Include(item => item.Contract).ThenInclude(contract => contract.ContractProjects)
            .SingleOrDefaultAsync(item => item.Id == amendmentId, cancellationToken);
        if (amendment is null) return (false, "Không tìm thấy phụ lục.");
        if (amendment.CreatedById != userId) return (false, "Chỉ người tạo phụ lục được gửi duyệt.");
        if (amendment.ApprovalStatus == AmendmentApprovalStatus.PendingReview ||
            amendment.ApprovalStatus == AmendmentApprovalStatus.PendingApproval)
            return (false, "Phụ lục đã được gửi duyệt.");
        amendment.ApprovalStatus = AmendmentApprovalStatus.PendingReview;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> ReviewAmendmentAsync(
        int amendmentId, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var amendment = await dbContext.ContractAmendments
            .SingleOrDefaultAsync(item => item.Id == amendmentId, cancellationToken);
        if (amendment is null) return (false, "Không tìm thấy phụ lục.");
        if (amendment.CreatedById == userId) return (false, "Người tạo không được tự kiểm tra phụ lục.");
        if (amendment.ApprovalStatus != AmendmentApprovalStatus.PendingReview)
            return (false, "Phụ lục chưa ở bước kiểm tra.");
        amendment.ApprovalStatus = AmendmentApprovalStatus.PendingApproval;
        amendment.ReviewedById = userId;
        amendment.ReviewedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> ApproveAmendmentAsync(
        int amendmentId, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var amendment = await dbContext.ContractAmendments
            .Include(item => item.Contract).ThenInclude(contract => contract.ContractProjects)
            .SingleOrDefaultAsync(item => item.Id == amendmentId, cancellationToken);
        if (amendment is null) return (false, "Không tìm thấy phụ lục.");
        if (amendment.ApprovalStatus != AmendmentApprovalStatus.PendingApproval)
            return (false, "Phụ lục chưa ở bước chờ phê duyệt cuối.");
        if (amendment.IsApplied) return (false, "Phụ lục đã được áp dụng.");
        if (amendment.CreatedById == userId) return (false, "Người tạo không được tự phê duyệt phụ lục.");
        var approverRoles = await dbContext.UserRoles.Where(ur => ur.UserId == userId)
            .Join(dbContext.Roles, ur => ur.RoleId, role => role.Id, (_, role) => role.Name)
            .ToListAsync(cancellationToken);
        if (!approverRoles.Contains("Project Director"))
            return (false, "Chỉ Project Director được phê duyệt phụ lục.");

        var projectIds = amendment.Contract.ContractProjects.Select(item => item.ProjectId).ToList();
        var hasProject = await dbContext.ProjectMembers.AnyAsync(
            member => projectIds.Contains(member.ProjectId) && member.UserId == userId && member.IsActive,
            cancellationToken);
        if (!hasProject) return (false, "Bạn không thuộc dự án của hợp đồng.");

        amendment.ApprovalStatus = AmendmentApprovalStatus.Approved;
        amendment.ApprovedById = userId;
        amendment.ApprovedAt = DateTime.UtcNow;
        amendment.IsApplied = true;
        var contract = amendment.Contract;

        if (amendment.ValueAdjustment != 0)
        {
            var previous = contract.CurrentValue;
            contract.CurrentValue += amendment.ValueAdjustment;
            dbContext.ContractValueHistories.Add(new ContractValueHistory
            {
                ContractId = contract.Id,
                AmendmentId = amendment.Id,
                PreviousValue = previous,
                AdjustmentAmount = amendment.ValueAdjustment,
                NewValue = contract.CurrentValue,
                RecordedById = userId,
                Note = amendment.ChangeDescription
            });
        }
        if (amendment.NewExpirationDate.HasValue)
            contract.EndDate = amendment.NewExpirationDate.Value;

        dbContext.ContractAuditLogs.Add(new ContractAuditLog
        {
            ContractId = contract.Id,
            AmendmentId = amendment.Id,
            Action = "AmendmentApproved",
            PerformedById = userId,
            Comment = amendment.ChangeDescription
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return (true, null);
    }

    private async Task<string> GenerateContractNumberAsync(CancellationToken cancellationToken)
    {
        var prefix = $"HD-{DateTime.UtcNow:yyyyMM}-";
        var count = await dbContext.Contracts.CountAsync(item => item.ContractNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{count + 1:D4}";
    }

    private IQueryable<Contract> VisibleQuery(
        string userId, bool isAdmin, bool includeArchived = false, bool tracking = false)
    {
        IQueryable<Contract> query = tracking ? dbContext.Contracts : dbContext.Contracts.AsNoTracking();
        query = query
            .Include(contract => contract.ContractProjects).ThenInclude(cp => cp.Project)
            .Include(contract => contract.Project)
            .AsQueryable();
        if (!includeArchived)
        {
            query = query.Where(contract => !contract.IsArchived);
        }

        var roles = dbContext.UserRoles.Where(ur => ur.UserId == userId)
            .Join(dbContext.Roles, ur => ur.RoleId, role => role.Id, (_, role) => role.Name)
            .Select(name => name!).ToList();
        if (roles.Contains("Admin")) return query;
        if (roles.Contains("Worker")) return query.Where(contract => false);

        var user = dbContext.Users.SingleOrDefault(item => item.Id == userId);
        var organizationName = user?.OrganizationName?.Trim();
        var memberProjectIds = dbContext.ProjectMembers
            .Where(member => member.UserId == userId && member.IsActive)
            .Select(member => member.ProjectId);

        query = query.Where(contract =>
            memberProjectIds.Contains(contract.ProjectId) ||
            contract.ContractProjects.Any(cp => memberProjectIds.Contains(cp.ProjectId)));

        if (roles.Contains("Client Representative") || roles.Contains("ClientRepresentative"))
        {
            if (string.IsNullOrWhiteSpace(organizationName))
                return query.Where(contract => false);
            query = query.Where(contract =>
                contract.Status != ContractStatus.Draft &&
                contract.Status != ContractStatus.PendingApproval &&
                ((contract.ClientParty != null && contract.ClientParty.Name == organizationName) ||
                 contract.Project.ClientName == organizationName ||
                 contract.ContractProjects.Any(cp => cp.Project.ClientName == organizationName)));
        }
        else if (roles.Contains("Site Manager") || roles.Contains("SiteManager"))
            query = query.Where(contract => contract.ContractType == ContractType.Construction ||
                                            contract.ContractType == ContractType.Other);
        else if (roles.Contains("Supervising Engineer") || roles.Contains("SupervisingEngineer"))
            query = query.Where(contract => contract.ContractType == ContractType.Construction ||
                                            contract.ContractType == ContractType.DesignConsulting ||
                                            contract.ContractType == ContractType.SupervisionConsulting ||
                                            contract.ContractType == ContractType.Survey ||
                                            contract.ContractType == ContractType.TestingInspection);
        else if (roles.Contains("Materials Staff") || roles.Contains("MaterialsStaff"))
            query = query.Where(contract => contract.ContractType == ContractType.MaterialSupply);
        return query;
    }

    private Task<bool> HasAnyRoleAsync(
        string userId, CancellationToken cancellationToken, params string[] roleNames)
    {
        return dbContext.UserRoles
            .Where(userRole => userRole.UserId == userId)
            .Join(dbContext.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name)
            .AnyAsync(roleName => roleName != null && roleNames.Contains(roleName), cancellationToken);
    }
}
