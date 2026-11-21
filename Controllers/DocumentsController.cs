using ClosedXML.Excel;
using ConstructionProjectManagement.Data;
using ConstructionProjectManagement.Models;
using ConstructionProjectManagement.Services.Interfaces;
using ConstructionProjectManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace ConstructionProjectManagement.Controllers;

[Authorize]
public class DocumentsController(
    IProjectDocumentService documentService,
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(
        [FromQuery] DocumentSearchOptions options,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (options.ShowArchived && !CanManageArchive())
            return Forbid();

        var page = await documentService.GetPageAsync(
            user.Id, User.IsInRole("Admin"), options, cancellationToken);
        var categories = await documentService.GetCategoriesAsync(
            includeInactive: true, cancellationToken);
        var visibleDocuments = await documentService.GetVisibleAsync(
            user.Id, User.IsInRole("Admin"), cancellationToken, options.ShowArchived);
        return View(new DocumentListViewModel
        {
            Documents = page.Documents,
            Categories = categories,
            Projects = visibleDocuments.Where(item => item.Project is not null)
                .Select(item => item.Project!).DistinctBy(item => item.Id).OrderBy(item => item.Name).ToArray(),
            Contracts = visibleDocuments.Where(item => item.Contract is not null)
                .Select(item => item.Contract!).DistinctBy(item => item.Id).OrderBy(item => item.ContractNumber).ToArray(),
            Uploaders = visibleDocuments.Where(item => item.UploadedBy is not null)
                .Select(item => item.UploadedBy).DistinctBy(item => item.Id).OrderBy(item => item.FullName).ToArray(),
            Keyword = options.Keyword,
            ProjectId = options.ProjectId,
            CategoryId = options.CategoryId,
            ContractId = options.ContractId,
            ApprovalStatus = options.ApprovalStatus,
            UploadedById = options.UploadedById,
            UploadedFrom = options.UploadedFrom,
            UploadedTo = options.UploadedTo,
            ShowArchived = options.ShowArchived,
            Page = page.Page,
            PageSize = page.PageSize,
            TotalItems = page.TotalItems,
            CanCreate = CanCreateDocuments(),
            CanExport = CanExport()
        });
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var document = await documentService.GetByIdAsync(
            id, user.Id, User.IsInRole("Admin"), cancellationToken);
        if (document is null)
            return await documentService.ExistsAsync(id, cancellationToken) ? Forbid() : NotFound();

        var permissions = await documentService.GetPermissionsAsync(
            id, user.Id, User.IsInRole("Admin"), cancellationToken);
        var versions = document.Versions.OrderByDescending(item => item.VersionNumber).ToList();
        if (User.IsInRole("Worker") || User.IsInRole("Client Representative"))
            versions = versions.Where(item => item.ApprovalStatus == DocumentApprovalStatus.Approved).ToList();

        return View(new DocumentDetailsViewModel
        {
            Document = document,
            Versions = versions,
            AuditLogs = document.AuditLogs.OrderByDescending(item => item.PerformedAt).ToList(),
            ApprovalLogs = document.ApprovalLogs.OrderByDescending(item => item.CreatedAt).ToList(),
            Permissions = permissions
        });
    }

    [Authorize(Roles = "Project Director,Project Manager,Site Manager,Supervising Engineer,Materials Staff")]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var model = await CreateFormModelAsync(user.Id, cancellationToken);
        return View(model);
    }

    [Authorize(Roles = "Project Director,Project Manager,Site Manager,Supervising Engineer,Materials Staff")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        DocumentCreateViewModel model,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (model.Files.Count == 0)
            ModelState.AddModelError(nameof(model.Files), "Vui lòng chọn ít nhất một tệp.");
        if (!ModelState.IsValid)
        {
            model = await CreateFormModelAsync(user.Id, cancellationToken, model);
            return View(model);
        }

        var succeeded = new List<ProjectDocument>();
        var errors = new List<string>();
        foreach (var item in model.Files)
        {
            if (item.File is null)
            {
                errors.Add($"“{item.Title}”: chưa chọn tệp.");
                continue;
            }
            var result = await documentService.CreateAsync(
                model.ProjectId,
                model.ContractId,
                model.AmendmentId,
                item.CategoryId,
                model.IsSystemWide,
                item.Title,
                item.Description,
                item.File,
                user.Id,
                User.IsInRole("Admin"),
                cancellationToken);
            if (result.Succeeded && result.Document is not null)
                succeeded.Add(result.Document);
            else
                errors.Add($"“{item.Title}”: {result.Error}");
        }

        if (succeeded.Count > 0)
            TempData["SuccessMessage"] = $"Đã tạo {succeeded.Count} tài liệu.";
        if (errors.Count > 0)
            TempData["ErrorMessage"] = string.Join(" ", errors);
        return succeeded.Count > 0
            ? RedirectToAction(nameof(Details), new { id = succeeded[0].Id })
            : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var document = await documentService.GetByIdAsync(
            id, user.Id, User.IsInRole("Admin"), cancellationToken);
        if (document is null)
            return await documentService.ExistsAsync(id, cancellationToken) ? Forbid() : NotFound();
        var permissions = await documentService.GetPermissionsAsync(
            id, user.Id, User.IsInRole("Admin"), cancellationToken);
        if (!permissions.CanEdit) return Forbid();

        var lockResult = await documentService.AcquireLockAsync(
            id, user.Id, User.IsInRole("Admin"), cancellationToken);
        if (!lockResult.Succeeded)
        {
            TempData["ErrorMessage"] = lockResult.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        document = await documentService.GetByIdAsync(
            id, user.Id, User.IsInRole("Admin"), cancellationToken);
        if (document is null) return Forbid();
        return View(await EditFormModelAsync(user.Id, document, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        DocumentEditViewModel model,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var scopeFailure = await ScopeFailureAsync(model.Id, user.Id, cancellationToken);
        if (scopeFailure is not null) return scopeFailure;
        if (!ModelState.IsValid)
        {
            var current = await documentService.GetByIdAsync(
                model.Id, user.Id, User.IsInRole("Admin"), cancellationToken);
            if (current is null) return Forbid();
            return View(await EditFormModelAsync(user.Id, current, cancellationToken, model));
        }

        var updatedDocument = new ProjectDocument
        {
            Id = model.Id,
            Title = model.Title,
            Description = model.Description,
            CategoryId = model.CategoryId,
            ProjectId = model.ProjectId,
            ContractId = model.ContractId,
            AmendmentId = model.AmendmentId,
            IsSystemWide = model.IsSystemWide,
            DocumentCode = string.Empty,
            FileName = string.Empty,
            StoredFileName = string.Empty,
            ContentType = string.Empty,
            Category = string.Empty
        };
        var result = await documentService.UpdateMetadataAsync(
            updatedDocument, user.Id, User.IsInRole("Admin"), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã cập nhật thông tin tài liệu." : result.Error;
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadVersion(
        int id,
        IFormFile file,
        string? versionNote,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var scopeFailure = await ScopeFailureAsync(id, user.Id, cancellationToken);
        if (scopeFailure is not null) return scopeFailure;
        var permissions = await documentService.GetPermissionsAsync(
            id, user.Id, User.IsInRole("Admin"), cancellationToken);
        if (!permissions.CanEdit) return Forbid();
        var result = await documentService.UploadVersionAsync(
            id, file, versionNote, user.Id, User.IsInRole("Admin"), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã tạo phiên bản mới." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReleaseLock(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var scopeFailure = await ScopeFailureAsync(id, user.Id, cancellationToken, includeArchived: false);
        if (scopeFailure is not null) return scopeFailure;
        var result = await documentService.ReleaseLockAsync(
            id, user.Id, User.IsInRole("Admin"), cancellationToken);
        if (!result.Succeeded) return Forbid();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var scopeFailure = await ScopeFailureAsync(id, user.Id, cancellationToken);
        if (scopeFailure is not null) return scopeFailure;
        if (!(await documentService.GetPermissionsAsync(id, user.Id, User.IsInRole("Admin"), cancellationToken)).CanSubmit)
            return Forbid();
        var result = await documentService.SubmitAsync(
            id, user.Id, User.IsInRole("Admin"), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã gửi duyệt tài liệu." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var scopeFailure = await ScopeFailureAsync(id, user.Id, cancellationToken);
        if (scopeFailure is not null) return scopeFailure;
        if (!(await documentService.GetPermissionsAsync(id, user.Id, User.IsInRole("Admin"), cancellationToken)).CanReview)
            return Forbid();
        var result = await documentService.ReviewAsync(
            id, user.Id, User.IsInRole("Admin"), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã hoàn tất bước kiểm tra." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var scopeFailure = await ScopeFailureAsync(id, user.Id, cancellationToken);
        if (scopeFailure is not null) return scopeFailure;
        if (!(await documentService.GetPermissionsAsync(id, user.Id, User.IsInRole("Admin"), cancellationToken)).CanApprove)
            return Forbid();
        var result = await documentService.ApproveAsync(
            id, user.Id, User.IsInRole("Admin"), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã phê duyệt tài liệu." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        int id,
        string reason,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var scopeFailure = await ScopeFailureAsync(id, user.Id, cancellationToken);
        if (scopeFailure is not null) return scopeFailure;
        var permissions = await documentService.GetPermissionsAsync(
            id, user.Id, User.IsInRole("Admin"), cancellationToken);
        if (!permissions.CanReview && !permissions.CanApprove) return Forbid();
        var result = await documentService.RejectAsync(
            id, reason, user.Id, User.IsInRole("Admin"), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã từ chối tài liệu." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(
        int id,
        string? reason,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var scopeFailure = await ScopeFailureAsync(id, user.Id, cancellationToken);
        if (scopeFailure is not null) return scopeFailure;
        if (!(await documentService.GetPermissionsAsync(id, user.Id, User.IsInRole("Admin"), cancellationToken)).CanArchive)
            return Forbid();
        var result = await documentService.ArchiveAsync(
            id, reason, user.Id, User.IsInRole("Admin"), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã lưu trữ tài liệu." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var scopeFailure = await ScopeFailureAsync(id, user.Id, cancellationToken, includeArchived: true);
        if (scopeFailure is not null) return scopeFailure;
        if (!(await documentService.GetPermissionsAsync(id, user.Id, User.IsInRole("Admin"), cancellationToken)).CanRestore)
            return Forbid();
        var result = await documentService.RestoreAsync(
            id, user.Id, User.IsInRole("Admin"), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã khôi phục tài liệu." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Download(
        int id,
        int? version,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await documentService.GetFileAsync(
            id, version, user.Id, User.IsInRole("Admin"), preview: false, cancellationToken);
        if (result.Document is null)
            return await documentService.ExistsAsync(id, cancellationToken) ? Forbid() : NotFound();
        if (result.FilePath is null || result.Version is null)
            return NotFound(result.Error);
        return new PhysicalFileResult(result.FilePath, result.Version.ContentType)
        {
            FileDownloadName = result.Version.OriginalFileName,
            EnableRangeProcessing = true
        };
    }

    public async Task<IActionResult> Preview(
        int id,
        int? version,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await documentService.GetFileAsync(
            id, version, user.Id, User.IsInRole("Admin"), preview: true, cancellationToken);
        if (result.Document is null)
            return await documentService.ExistsAsync(id, cancellationToken) ? Forbid() : NotFound();
        if (result.FilePath is null || result.Version is null)
            return BadRequest(result.Error);
        return new PhysicalFileResult(result.FilePath, result.Version.ContentType)
        {
            EnableRangeProcessing = true
        };
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Categories(CancellationToken cancellationToken)
        => View(await documentService.GetCategoriesAsync(includeInactive: true, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult CreateCategory()
        => View("EditCategory", new DocumentCategory { Code = string.Empty, Name = string.Empty });

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> EditCategory(int id, CancellationToken cancellationToken)
    {
        var category = await dbContext.DocumentCategories
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return category is null ? NotFound() : View("EditCategory", category);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(
        DocumentCategory category,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View("EditCategory", category);
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await documentService.SaveCategoryAsync(category, user.Id, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("EditCategory", category);
        }
        TempData["SuccessMessage"] = "Đã lưu danh mục.";
        return RedirectToAction(nameof(Categories));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCategoryActive(
        int id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await documentService.SetCategoryActiveAsync(
            id, isActive, user.Id, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Đã cập nhật trạng thái danh mục." : result.Error;
        return RedirectToAction(nameof(Categories));
    }

    [Authorize(Roles = "Admin,Project Director,Project Manager,Site Manager,Supervising Engineer")]
    [HttpGet]
    public async Task<IActionResult> Statistics(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var documents = await documentService.GetVisibleAsync(
            user.Id, User.IsInRole("Admin"), cancellationToken);
        ViewBag.ByStatus = documents.GroupBy(item => GetApprovalStatusName(item.ApprovalStatus))
            .ToDictionary(group => group.Key, group => group.Count());
        ViewBag.ByCategory = documents.GroupBy(item => item.CategoryEntity?.Name ?? item.Category)
            .ToDictionary(group => group.Key, group => group.Count());
        ViewBag.ByProject = documents.GroupBy(item => item.Project?.Name ?? "Toàn hệ thống")
            .ToDictionary(group => group.Key, group => group.Count());
        ViewBag.ByMonth = documents.GroupBy(item => item.UploadedAt.ToString("yyyy-MM"))
            .OrderBy(group => group.Key)
            .ToDictionary(group => group.Key, group => group.Count());
        ViewBag.TotalFiles = documents.Count;
        ViewBag.TotalBytes = documents.Sum(item => item.FileSize);
        return View(documents);
    }

    [Authorize(Roles = "Admin,Project Director,Project Manager,Site Manager,Supervising Engineer")]
    [HttpGet]
    public async Task<IActionResult> ExportStatisticsExcel(CancellationToken cancellationToken)
    {
        if (!CanExport()) return Forbid();
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var documents = await documentService.GetVisibleAsync(
            user.Id, User.IsInRole("Admin"), cancellationToken);
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("ThongKe");
        sheet.Cell(1, 1).Value = "Nhóm thống kê";
        sheet.Cell(1, 2).Value = "Giá trị";
        sheet.Cell(1, 3).Value = "Số lượng";
        var row = 2;
        WriteStatisticsRows(sheet, ref row, "Danh mục", documents
            .GroupBy(item => item.CategoryEntity?.Name ?? item.Category)
            .Select(group => (group.Key, group.Count())));
        WriteStatisticsRows(sheet, ref row, "Dự án", documents
            .GroupBy(item => item.Project?.Name ?? "Toàn hệ thống")
            .Select(group => (group.Key, group.Count())));
        WriteStatisticsRows(sheet, ref row, "Trạng thái", documents
            .GroupBy(item => GetApprovalStatusName(item.ApprovalStatus))
            .Select(group => (group.Key, group.Count())));
        WriteStatisticsRows(sheet, ref row, "Tháng tải lên", documents
            .GroupBy(item => item.UploadedAt.ToString("yyyy-MM"))
            .Select(group => (group.Key, group.Count())));
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "thong-ke-tai-lieu.xlsx");
    }

    [Authorize(Roles = "Admin,Project Director,Project Manager,Site Manager,Supervising Engineer")]
    [HttpGet]
    public async Task<IActionResult> ExportStatisticsPdf(CancellationToken cancellationToken)
    {
        if (!CanExport()) return Forbid();
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var documents = await documentService.GetVisibleAsync(
            user.Id, User.IsInRole("Admin"), cancellationToken);
        var groups = new[]
        {
            ("Theo danh mục", documents.GroupBy(item => item.CategoryEntity?.Name ?? item.Category)
                .Select(group => (group.Key, group.Count())).ToArray()),
            ("Theo dự án", documents.GroupBy(item => item.Project?.Name ?? "Toàn hệ thống")
                .Select(group => (group.Key, group.Count())).ToArray()),
            ("Theo trạng thái", documents.GroupBy(item => GetApprovalStatusName(item.ApprovalStatus))
                .Select(group => (group.Key, group.Count())).ToArray()),
            ("Theo tháng tải lên", documents.GroupBy(item => item.UploadedAt.ToString("yyyy-MM"))
                .Select(group => (group.Key, group.Count())).ToArray())
        };
        var report = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(24);
                page.Header().Text("Thống kê quản lý tài liệu").FontSize(18).SemiBold();
                page.Content().Column(column =>
                {
                    foreach (var (title, rows) in groups)
                    {
                        column.Item().PaddingTop(10).Text(title).FontSize(13).SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Text("Giá trị");
                                header.Cell().Text("Số lượng");
                            });
                            foreach (var row in rows)
                            {
                                table.Cell().Text(row.Key);
                                table.Cell().Text(row.Item2.ToString());
                            }
                        });
                    }
                });
            });
        });
        using var stream = new MemoryStream();
        report.GeneratePdf(stream);
        return File(stream.ToArray(), "application/pdf", "thong-ke-tai-lieu.pdf");
    }

    [Authorize(Roles = "Admin,Project Director,Project Manager,Site Manager,Supervising Engineer")]
    [HttpGet]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] DocumentSearchOptions options,
        CancellationToken cancellationToken)
    {
        if (!CanExport() || (options.ShowArchived && !CanManageArchive())) return Forbid();
        var documents = await GetAllFilteredDocumentsAsync(options, cancellationToken);
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("TaiLieu");
        sheet.Cell(1, 1).Value = "Mã tài liệu";
        sheet.Cell(1, 2).Value = "Tiêu đề";
        sheet.Cell(1, 3).Value = "Danh mục";
        sheet.Cell(1, 4).Value = "Dự án";
        sheet.Cell(1, 5).Value = "Trạng thái";
        sheet.Cell(1, 6).Value = "Phiên bản";
        sheet.Cell(1, 7).Value = "Dung lượng (byte)";
        sheet.Cell(1, 8).Value = "Lưu trữ";
        var row = 2;
        foreach (var document in documents)
        {
            sheet.Cell(row, 1).Value = document.DocumentCode;
            sheet.Cell(row, 2).Value = document.Title;
            sheet.Cell(row, 3).Value = document.CategoryEntity?.Name ?? document.Category;
            sheet.Cell(row, 4).Value = document.Project?.Name ?? "Toàn hệ thống";
            sheet.Cell(row, 5).Value = document.ApprovalStatus.ToString();
            sheet.Cell(row, 6).Value = document.CurrentVersionNumber;
            sheet.Cell(row, 7).Value = document.FileSize;
            sheet.Cell(row, 8).Value = document.IsArchived ? "Đã lưu trữ" : "Đang hoạt động";
            row++;
        }
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "tai-lieu.xlsx");
    }

    [Authorize(Roles = "Admin,Project Director,Project Manager,Site Manager,Supervising Engineer")]
    [HttpGet]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] DocumentSearchOptions options,
        CancellationToken cancellationToken)
    {
        if (!CanExport() || (options.ShowArchived && !CanManageArchive())) return Forbid();
        var documents = await GetAllFilteredDocumentsAsync(options, cancellationToken);
        var report = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(24);
                page.Header().Text("Báo cáo quản lý tài liệu").FontSize(18).SemiBold();
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.3f);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                    });
                    table.Header(header =>
                    {
                        header.Cell().Text("Mã");
                        header.Cell().Text("Tiêu đề");
                        header.Cell().Text("Danh mục");
                        header.Cell().Text("Trạng thái");
                    });
                    foreach (var document in documents)
                    {
                        table.Cell().Text(document.DocumentCode);
                        table.Cell().Text(document.Title);
                        table.Cell().Text(document.CategoryEntity?.Name ?? document.Category);
                        table.Cell().Text(document.IsArchived ? "Đã lưu trữ" : document.ApprovalStatus.ToString());
                    }
                });
            });
        });
        using var stream = new MemoryStream();
        report.GeneratePdf(stream);
        return File(stream.ToArray(), "application/pdf", "tai-lieu.pdf");
    }

    private async Task<IActionResult?> ScopeFailureAsync(
        int id,
        string userId,
        CancellationToken cancellationToken,
        bool includeArchived = true)
    {
        var document = await documentService.GetByIdAsync(
            id, userId, User.IsInRole("Admin"), cancellationToken, includeArchived);
        if (document is not null) return null;
        return await documentService.ExistsAsync(id, cancellationToken) ? Forbid() : NotFound();
    }

    private async Task<DocumentCreateViewModel> CreateFormModelAsync(
        string userId,
        CancellationToken cancellationToken,
        DocumentCreateViewModel? values = null)
    {
        var categories = await documentService.GetCategoriesAsync(false, cancellationToken);
        var role = GetCurrentRoleCode();
        var allowedCategoryCodes = GetSpecialtyCategoryCodes(role);
        if (allowedCategoryCodes is not null)
            categories = categories.Where(item => allowedCategoryCodes.Contains(item.Code)).ToArray();

        var projectsQuery = dbContext.Projects.AsNoTracking().Where(item => !item.IsArchived);
        if (!User.IsInRole("Admin"))
        {
            var projectIds = dbContext.ProjectMembers
                .Where(item => item.UserId == userId && item.IsActive)
                .Select(item => item.ProjectId);
            projectsQuery = projectsQuery.Where(item => projectIds.Contains(item.Id));
        }
        var projects = await projectsQuery.OrderBy(item => item.Name).ToListAsync(cancellationToken);
        var projectId = values?.ProjectId;
        var contractsQuery = dbContext.Contracts.AsNoTracking()
            .Where(item => !item.IsArchived);
        if (!User.IsInRole("Admin"))
        {
            var activeProjectIds = dbContext.ProjectMembers
                .Where(item => item.UserId == userId && item.IsActive)
                .Select(item => item.ProjectId);
            contractsQuery = contractsQuery.Where(contract =>
                contract.ContractProjects.Any(link => activeProjectIds.Contains(link.ProjectId)));
        }
        if (projectId.HasValue)
            contractsQuery = contractsQuery.Where(contract =>
                contract.ContractProjects.Any(link => link.ProjectId == projectId.Value));
        var contracts = await contractsQuery.OrderBy(item => item.ContractNumber).ToListAsync(cancellationToken);
        var eligibleContractIds = contracts.Select(item => item.Id).ToArray();
        var amendmentQuery = dbContext.ContractAmendments.AsNoTracking()
            .Where(item => eligibleContractIds.Contains(item.ContractId));
        if (values?.ContractId is int selectedContractId)
            amendmentQuery = amendmentQuery.Where(item => item.ContractId == selectedContractId);
        var amendments = await amendmentQuery.OrderBy(item => item.AmendmentNumber).ToListAsync(cancellationToken);

        return new DocumentCreateViewModel
        {
            ProjectId = values?.ProjectId,
            ContractId = values?.ContractId,
            AmendmentId = values?.AmendmentId,
            IsSystemWide = values?.IsSystemWide ?? false,
            Files = values?.Files ?? [new DocumentUploadItemViewModel()],
            Categories = categories,
            Projects = projects,
            Contracts = contracts,
            Amendments = amendments,
            CanCreateSystemWide = User.IsInRole("Admin")
        };
    }

    private async Task<DocumentEditViewModel> EditFormModelAsync(
        string userId,
        ProjectDocument document,
        CancellationToken cancellationToken,
        DocumentEditViewModel? values = null)
    {
        var categories = await documentService.GetCategoriesAsync(true, cancellationToken);
        var role = GetCurrentRoleCode();
        var allowedCategoryCodes = GetSpecialtyCategoryCodes(role);
        if (allowedCategoryCodes is not null)
            categories = categories.Where(item =>
                allowedCategoryCodes.Contains(item.Code) || item.Id == document.CategoryId).ToArray();

        var projectsQuery = dbContext.Projects.AsNoTracking().Where(item => !item.IsArchived);
        if (!User.IsInRole("Admin"))
        {
            var projectIds = dbContext.ProjectMembers
                .Where(item => item.UserId == userId && item.IsActive)
                .Select(item => item.ProjectId);
            projectsQuery = projectsQuery.Where(item => projectIds.Contains(item.Id));
        }
        var projects = await projectsQuery.OrderBy(item => item.Name).ToListAsync(cancellationToken);
        var projectId = values?.ProjectId ?? document.ProjectId;
        var contractsQuery = dbContext.Contracts.AsNoTracking().Where(item => !item.IsArchived);
        if (projectId.HasValue)
            contractsQuery = contractsQuery.Where(item =>
                item.ContractProjects.Any(link => link.ProjectId == projectId.Value));
        else if (!User.IsInRole("Admin"))
            contractsQuery = contractsQuery.Where(_ => false);
        var contracts = await contractsQuery.OrderBy(item => item.ContractNumber).ToListAsync(cancellationToken);
        var contractId = values?.ContractId ?? document.ContractId;
        var eligibleContractIds = contracts.Select(item => item.Id).ToArray();
        var amendmentQuery = dbContext.ContractAmendments.AsNoTracking()
            .Where(item => eligibleContractIds.Contains(item.ContractId));
        if (contractId.HasValue)
            amendmentQuery = amendmentQuery.Where(item => item.ContractId == contractId.Value);
        var amendments = await amendmentQuery.OrderBy(item => item.AmendmentNumber).ToListAsync(cancellationToken);
        var editLock = await dbContext.DocumentEditLocks.AsNoTracking()
            .SingleOrDefaultAsync(item => item.DocumentId == document.Id, cancellationToken);

        return new DocumentEditViewModel
        {
            Id = document.Id,
            Title = values?.Title ?? document.Title,
            Description = values?.Description ?? document.Description,
            CategoryId = values?.CategoryId ?? document.CategoryId,
            ProjectId = projectId,
            ContractId = values?.ContractId ?? document.ContractId,
            AmendmentId = values?.AmendmentId ?? document.AmendmentId,
            IsSystemWide = values?.IsSystemWide ?? document.IsSystemWide,
            VersionNote = values?.VersionNote,
            Categories = categories,
            Projects = projects,
            Contracts = contracts,
            Amendments = amendments,
            EditLock = editLock
        };
    }

    private async Task<IReadOnlyCollection<ProjectDocument>> GetAllFilteredDocumentsAsync(
        DocumentSearchOptions options,
        CancellationToken cancellationToken)
    {
        var documents = new List<ProjectDocument>();
        var pageNumber = 1;
        DocumentPage page;
        do
        {
            options.Page = pageNumber++;
            options.PageSize = 100;
            var user = await userManager.GetUserAsync(User)
                ?? throw new InvalidOperationException("Không tìm thấy người dùng hiện tại.");
            page = await documentService.GetPageAsync(
                user.Id,
                User.IsInRole("Admin"),
                options,
                cancellationToken);
            documents.AddRange(page.Documents);
        }
        while (documents.Count < page.TotalItems);
        return documents;
    }

    private bool CanCreateDocuments()
        => User.IsInRole("Project Director") ||
           User.IsInRole("Project Manager") ||
           User.IsInRole("Site Manager") ||
           User.IsInRole("Supervising Engineer") ||
           User.IsInRole("Materials Staff");

    private bool CanManageArchive()
        => User.IsInRole("Admin") ||
           User.IsInRole("Project Director") ||
           User.IsInRole("Project Manager");

    private bool CanExport()
        => CanManageArchive() ||
           User.IsInRole("Site Manager") ||
           User.IsInRole("Supervising Engineer");

    private static void WriteStatisticsRows(
        IXLWorksheet sheet,
        ref int row,
        string groupName,
        IEnumerable<(string Key, int Count)> items)
    {
        foreach (var (key, count) in items)
        {
            sheet.Cell(row, 1).Value = groupName;
            sheet.Cell(row, 2).Value = key;
            sheet.Cell(row, 3).Value = count;
            row++;
        }
    }

    private static string GetApprovalStatusName(DocumentApprovalStatus status)
        => status switch
        {
            DocumentApprovalStatus.Draft => "Bản nháp",
            DocumentApprovalStatus.PendingReview => "Chờ kiểm tra cấp 1",
            DocumentApprovalStatus.PendingApproval => "Chờ phê duyệt",
            DocumentApprovalStatus.Approved => "Đã phê duyệt",
            DocumentApprovalStatus.Rejected => "Bị từ chối",
            _ => status.ToString()
        };

    private string? GetCurrentRoleCode()
    {
        if (User.IsInRole("Admin")) return "Admin";
        var roles = new[]
        {
            "Project Director", "Project Manager", "Site Manager",
            "Supervising Engineer", "Materials Staff"
        };
        return roles.FirstOrDefault(User.IsInRole)?.Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    private static string[]? GetSpecialtyCategoryCodes(string? role)
        => role switch
        {
            "SiteManager" => ["Construction", "Safety", "Minutes", "OfficialLetter", "Other"],
            "SupervisingEngineer" => ["Design", "Quality", "Acceptance", "Handover", "Minutes", "Other"],
            "MaterialsStaff" => ["Materials", "Other"],
            _ => null
        };
}
