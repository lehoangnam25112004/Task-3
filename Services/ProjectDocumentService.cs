using System.Security.Cryptography;
using System.Text.Json;
using ConstructionProjectManagement.Data;
using ConstructionProjectManagement.Helpers;
using ConstructionProjectManagement.Models;
using ConstructionProjectManagement.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ConstructionProjectManagement.Services;

public sealed class ProjectDocumentService : IProjectDocumentService
{
    private static readonly HashSet<string> ArchiveRoles =
        new(["Admin", "ProjectDirector", "ProjectManager"], StringComparer.Ordinal);

    private readonly ApplicationDbContext dbContext;
    private readonly IWebHostEnvironment environment;
    private readonly IHttpContextAccessor? httpContextAccessor;

    public ProjectDocumentService(
        ApplicationDbContext dbContext,
        IWebHostEnvironment environment,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        this.dbContext = dbContext;
        this.environment = environment;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyCollection<ProjectDocument>> GetVisibleAsync(
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default,
        bool includeArchived = false)
    {
        var query = await VisibleQueryAsync(
            userId, isAdmin, includeArchived, tracking: false, cancellationToken);
        return await query
            .OrderByDescending(item => item.UploadedAt)
            .ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentPage> GetPageAsync(
        string userId,
        bool isAdmin,
        DocumentSearchOptions options,
        CancellationToken cancellationToken = default)
    {
        var query = await VisibleQueryAsync(
            userId, isAdmin, options.ShowArchived, tracking: false, cancellationToken);
        query = query.Where(item => item.IsArchived == options.ShowArchived);

        if (options.ProjectId.HasValue)
            query = query.Where(item => item.ProjectId == options.ProjectId.Value);
        if (options.CategoryId.HasValue)
            query = query.Where(item => item.CategoryId == options.CategoryId.Value);
        if (options.ContractId.HasValue)
            query = query.Where(item => item.ContractId == options.ContractId.Value);
        if (options.ApprovalStatus.HasValue)
            query = query.Where(item => item.ApprovalStatus == options.ApprovalStatus.Value);
        if (!string.IsNullOrWhiteSpace(options.UploadedById))
            query = query.Where(item => item.UploadedById == options.UploadedById);
        if (options.UploadedFrom.HasValue)
            query = query.Where(item => item.UploadedAt >= options.UploadedFrom.Value.Date);
        if (options.UploadedTo.HasValue)
        {
            var endExclusive = options.UploadedTo.Value.Date.AddDays(1);
            query = query.Where(item => item.UploadedAt < endExclusive);
        }
        if (!string.IsNullOrWhiteSpace(options.Keyword))
        {
            var keyword = options.Keyword.Trim();
            query = query.Where(item =>
                item.DocumentCode.Contains(keyword) ||
                item.Title.Contains(keyword) ||
                (item.Description != null && item.Description.Contains(keyword)));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var pageSize = Math.Clamp(options.PageSize, 10, 100);
        var pageCount = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        var page = Math.Clamp(options.Page, 1, pageCount);
        var documents = await query
            .Include(item => item.CategoryEntity)
            .Include(item => item.Project)
            .Include(item => item.Contract)
            .Include(item => item.UploadedBy)
            .OrderByDescending(item => item.UploadedAt)
            .ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new DocumentPage(documents, page, pageSize, totalItems);
    }

    public async Task<ProjectDocument?> GetByIdAsync(
        int id,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default,
        bool includeArchived = true)
    {
        var query = await VisibleQueryAsync(
            userId, isAdmin, includeArchived, tracking: false, cancellationToken);
        return await query
            .Include(item => item.Contract)
            .Include(item => item.Amendment)
            .Include(item => item.Versions)
                .ThenInclude(version => version.UploadedBy)
            .Include(item => item.AuditLogs)
                .ThenInclude(log => log.PerformedBy)
            .Include(item => item.ApprovalLogs)
                .ThenInclude(log => log.Actor)
            .Include(item => item.EditLock)
                .ThenInclude(lockRecord => lockRecord!.LockedBy)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        => dbContext.ProjectDocuments.AnyAsync(item => item.Id == id, cancellationToken);

    public async Task<DocumentPermissionSet> GetPermissionsAsync(
        int documentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var document = await GetByIdAsync(documentId, userId, isAdmin, cancellationToken);
        if (document is null) return new DocumentPermissionSet();

        var roles = await GetRoleCodesAsync(userId, cancellationToken);
        var role = ResolveRole(roles);
        var isAdminRole = roles.Contains("Admin");
        var archiveScopeAllowed = await CanManageArchiveInScopeAsync(document, userId, role, cancellationToken);
        var canArchive = !document.IsArchived && archiveScopeAllowed;
        var canRestore = document.IsArchived && archiveScopeAllowed;
        var editLockAvailable = document.EditLock is null ||
            document.EditLock.IsReleased ||
            document.EditLock.ExpiresAt <= DateTime.UtcNow ||
            document.EditLock.LockedByUserId == userId;
        var canEdit = !document.IsArchived &&
            editLockAvailable &&
            document.ApprovalStatus is not (DocumentApprovalStatus.PendingReview or DocumentApprovalStatus.PendingApproval) &&
            await CanEditDocumentAsync(document, userId, isAdminRole, cancellationToken);
        var levels = await GetApprovalLevelsAsync(document, cancellationToken);
        var canSubmit = canEdit && levels > 0 &&
            document.ApprovalStatus is DocumentApprovalStatus.Draft or DocumentApprovalStatus.Rejected;
        var isIndependentOfWorkflow = document.UploadedById != userId &&
            document.Versions.OrderByDescending(item => item.VersionNumber).FirstOrDefault()?.UploadedById != userId;
        var canReview = !document.IsArchived &&
            document.ApprovalStatus == DocumentApprovalStatus.PendingReview &&
            CanReviewRole(role, GetCategoryCode(document), levels) &&
            isIndependentOfWorkflow;
        var canApprove = !document.IsArchived &&
            document.ApprovalStatus == DocumentApprovalStatus.PendingApproval &&
            CanApproveRole(role, GetCategoryCode(document), levels, document.CurrentApprovalStep) &&
            isIndependentOfWorkflow;

        return new DocumentPermissionSet
        {
            CanDownload = true,
            CanEdit = canEdit,
            CanSubmit = canSubmit,
            CanReview = canReview,
            CanApprove = canApprove,
            CanArchive = canArchive,
            CanRestore = canRestore
        };
    }

    public async Task<IReadOnlyCollection<DocumentCategory>> GetCategoriesAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.DocumentCategories.AsNoTracking();
        if (!includeInactive)
            query = query.Where(item => item.IsActive);
        return await query.OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Succeeded, string? Error)> SaveCategoryAsync(
        DocumentCategory category,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!await HasRoleAsync(userId, "Admin", cancellationToken))
            return (false, "Chỉ Admin được quản trị danh mục tài liệu.");
        if (string.IsNullOrWhiteSpace(category.Code) || string.IsNullOrWhiteSpace(category.Name))
            return (false, "Mã và tên danh mục là bắt buộc.");
        if (category.ApprovalLevels is < 0 or > 2)
            return (false, "Số cấp phê duyệt chỉ được là 0, 1 hoặc 2.");

        var extensions = NormalizeAllowedExtensions(category.AllowedExtensions);
        if (extensions.Error is not null) return (false, extensions.Error);

        var code = category.Code.Trim();
        var duplicate = await dbContext.DocumentCategories.AnyAsync(
            item => item.Code == code && item.Id != category.Id, cancellationToken);
        if (duplicate) return (false, "Mã danh mục đã tồn tại.");

        var now = DateTime.UtcNow;
        if (category.Id == 0)
        {
            category.Code = code;
            category.Name = category.Name.Trim();
            category.Description = category.Description?.Trim();
            category.AllowedExtensions = extensions.Value;
            category.CreatedAt = now;
            dbContext.DocumentCategories.Add(category);
        }
        else
        {
            var existing = await dbContext.DocumentCategories
                .SingleOrDefaultAsync(item => item.Id == category.Id, cancellationToken);
            if (existing is null) return (false, "Không tìm thấy danh mục.");
            existing.Code = code;
            existing.Name = category.Name.Trim();
            existing.Description = category.Description?.Trim();
            existing.AllowedExtensions = extensions.Value;
            existing.ApprovalLevels = category.ApprovalLevels;
            existing.DisplayOrder = category.DisplayOrder;
            existing.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> SetCategoryActiveAsync(
        int categoryId,
        bool isActive,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!await HasRoleAsync(userId, "Admin", cancellationToken))
            return (false, "Chỉ Admin được quản trị danh mục tài liệu.");
        var category = await dbContext.DocumentCategories
            .SingleOrDefaultAsync(item => item.Id == categoryId, cancellationToken);
        if (category is null) return (false, "Không tìm thấy danh mục.");
        category.IsActive = isActive;
        category.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error, ProjectDocument? Document)> UploadAsync(
        int projectId,
        int? contractId,
        string category,
        IFormFile file,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var categoryEntity = await dbContext.DocumentCategories.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Code == category && item.IsActive, cancellationToken);
        if (categoryEntity is null)
            return (false, "Danh mục tài liệu không hợp lệ hoặc đã ngừng hoạt động.", null);

        return await CreateAsync(
            projectId,
            contractId,
            null,
            categoryEntity.Id,
            false,
            Path.GetFileName(file.FileName),
            null,
            file,
            userId,
            false,
            cancellationToken);
    }

    public async Task<(bool Succeeded, string? Error, ProjectDocument? Document)> CreateAsync(
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
        CancellationToken cancellationToken = default)
    {
        var category = categoryId.HasValue
            ? await dbContext.DocumentCategories.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == categoryId.Value, cancellationToken)
            : null;
        if (category is null || !category.IsActive)
            return (false, "Danh mục tài liệu không hợp lệ hoặc đã ngừng hoạt động.", null);
        var validation = FileValidationHelper.Validate(file, category.AllowedExtensions);
        if (!validation.Valid) return (false, validation.Error, null);
        if (string.IsNullOrWhiteSpace(title))
            return (false, "Vui lòng nhập tiêu đề tài liệu.", null);
        if (!await CanCreateInCategoryAsync(userId, category.Code, projectId, isSystemWide, cancellationToken))
            return (false, "Bạn không có quyền tạo tài liệu này trong phạm vi đã chọn.", null);
        var linkError = await ValidateLinksAsync(projectId, contractId, amendmentId, isSystemWide, cancellationToken);
        if (linkError is not null) return (false, linkError, null);

        var now = DateTime.UtcNow;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var relativeDirectory = $"Documents/{now:yyyy}/{now:MM}";
        var storageDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "Documents", now.ToString("yyyy"), now.ToString("MM"));
        Directory.CreateDirectory(storageDirectory);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(storageDirectory, storedFileName);
        var relativePath = $"{relativeDirectory}/{storedFileName}";

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var documentCode = await GenerateDocumentCodeAsync(projectId, isSystemWide, now, cancellationToken);
            await using (var destination = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await file.CopyToAsync(destination, cancellationToken);
            var hash = await HashFileAsync(fullPath, cancellationToken);
            var contentType = FileValidationHelper.GetContentType(extension);
            var approvalStatus = category.ApprovalLevels == 0
                ? DocumentApprovalStatus.Approved
                : DocumentApprovalStatus.Draft;
            var document = new ProjectDocument
            {
                ProjectId = projectId,
                ContractId = contractId,
                AmendmentId = amendmentId,
                IsSystemWide = isSystemWide,
                DocumentCode = documentCode,
                Title = title.Trim(),
                Description = description?.Trim(),
                CategoryId = category.Id,
                LegacyCategory = category.Code,
                Category = category.Code,
                FileName = Path.GetFileName(file.FileName),
                StoredFileName = storedFileName,
                ContentType = contentType,
                FileSize = file.Length,
                UploadedById = userId,
                CreatedByUserId = userId,
                UploadedAt = now,
                CurrentVersionNumber = 1,
                CurrentApprovalStep = 0,
                ApprovalStatus = approvalStatus
            };
            dbContext.ProjectDocuments.Add(document);
            await dbContext.SaveChangesAsync(cancellationToken);

            var version = new DocumentVersion
            {
                DocumentId = document.Id,
                VersionNumber = 1,
                OriginalFileName = document.FileName,
                StoredFileName = storedFileName,
                RelativeStoragePath = relativePath,
                FileExtension = extension,
                ContentType = contentType,
                FileSize = file.Length,
                FileSizeBytes = file.Length,
                Sha256Hash = hash,
                ApprovalStatus = approvalStatus,
                UploadedById = userId,
                CreatedByUserId = userId,
                UploadedAt = now,
                CreatedAt = now,
                IsCurrent = true
            };
            dbContext.DocumentVersions.Add(version);
            dbContext.DocumentAuditLogs.Add(await CreateAuditAsync(
                document, "Created", userId, cancellationToken,
                newValues: JsonSerializer.Serialize(new
                {
                    document.DocumentCode,
                    document.Title,
                    document.CategoryId,
                    document.ProjectId,
                    document.IsSystemWide,
                    document.CurrentVersionNumber
                })));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (true, null, document);
        }
        catch
        {
            if (File.Exists(fullPath)) File.Delete(fullPath);
            throw;
        }
    }

    public async Task<(bool Succeeded, string? Error)> UpdateMetadataAsync(
        ProjectDocument updatedDocument,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var document = await GetTrackedDocumentAsync(
            updatedDocument.Id, userId, isAdmin, cancellationToken);
        if (document is null) return (false, "Không tìm thấy tài liệu trong phạm vi được cấp.");
        if (document.IsArchived) return (false, "Không thể sửa tài liệu đã lưu trữ.");
        if (document.ApprovalStatus is DocumentApprovalStatus.PendingReview or DocumentApprovalStatus.PendingApproval)
            return (false, "Không thể sửa tài liệu đang chờ kiểm tra hoặc phê duyệt.");
        if (!await CanEditDocumentAsync(document, userId, isAdmin, cancellationToken))
            return (false, "Bạn không có quyền sửa tài liệu này.");
        if (!await HasOwnedActiveLockAsync(document.Id, userId, cancellationToken))
            return (false, "Khóa chỉnh sửa không còn hiệu lực. Hãy mở lại màn hình sửa để lấy khóa mới.");
        if (string.IsNullOrWhiteSpace(updatedDocument.Title))
            return (false, "Vui lòng nhập tiêu đề tài liệu.");

        var category = updatedDocument.CategoryId.HasValue
            ? await dbContext.DocumentCategories.SingleOrDefaultAsync(
                item => item.Id == updatedDocument.CategoryId.Value, cancellationToken)
            : null;
        if (category is null)
            return (false, "Danh mục tài liệu không hợp lệ.");
        if (category.Id != document.CategoryId && !category.IsActive)
            return (false, "Không thể chuyển tài liệu sang danh mục đã ngừng hoạt động.");
        if (!CanCreateCategory(ResolveRole(await GetRoleCodesAsync(userId, cancellationToken)), category.Code))
            return (false, "Danh mục tài liệu nằm ngoài phạm vi chuyên môn của bạn.");
        var linkError = await ValidateLinksAsync(
            updatedDocument.ProjectId,
            updatedDocument.ContractId,
            updatedDocument.AmendmentId,
            updatedDocument.IsSystemWide,
            cancellationToken);
        if (linkError is not null) return (false, linkError);
        if (updatedDocument.IsSystemWide &&
            !await HasRoleAsync(userId, "Admin", cancellationToken))
            return (false, "Chỉ Admin được quản lý tài liệu dùng chung toàn hệ thống.");
        if (updatedDocument.ProjectId.HasValue &&
            !await IsProjectMemberAsync(updatedDocument.ProjectId.Value, userId, isAdmin, cancellationToken))
            return (false, "Bạn không còn là thành viên hoạt động của dự án.");

        var oldValues = JsonSerializer.Serialize(new
        {
            document.Title,
            document.Description,
            document.CategoryId,
            document.ProjectId,
            document.ContractId,
            document.AmendmentId,
            document.IsSystemWide
        });
        document.Title = updatedDocument.Title.Trim();
        document.Description = updatedDocument.Description?.Trim();
        document.CategoryId = category.Id;
        document.LegacyCategory = category.Code;
        document.Category = category.Code;
        document.ProjectId = updatedDocument.ProjectId;
        document.ContractId = updatedDocument.ContractId;
        document.AmendmentId = updatedDocument.AmendmentId;
        document.IsSystemWide = updatedDocument.IsSystemWide;
        document.UpdatedByUserId = userId;
        document.UpdatedAt = DateTime.UtcNow;

        var lockRecord = await dbContext.DocumentEditLocks
            .SingleAsync(item => item.DocumentId == document.Id, cancellationToken);
        lockRecord.IsReleased = true;
        dbContext.DocumentAuditLogs.Add(await CreateAuditAsync(
            document,
            "MetadataUpdated",
            userId,
            cancellationToken,
            oldValues,
            JsonSerializer.Serialize(new
            {
                document.Title,
                document.Description,
                document.CategoryId,
                document.ProjectId,
                document.ContractId,
                document.AmendmentId,
                document.IsSystemWide
            })));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> UploadVersionAsync(
        int documentId,
        IFormFile file,
        string? versionNote,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var documentPreview = await GetByIdAsync(documentId, userId, isAdmin, cancellationToken);
        if (documentPreview is null) return (false, "Không tìm thấy tài liệu trong phạm vi được cấp.");
        if (documentPreview.IsArchived) return (false, "Không thể tải phiên bản mới cho tài liệu đã lưu trữ.");
        if (documentPreview.ApprovalStatus is DocumentApprovalStatus.PendingReview or DocumentApprovalStatus.PendingApproval)
            return (false, "Không thể tải phiên bản mới khi tài liệu đang chờ kiểm tra hoặc phê duyệt.");
        var categoryPreview = documentPreview.CategoryEntity;
        if (categoryPreview is null || !categoryPreview.IsActive)
            return (false, "Danh mục tài liệu không còn hoạt động.");
        var validation = FileValidationHelper.Validate(file, categoryPreview.AllowedExtensions);
        if (!validation.Valid) return (false, validation.Error);
        if (!await CanEditDocumentAsync(documentPreview, userId, isAdmin, cancellationToken))
            return (false, "Bạn không có quyền tải phiên bản cho tài liệu này.");

        var now = DateTime.UtcNow;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var relativeDirectory = $"Documents/{now:yyyy}/{now:MM}";
        var storageDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "Documents", now.ToString("yyyy"), now.ToString("MM"));
        Directory.CreateDirectory(storageDirectory);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(storageDirectory, storedFileName);

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var document = await GetTrackedDocumentAsync(documentId, userId, isAdmin, cancellationToken);
            if (document is null || document.IsArchived)
                return (false, "Không tìm thấy tài liệu đang hoạt động trong phạm vi được cấp.");
            if (!await HasOwnedActiveLockAsync(documentId, userId, cancellationToken))
                return (false, "Khóa chỉnh sửa không còn hiệu lực. Hãy mở lại màn hình sửa để lấy khóa mới.");

            await using (var destination = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await file.CopyToAsync(destination, cancellationToken);
            var hash = await HashFileAsync(fullPath, cancellationToken);
            var currentVersions = await dbContext.DocumentVersions
                .Where(item => item.DocumentId == documentId)
                .ToListAsync(cancellationToken);
            foreach (var oldVersion in currentVersions)
                oldVersion.IsCurrent = false;

            var versionNumber = currentVersions.Count == 0
                ? document.CurrentVersionNumber + 1
                : currentVersions.Max(item => item.VersionNumber) + 1;
            var contentType = FileValidationHelper.GetContentType(extension);
            var approvalStatus = categoryPreview.ApprovalLevels == 0
                ? DocumentApprovalStatus.Approved
                : DocumentApprovalStatus.Draft;
            var version = new DocumentVersion
            {
                DocumentId = document.Id,
                VersionNumber = versionNumber,
                OriginalFileName = Path.GetFileName(file.FileName),
                StoredFileName = storedFileName,
                RelativeStoragePath = $"{relativeDirectory}/{storedFileName}",
                FileExtension = extension,
                ContentType = contentType,
                FileSize = file.Length,
                FileSizeBytes = file.Length,
                Sha256Hash = hash,
                ChangeNote = versionNote?.Trim(),
                VersionNote = versionNote?.Trim(),
                ApprovalStatus = approvalStatus,
                UploadedById = userId,
                CreatedByUserId = userId,
                UploadedAt = now,
                CreatedAt = now,
                IsCurrent = true
            };
            dbContext.DocumentVersions.Add(version);

            var oldValues = JsonSerializer.Serialize(new
            {
                document.CurrentVersionNumber,
                document.FileName,
                document.FileSize,
                document.ApprovalStatus
            });
            document.CurrentVersionNumber = versionNumber;
            document.FileName = version.OriginalFileName;
            document.StoredFileName = version.StoredFileName;
            document.ContentType = contentType;
            document.FileSize = file.Length;
            document.ApprovalStatus = approvalStatus;
            document.CurrentApprovalStep = 0;
            document.SubmittedByUserId = null;
            document.ApprovedById = null;
            document.ApprovedAt = null;
            document.RejectionReason = null;
            document.UpdatedByUserId = userId;
            document.UpdatedAt = now;
            var lockRecord = await dbContext.DocumentEditLocks
                .SingleAsync(item => item.DocumentId == documentId, cancellationToken);
            lockRecord.IsReleased = true;
            dbContext.DocumentAuditLogs.Add(await CreateAuditAsync(
                document,
                "VersionUploaded",
                userId,
                cancellationToken,
                oldValues,
                JsonSerializer.Serialize(new
                {
                    document.CurrentVersionNumber,
                    document.FileName,
                    document.FileSize,
                    document.ApprovalStatus
                }),
                versionNumber: versionNumber));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (true, null);
        }
        catch (Exception operationException)
        {
            if (File.Exists(fullPath)) File.Delete(fullPath);
            try
            {
                await ReleaseLockAsync(documentId, userId, isAdmin, CancellationToken.None);
            }
            catch (Exception releaseException)
            {
                throw new AggregateException(operationException, releaseException);
            }
            throw;
        }
    }

    public async Task<(ProjectDocument? Document, string? FilePath)> GetDownloadAsync(
        int id,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var result = await GetFileAsync(id, null, userId, isAdmin, preview: false, cancellationToken);
        return (result.Document, result.FilePath);
    }

    public async Task<DocumentFileAccessResult> GetFileAsync(
        int id,
        int? versionNumber,
        string userId,
        bool isAdmin,
        bool preview,
        CancellationToken cancellationToken = default)
    {
        var document = await GetByIdAsync(id, userId, isAdmin, cancellationToken);
        if (document is null) return new(null, null, null, "Không tìm thấy tài liệu trong phạm vi được cấp.");
        var versionQuery = dbContext.DocumentVersions.AsNoTracking()
            .Where(item => item.DocumentId == id);
        var version = versionNumber.HasValue
            ? await versionQuery.SingleOrDefaultAsync(
                item => item.VersionNumber == versionNumber.Value, cancellationToken)
            : await versionQuery.SingleOrDefaultAsync(item => item.IsCurrent, cancellationToken);
        if (version is null) return new(document, null, null, "Không tìm thấy phiên bản tài liệu.");

        var roles = await GetRoleCodesAsync(userId, cancellationToken);
        var role = ResolveRole(roles);
        if ((role is "Worker" or "ClientRepresentative") &&
            (document.ApprovalStatus != DocumentApprovalStatus.Approved ||
             version.ApprovalStatus != DocumentApprovalStatus.Approved))
            return new(document, null, null, "Bạn không có quyền tải phiên bản chưa được phê duyệt.");
        if (preview && version.FileExtension is not (".pdf" or ".png" or ".jpg" or ".jpeg"))
            return new(document, version, null, "Định dạng này không hỗ trợ xem trước; vui lòng tải tệp xuống.");

        var storageRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data"));
        var filePath = Path.GetFullPath(Path.Combine(
            storageRoot,
            version.RelativeStoragePath.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = storageRoot.EndsWith(Path.DirectorySeparatorChar)
            ? storageRoot
            : storageRoot + Path.DirectorySeparatorChar;
        if (!filePath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            return new(document, version, null, "Đường dẫn lưu trữ không hợp lệ.");
        if (!File.Exists(filePath))
            return new(document, version, null, "Không tìm thấy tệp vật lý của phiên bản này.");

        dbContext.DocumentAuditLogs.Add(await CreateAuditAsync(
            document,
            preview ? "Previewed" : "Downloaded",
            userId,
            cancellationToken,
            versionNumber: version.VersionNumber));
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(document, version, filePath, null);
    }

    public async Task<(bool Succeeded, string? Error)> SubmitAsync(
        int documentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var document = await GetTrackedDocumentAsync(documentId, userId, isAdmin, cancellationToken);
        if (document is null) return (false, "Không tìm thấy tài liệu trong phạm vi được cấp.");
        if (document.IsArchived) return (false, "Không thể gửi duyệt tài liệu đã lưu trữ.");
        if (!await CanEditDocumentAsync(document, userId, isAdmin, cancellationToken))
            return (false, "Bạn không có quyền gửi duyệt tài liệu này.");
        var category = await GetCategoryAsync(document, cancellationToken);
        if (category.ApprovalLevels == 0)
            return (false, "Danh mục này không yêu cầu phê duyệt.");
        if (document.ApprovalStatus is not (DocumentApprovalStatus.Draft or DocumentApprovalStatus.Rejected))
            return (false, "Chỉ tài liệu Nháp hoặc Bị từ chối mới được gửi duyệt.");

        var wasRejected = await dbContext.DocumentApprovalLogs.AnyAsync(
            item => item.DocumentId == document.Id && item.Action == "Rejected", cancellationToken);
        var action = wasRejected ? "Resubmitted" : "Submitted";
        document.ApprovalStatus = category.ApprovalLevels == 2
            ? DocumentApprovalStatus.PendingReview
            : DocumentApprovalStatus.PendingApproval;
        document.CurrentApprovalStep = 1;
        document.SubmittedByUserId = userId;
        document.RejectionReason = null;
        document.UpdatedByUserId = userId;
        document.UpdatedAt = DateTime.UtcNow;
        var version = await GetCurrentVersionAsync(document.Id, cancellationToken);
        if (version is not null) version.ApprovalStatus = document.ApprovalStatus;
        dbContext.DocumentApprovalLogs.Add(await CreateApprovalLogAsync(
            document, version, 0, action, userId, null, cancellationToken));
        dbContext.DocumentAuditLogs.Add(await CreateAuditAsync(
            document, action, userId, cancellationToken, versionNumber: document.CurrentVersionNumber));
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> ReviewAsync(
        int documentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var document = await GetTrackedDocumentAsync(documentId, userId, isAdmin, cancellationToken);
        if (document is null) return (false, "Không tìm thấy tài liệu trong phạm vi được cấp.");
        if (document.IsArchived) return (false, "Không thể kiểm tra tài liệu đã lưu trữ.");
        var category = await GetCategoryAsync(document, cancellationToken);
        if (category.ApprovalLevels != 2 ||
            document.ApprovalStatus != DocumentApprovalStatus.PendingReview ||
            document.CurrentApprovalStep != 1)
            return (false, "Tài liệu không ở bước kiểm tra cấp 1.");
        if (!CanReviewRole(
                ResolveRole(await GetRoleCodesAsync(userId, cancellationToken)), category.Code, category.ApprovalLevels))
            return (false, "Bạn không có quyền kiểm tra danh mục tài liệu này.");
        if (await IsSelfApprovalAsync(document, userId, cancellationToken))
            return (false, "Người tạo, người gửi hoặc người tạo phiên bản hiện tại không được tự kiểm tra.");

        var version = await GetCurrentVersionAsync(document.Id, cancellationToken);
        document.ApprovalStatus = DocumentApprovalStatus.PendingApproval;
        document.CurrentApprovalStep = 2;
        document.UpdatedByUserId = userId;
        document.UpdatedAt = DateTime.UtcNow;
        if (version is not null) version.ApprovalStatus = DocumentApprovalStatus.PendingApproval;
        dbContext.DocumentApprovalLogs.Add(await CreateApprovalLogAsync(
            document, version, 1, "ApprovedStep1", userId, null, cancellationToken));
        dbContext.DocumentAuditLogs.Add(await CreateAuditAsync(
            document, "Reviewed", userId, cancellationToken, versionNumber: document.CurrentVersionNumber));
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> ApproveAsync(
        int documentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var document = await GetTrackedDocumentAsync(documentId, userId, isAdmin, cancellationToken);
        if (document is null) return (false, "Không tìm thấy tài liệu trong phạm vi được cấp.");
        if (document.IsArchived) return (false, "Không thể phê duyệt tài liệu đã lưu trữ.");
        var category = await GetCategoryAsync(document, cancellationToken);
        if (document.ApprovalStatus != DocumentApprovalStatus.PendingApproval)
            return (false, "Tài liệu không ở trạng thái chờ phê duyệt.");
        if (!CanApproveRole(
                ResolveRole(await GetRoleCodesAsync(userId, cancellationToken)),
                category.Code,
                category.ApprovalLevels,
                document.CurrentApprovalStep))
            return (false, "Bạn không có quyền phê duyệt ở bước hiện tại.");
        if (await IsSelfApprovalAsync(document, userId, cancellationToken))
            return (false, "Người tạo, người gửi hoặc người tạo phiên bản hiện tại không được tự phê duyệt.");

        var version = await GetCurrentVersionAsync(document.Id, cancellationToken);
        document.ApprovalStatus = DocumentApprovalStatus.Approved;
        document.ApprovedById = userId;
        document.ApprovedAt = DateTime.UtcNow;
        document.CurrentApprovalStep = category.ApprovalLevels;
        document.UpdatedByUserId = userId;
        document.UpdatedAt = DateTime.UtcNow;
        if (version is not null) version.ApprovalStatus = DocumentApprovalStatus.Approved;
        dbContext.DocumentApprovalLogs.Add(await CreateApprovalLogAsync(
            document,
            version,
            document.CurrentApprovalStep,
            "ApprovedFinal",
            userId,
            null,
            cancellationToken));
        dbContext.DocumentAuditLogs.Add(await CreateAuditAsync(
            document, "Approved", userId, cancellationToken, versionNumber: document.CurrentVersionNumber));
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> RejectAsync(
        int documentId,
        string reason,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return (false, "Vui lòng nhập lý do từ chối.");
        var document = await GetTrackedDocumentAsync(documentId, userId, isAdmin, cancellationToken);
        if (document is null) return (false, "Không tìm thấy tài liệu trong phạm vi được cấp.");
        if (document.IsArchived) return (false, "Không thể từ chối tài liệu đã lưu trữ.");
        var category = await GetCategoryAsync(document, cancellationToken);
        if (document.ApprovalStatus is not (
                DocumentApprovalStatus.PendingReview or DocumentApprovalStatus.PendingApproval))
            return (false, "Tài liệu không ở bước có thể từ chối.");
        var role = ResolveRole(await GetRoleCodesAsync(userId, cancellationToken));
        var canReject = document.ApprovalStatus == DocumentApprovalStatus.PendingReview
            ? CanReviewRole(role, category.Code, category.ApprovalLevels)
            : CanApproveRole(role, category.Code, category.ApprovalLevels, document.CurrentApprovalStep);
        if (!canReject)
            return (false, "Bạn không có quyền từ chối danh mục tài liệu này.");
        if (await IsSelfApprovalAsync(document, userId, cancellationToken))
            return (false, "Người tạo, người gửi hoặc người tạo phiên bản hiện tại không được tự từ chối.");

        var version = await GetCurrentVersionAsync(document.Id, cancellationToken);
        document.ApprovalStatus = DocumentApprovalStatus.Rejected;
        document.RejectionReason = reason.Trim();
        document.UpdatedByUserId = userId;
        document.UpdatedAt = DateTime.UtcNow;
        if (version is not null) version.ApprovalStatus = DocumentApprovalStatus.Rejected;
        dbContext.DocumentApprovalLogs.Add(await CreateApprovalLogAsync(
            document,
            version,
            document.CurrentApprovalStep,
            "Rejected",
            userId,
            reason.Trim(),
            cancellationToken));
        dbContext.DocumentAuditLogs.Add(await CreateAuditAsync(
            document,
            "Rejected",
            userId,
            cancellationToken,
            reason: reason.Trim(),
            versionNumber: document.CurrentVersionNumber));
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> AcquireLockAsync(
        int documentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var document = await GetByIdAsync(documentId, userId, isAdmin, cancellationToken);
        if (document is null) return (false, "Không tìm thấy tài liệu trong phạm vi được cấp.");
        if (document.IsArchived) return (false, "Tài liệu đã lưu trữ và không thể chỉnh sửa.");
        if (document.ApprovalStatus is DocumentApprovalStatus.PendingReview or DocumentApprovalStatus.PendingApproval)
            return (false, "Không thể chỉnh sửa tài liệu đang chờ kiểm tra hoặc phê duyệt.");
        if (!await CanEditDocumentAsync(document, userId, isAdmin, cancellationToken))
            return (false, "Bạn không có quyền chỉnh sửa tài liệu này.");

        var lockRecord = await dbContext.DocumentEditLocks
            .SingleOrDefaultAsync(item => item.DocumentId == documentId, cancellationToken);
        var now = DateTime.UtcNow;
        if (lockRecord is not null &&
            !lockRecord.IsReleased &&
            lockRecord.ExpiresAt > now &&
            lockRecord.LockedByUserId != userId)
        {
            var owner = string.IsNullOrWhiteSpace(lockRecord.LockedByDisplayName)
                ? lockRecord.LockedByUserId
                : lockRecord.LockedByDisplayName;
            return (false, $"Tài liệu đang được {owner} chỉnh sửa đến {lockRecord.ExpiresAt.ToLocalTime():HH:mm}.");
        }

        var user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null) return (false, "Không tìm thấy tài khoản.");
        if (lockRecord is null)
        {
            lockRecord = new DocumentEditLock
            {
                DocumentId = documentId,
                LockedById = userId,
                LockedByUserId = userId,
                LockedByDisplayName = GetDisplayName(user),
                LockedAt = now,
                AcquiredAt = now,
                ExpiresAt = now.AddMinutes(15),
                IsReleased = false
            };
            dbContext.DocumentEditLocks.Add(lockRecord);
        }
        else
        {
            lockRecord.LockedById = userId;
            lockRecord.LockedByUserId = userId;
            lockRecord.LockedByDisplayName = GetDisplayName(user);
            lockRecord.LockedAt = now;
            lockRecord.AcquiredAt = now;
            lockRecord.ExpiresAt = now.AddMinutes(15);
            lockRecord.IsReleased = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> ReleaseLockAsync(
        int documentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var lockRecord = await dbContext.DocumentEditLocks
            .SingleOrDefaultAsync(item => item.DocumentId == documentId, cancellationToken);
        if (lockRecord is null || lockRecord.IsReleased)
            return (true, null);
        if (lockRecord.LockedByUserId != userId &&
            !await HasRoleAsync(userId, "Admin", cancellationToken))
            return (false, "Chỉ người đang giữ khóa hoặc Admin được nhả khóa.");

        lockRecord.IsReleased = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> ArchiveAsync(
        int documentId,
        string? reason,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return (false, "Vui lòng nhập lý do lưu trữ.");
        if (!await HasAnyRoleAsync(userId, cancellationToken, "Admin", "Project Director", "Project Manager"))
            return (false, "Bạn không có quyền lưu trữ tài liệu.");
        var document = await GetTrackedDocumentAsync(documentId, userId, isAdmin, cancellationToken);
        if (document is null) return (false, "Không tìm thấy tài liệu trong phạm vi được cấp.");
        if (!await CanManageArchiveInScopeAsync(
                document, userId, ResolveRole(await GetRoleCodesAsync(userId, cancellationToken)), cancellationToken))
            return (false, "Tài liệu nằm ngoài phạm vi dự án được cấp.");
        if (document.IsArchived) return (false, "Tài liệu đã được lưu trữ.");

        document.IsArchived = true;
        document.ArchivedAt = DateTime.UtcNow;
        document.ArchivedById = userId;
        document.ArchiveReason = reason?.Trim();
        var lockRecord = await dbContext.DocumentEditLocks
            .SingleOrDefaultAsync(item => item.DocumentId == documentId, cancellationToken);
        if (lockRecord is not null) lockRecord.IsReleased = true;
        dbContext.DocumentAuditLogs.Add(await CreateAuditAsync(
            document, "Archived", userId, cancellationToken, reason: document.ArchiveReason));
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> RestoreAsync(
        int documentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (!await HasAnyRoleAsync(userId, cancellationToken, "Admin", "Project Director", "Project Manager"))
            return (false, "Bạn không có quyền khôi phục tài liệu.");
        var document = await GetTrackedDocumentAsync(
            documentId, userId, isAdmin, cancellationToken, includeArchived: true);
        if (document is null) return (false, "Không tìm thấy tài liệu trong phạm vi được cấp.");
        if (!await CanManageArchiveInScopeAsync(
                document, userId, ResolveRole(await GetRoleCodesAsync(userId, cancellationToken)), cancellationToken))
            return (false, "Tài liệu nằm ngoài phạm vi dự án được cấp.");
        if (!document.IsArchived) return (false, "Tài liệu chưa được lưu trữ.");

        document.IsArchived = false;
        document.RestoredAt = DateTime.UtcNow;
        document.RestoredByUserId = userId;
        document.UpdatedByUserId = userId;
        document.UpdatedAt = DateTime.UtcNow;
        dbContext.DocumentAuditLogs.Add(await CreateAuditAsync(
            document, "Restored", userId, cancellationToken));
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    private async Task<IQueryable<ProjectDocument>> VisibleQueryAsync(
        string userId,
        bool isAdmin,
        bool includeArchived,
        bool tracking,
        CancellationToken cancellationToken)
    {
        IQueryable<ProjectDocument> query = tracking
            ? dbContext.ProjectDocuments
            : dbContext.ProjectDocuments.AsNoTracking();
        query = query
            .Include(item => item.CategoryEntity)
            .Include(item => item.Project)
            .Include(item => item.Contract)
            .Include(item => item.UploadedBy);
        if (!includeArchived)
            query = query.Where(item => !item.IsArchived);

        var user = await dbContext.Users.AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new { item.IsActive, item.OrganizationName })
            .SingleOrDefaultAsync(cancellationToken);
        if (user is null || !user.IsActive)
            return query.Where(_ => false);

        var roles = await GetRoleCodesAsync(userId, cancellationToken);
        if (roles.Contains("Admin")) return query;
        var role = ResolveRole(roles);
        if (role is null)
            return query.Where(_ => false);
        if (includeArchived && !ArchiveRoles.Contains(role))
            query = query.Where(item => !item.IsArchived);

        var activeProjectIds = dbContext.ProjectMembers
            .Where(member => member.UserId == userId && member.IsActive)
            .Select(member => member.ProjectId);

        if (role == "Worker")
        {
            return query.Where(item =>
                item.ApprovalStatus == DocumentApprovalStatus.Approved &&
                (item.IsSystemWide ||
                 (item.ProjectId.HasValue &&
                  activeProjectIds.Contains(item.ProjectId.Value) &&
                  (item.CategoryEntity != null &&
                   (item.CategoryEntity.Code == "Safety" ||
                    item.CategoryEntity.Code == "Minutes" ||
                    item.CategoryEntity.Code == "Other")))));
        }

        if (role == "ClientRepresentative")
        {
            if (string.IsNullOrWhiteSpace(user.OrganizationName))
                return query.Where(_ => false);
            var organization = user.OrganizationName.Trim();
            return query.Where(item =>
                item.ProjectId.HasValue &&
                activeProjectIds.Contains(item.ProjectId.Value) &&
                item.ApprovalStatus == DocumentApprovalStatus.Approved &&
                item.Project != null &&
                item.Project.ClientName != null &&
                item.Project.ClientName.Trim() == organization);
        }

        if (role is not ("ProjectDirector" or "ProjectManager" or "SiteManager" or
            "SupervisingEngineer" or "MaterialsStaff"))
            return query.Where(_ => false);

        query = query.Where(item =>
            item.IsSystemWide ||
            (item.ProjectId.HasValue && activeProjectIds.Contains(item.ProjectId.Value)));

        _ = isAdmin;
        return query;
    }

    private async Task<ProjectDocument?> GetTrackedDocumentAsync(
        int documentId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        var query = await VisibleQueryAsync(
            userId, isAdmin, includeArchived, tracking: true, cancellationToken);
        return await query.SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);
    }

    private async Task<bool> CanCreateInCategoryAsync(
        string userId,
        string categoryCode,
        int? projectId,
        bool isSystemWide,
        CancellationToken cancellationToken)
    {
        var roles = await GetRoleCodesAsync(userId, cancellationToken);
        var role = ResolveRole(roles);
        if (role is "Admin" or "Worker" or "ClientRepresentative") return false;
        if (isSystemWide || !projectId.HasValue) return false;
        if (!CanCreateCategory(role, categoryCode)) return false;
        return await IsProjectMemberAsync(projectId.Value, userId, isAdmin: false, cancellationToken);
    }

    private async Task<bool> CanEditDocumentAsync(
        ProjectDocument document,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var roles = await GetRoleCodesAsync(userId, cancellationToken);
        var role = ResolveRole(roles);
        if (role is "Admin" or "Worker" or "ClientRepresentative" || document.IsSystemWide)
            return false;
        if (!CanCreateCategory(role, GetCategoryCode(document))) return false;
        var isSpecialist = role is "SiteManager" or "SupervisingEngineer" or "MaterialsStaff";
        if (isSpecialist && document.CreatedByUserId != userId && document.UploadedById != userId)
            return false;
        return document.ProjectId.HasValue &&
            await IsProjectMemberAsync(document.ProjectId.Value, userId, isAdmin: false, cancellationToken);
    }

    private async Task<bool> IsProjectMemberAsync(
        int projectId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        if (isAdmin && await HasRoleAsync(userId, "Admin", cancellationToken))
            return await dbContext.Projects.AnyAsync(
                item => item.Id == projectId && !item.IsArchived, cancellationToken);
        return await dbContext.ProjectMembers.AnyAsync(
            item => item.ProjectId == projectId && item.UserId == userId && item.IsActive,
            cancellationToken);
    }

    private async Task<bool> CanManageArchiveInScopeAsync(
        ProjectDocument document,
        string userId,
        string? role,
        CancellationToken cancellationToken)
    {
        if (role == "Admin") return true;
        return (role is "ProjectDirector" or "ProjectManager") &&
            document.ProjectId.HasValue &&
            await IsProjectMemberAsync(document.ProjectId.Value, userId, false, cancellationToken);
    }

    private async Task<string?> ValidateLinksAsync(
        int? projectId,
        int? contractId,
        int? amendmentId,
        bool isSystemWide,
        CancellationToken cancellationToken)
    {
        if (isSystemWide && projectId.HasValue)
            return "Tài liệu dùng chung toàn hệ thống không được gắn với một dự án.";
        if (!isSystemWide && !projectId.HasValue)
            return "Tài liệu dự án phải được gắn với một dự án.";
        if (projectId.HasValue &&
            !await dbContext.Projects.AnyAsync(
                item => item.Id == projectId.Value && !item.IsArchived, cancellationToken))
            return "Dự án không tồn tại hoặc đã lưu trữ.";
        if (contractId.HasValue)
        {
            if (!projectId.HasValue ||
                !await dbContext.ContractProjects.AnyAsync(
                    item => item.ContractId == contractId.Value && item.ProjectId == projectId.Value,
                    cancellationToken))
                return "Hợp đồng không thuộc dự án đã chọn.";
            if (await dbContext.Contracts.AnyAsync(
                    item => item.Id == contractId.Value && item.IsArchived, cancellationToken))
                return "Không thể liên kết tài liệu mới với hợp đồng đã lưu trữ.";
        }
        if (amendmentId.HasValue)
        {
            if (!contractId.HasValue ||
                !await dbContext.ContractAmendments.AnyAsync(
                    item => item.Id == amendmentId.Value && item.ContractId == contractId.Value,
                    cancellationToken))
                return "Phụ lục không thuộc hợp đồng đã chọn.";
        }
        return null;
    }

    private async Task<bool> HasOwnedActiveLockAsync(
        int documentId,
        string userId,
        CancellationToken cancellationToken)
    {
        var editLock = await dbContext.DocumentEditLocks
            .SingleOrDefaultAsync(item => item.DocumentId == documentId, cancellationToken);
        return editLock is not null &&
            !editLock.IsReleased &&
            editLock.ExpiresAt > DateTime.UtcNow &&
            editLock.LockedByUserId == userId;
    }

    private async Task<bool> IsSelfApprovalAsync(
        ProjectDocument document,
        string userId,
        CancellationToken cancellationToken)
    {
        if (document.UploadedById == userId || document.SubmittedByUserId == userId)
            return true;
        var currentVersion = await dbContext.DocumentVersions
            .Where(item => item.DocumentId == document.Id && item.IsCurrent)
            .Select(item => item.UploadedById)
            .SingleOrDefaultAsync(cancellationToken);
        return currentVersion == userId;
    }

    private async Task<DocumentCategory> GetCategoryAsync(
        ProjectDocument document,
        CancellationToken cancellationToken)
    {
        if (document.CategoryId.HasValue)
        {
            var category = await dbContext.DocumentCategories
                .SingleOrDefaultAsync(item => item.Id == document.CategoryId.Value, cancellationToken);
            if (category is not null) return category;
        }
        return new DocumentCategory
        {
            Code = document.Category,
            Name = document.Category,
            ApprovalLevels = 0,
            IsActive = false
        };
    }

    private async Task<int> GetApprovalLevelsAsync(
        ProjectDocument document,
        CancellationToken cancellationToken)
        => (await GetCategoryAsync(document, cancellationToken)).ApprovalLevels;

    private Task<DocumentVersion?> GetCurrentVersionAsync(
        int documentId,
        CancellationToken cancellationToken)
    {
        return dbContext.DocumentVersions
            .SingleOrDefaultAsync(item => item.DocumentId == documentId && item.IsCurrent, cancellationToken);
    }

    private async Task<DocumentAuditLog> CreateAuditAsync(
        ProjectDocument document,
        string action,
        string userId,
        CancellationToken cancellationToken,
        string? oldValues = null,
        string? newValues = null,
        string? reason = null,
        int? versionNumber = null)
    {
        var actor = await GetActorAsync(userId, cancellationToken);
        var now = DateTime.UtcNow;
        return new DocumentAuditLog
        {
            DocumentId = document.Id,
            DocumentCode = document.DocumentCode,
            VersionNumber = versionNumber ?? document.CurrentVersionNumber,
            Action = action,
            PerformedById = userId,
            ActorUserId = userId,
            ActorDisplayName = actor.DisplayName,
            ActorRole = actor.Role,
            PerformedAt = now,
            CreatedAt = now,
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            Comment = reason,
            Reason = reason,
            IpAddress = httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString()
        };
    }

    private async Task<DocumentApprovalLog> CreateApprovalLogAsync(
        ProjectDocument document,
        DocumentVersion? version,
        int stepNumber,
        string action,
        string userId,
        string? comment,
        CancellationToken cancellationToken)
    {
        var roles = await GetRoleCodesAsync(userId, cancellationToken);
        return new DocumentApprovalLog
        {
            DocumentId = document.Id,
            DocumentVersionId = version?.Id
                ?? throw new InvalidOperationException("Không thể ghi lịch sử duyệt khi chưa có phiên bản tài liệu."),
            StepNumber = stepNumber,
            Action = action,
            ActorUserId = userId,
            ActorRole = roles.FirstOrDefault() ?? "Unassigned",
            CommentOrReason = comment,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task<(string DisplayName, string Role)> GetActorAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản thực hiện thao tác.");
        var roles = await GetRoleCodesAsync(userId, cancellationToken);
        var role = roles.FirstOrDefault() ?? "Unassigned";
        return (GetDisplayName(user), role);
    }

    private async Task<IReadOnlyCollection<string>> GetRoleCodesAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var roles = await dbContext.UserRoles
            .Where(userRole => userRole.UserId == userId)
            .Join(dbContext.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name)
            .Where(roleName => roleName != null)
            .Select(roleName => roleName!)
            .ToListAsync(cancellationToken);
        return roles.Select(NormalizeRole).ToArray();
    }

    private Task<bool> HasRoleAsync(
        string userId,
        string role,
        CancellationToken cancellationToken)
    {
        var roleCode = NormalizeRole(role);
        return dbContext.UserRoles
            .Where(userRole => userRole.UserId == userId)
            .Join(dbContext.Roles, userRole => userRole.RoleId, roleEntity => roleEntity.Id, (_, roleEntity) => roleEntity.Name)
            .AnyAsync(name => name == role || name == roleCode, cancellationToken);
    }

    private async Task<bool> HasAnyRoleAsync(
        string userId,
        CancellationToken cancellationToken,
        params string[] roleNames)
    {
        var roleCodes = roleNames.Select(NormalizeRole).ToArray();
        var roles = await GetRoleCodesAsync(userId, cancellationToken);
        return roles.Any(role => roleCodes.Contains(role, StringComparer.Ordinal));
    }

    private async Task<string> GenerateDocumentCodeAsync(
        int? projectId,
        bool isSystemWide,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        var scopeCode = isSystemWide
            ? "SYS"
            : await dbContext.Projects.AsNoTracking()
                .Where(item => item.Id == projectId)
                .Select(item => item.ProjectCode)
                .SingleAsync(cancellationToken);
        var prefix = $"DOC-{scopeCode}-{createdAt:yyyyMM}-";
        var count = await dbContext.ProjectDocuments.CountAsync(
            item => item.DocumentCode.StartsWith(prefix), cancellationToken);
        var sequence = count + 1;
        string code;
        do
        {
            code = $"{prefix}{sequence++:D4}";
        }
        while (await dbContext.ProjectDocuments.AnyAsync(
            item => item.DocumentCode == code, cancellationToken));
        return code;
    }

    private static async Task<string> HashFileAsync(
        string fullPath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(fullPath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GetDisplayName(ApplicationUser user)
        => !string.IsNullOrWhiteSpace(user.FullName)
            ? user.FullName.Trim()
            : user.Email ?? user.UserName ?? user.Id;

    private static string GetCategoryCode(ProjectDocument document)
        => document.CategoryEntity?.Code ?? document.LegacyCategory ?? document.Category;

    private static string NormalizeRole(string role)
        => role.Replace(" ", string.Empty, StringComparison.Ordinal);

    private static string? ResolveRole(IReadOnlyCollection<string> roles)
    {
        if (roles.Contains("Admin")) return "Admin";
        return roles.Count == 1 ? roles.First() : null;
    }

    private static string[]? GetSpecialtyCategoryCodes(string role)
        => role switch
        {
            "SiteManager" => ["Construction", "Safety", "Minutes", "OfficialLetter", "Other"],
            "SupervisingEngineer" => ["Design", "Quality", "Acceptance", "Handover", "Minutes", "Other"],
            "MaterialsStaff" => ["Materials", "Other"],
            _ => null
        };

    private static bool CanCreateCategory(string? role, string categoryCode)
    {
        if (role is "ProjectDirector" or "ProjectManager") return true;
        var allowed = role is null ? null : GetSpecialtyCategoryCodes(role);
        return allowed?.Contains(categoryCode, StringComparer.Ordinal) == true;
    }

    private static bool CanReviewRole(string? role, string categoryCode, int approvalLevels)
        => approvalLevels == 2 &&
            (role == "ProjectManager" ||
             (role == "SiteManager" && categoryCode is "Construction" or "Safety") ||
             (role == "SupervisingEngineer" &&
              categoryCode is "Design" or "Quality" or "Acceptance"));

    private static bool CanApproveRole(
        string? role,
        string categoryCode,
        int approvalLevels,
        int currentStep)
    {
        _ = categoryCode;
        return role switch
        {
            "ProjectDirector" => approvalLevels switch
            {
                1 => currentStep == 1,
                2 => currentStep == 2,
                _ => false
            },
            "ProjectManager" => approvalLevels == 1 && currentStep == 1,
            _ => false
        };
    }

    private static (string? Value, string? Error) NormalizeAllowedExtensions(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return (null, null);
        var extensions = value.Split(
                ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(extension => extension.StartsWith('.') ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (extensions.Length == 0 || extensions.Any(extension =>
                !FileValidationHelper.DefaultAllowedExtensions.Split(',').Contains(extension, StringComparer.OrdinalIgnoreCase)))
            return (null, "Danh mục chỉ được cho phép PDF, PNG, JPG, JPEG, DOCX hoặc XLSX.");
        return (string.Join(',', extensions), null);
    }
}
