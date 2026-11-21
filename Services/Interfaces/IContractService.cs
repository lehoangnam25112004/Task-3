using ConstructionProjectManagement.Models;

namespace ConstructionProjectManagement.Services.Interfaces;

public interface IContractService
{
    Task<IReadOnlyCollection<Contract>> GetVisibleAsync(
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default,
        bool includeArchived = false);

    Task<Contract?> GetByIdAsync(
        int id,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error, Contract? Contract)> CreateAsync(
        Contract contract,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> UpdateAsync(
        Contract contract,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> ApproveAmendmentAsync(
        int amendmentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> SubmitAmendmentAsync(
        int amendmentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> ReviewAmendmentAsync(
        int amendmentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> SubmitAsync(
        int contractId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> ReviewAsync(
        int contractId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> ApproveAsync(
        int contractId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> RejectAsync(
        int contractId,
        string reason,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> ArchiveAsync(int contractId, string reason, string userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> RestoreAsync(int contractId, string userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error, ContractAmendment? Amendment)> CreateAmendmentAsync(ContractAmendment amendment, string userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> RejectAmendmentAsync(int amendmentId, string reason, string userId, bool isAdmin, CancellationToken cancellationToken = default);
}
