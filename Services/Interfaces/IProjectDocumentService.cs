using ConstructionProjectManagement.Models;
using Microsoft.AspNetCore.Http;

namespace ConstructionProjectManagement.Services.Interfaces;

public interface IProjectDocumentService
{
    Task<(bool Succeeded, string? Error, ProjectDocument? Document)> UploadAsync(
        int projectId,
        int? contractId,
        string category,
        IFormFile file,
        string userId,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error, ProjectDocument? Document)> CreateAsync(
        int? projectId,
        int? contractId,
        int? amendmentId,
        int? categoryId,
        bool isSystemWide,
        string title,
        string? description,
        IFormFile file,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> UpdateMetadataAsync(
        ProjectDocument updatedDocument,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> UploadVersionAsync(
        int documentId,
        IFormFile file,
        string? versionNote,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(ProjectDocument? Document, string? FilePath)> GetDownloadAsync(
        int id,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<DocumentFileAccessResult> GetFileAsync(
        int id,
        int? versionNumber,
        string userId,
        bool isAdmin,
        bool preview,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProjectDocument>> GetVisibleAsync(
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default,
        bool includeArchived = false);

    Task<DocumentPage> GetPageAsync(
        string userId,
        bool isAdmin,
        DocumentSearchOptions options,
        CancellationToken cancellationToken = default);

    Task<ProjectDocument?> GetByIdAsync(
        int id,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default,
        bool includeArchived = true);

    Task<DocumentPermissionSet> GetPermissionsAsync(
        int documentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DocumentCategory>> GetCategoriesAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> SaveCategoryAsync(
        DocumentCategory category,
        string userId,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> SetCategoryActiveAsync(
        int categoryId,
        bool isActive,
        string userId,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> SubmitAsync(
        int documentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> ReviewAsync(
        int documentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> ApproveAsync(
        int documentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> RejectAsync(
        int documentId,
        string reason,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> AcquireLockAsync(
        int documentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> ReleaseLockAsync(
        int documentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> ArchiveAsync(
        int documentId,
        string? reason,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> RestoreAsync(
        int documentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}
