using ConstructionProjectManagement.Models;
using ConstructionProjectManagement.Data;
using ConstructionProjectManagement.Services.Interfaces;
using ConstructionProjectManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using Task = System.Threading.Tasks.Task;

namespace ConstructionProjectManagement.Controllers;

[Authorize]
public class ContractsController(
    IContractService contractService,
    IProjectDocumentService documentService,
    IProjectService projectService,
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(
        string? keyword,
        ContractStatus? status,
        ContractType? contractType,
        bool showArchived,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var contracts = await contractService.GetVisibleAsync(
            user.Id,
            User.IsInRole("Admin"),
            cancellationToken,
            showArchived);
        if (!string.IsNullOrWhiteSpace(keyword))
            contracts = contracts.Where(item =>
                item.ContractNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
        if (status.HasValue)
            contracts = contracts.Where(item => item.Status == status.Value).ToList();
        if (contractType.HasValue)
            contracts = contracts.Where(item => item.ContractType == contractType.Value).ToList();
        return View(new ContractListViewModel
        {
            Contracts = contracts,
            Keyword = keyword,
            Status = status,
            ContractType = contractType,
            ShowArchived = showArchived
        });
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var contract = await contractService.GetByIdAsync(
            id,
            user.Id,
            User.IsInRole("Admin"),
            cancellationToken);
        if (contract is null)
        {
            return await NotFoundOrForbidAsync(id, cancellationToken);
        }

        return View(new ContractDetailsViewModel { Contract = contract });
    }

    [Authorize(Roles = "Project Director,Project Manager")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadProjectsAsync(cancellationToken);
        return View(new ContractEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Project Director,Project Manager")]
    public async Task<IActionResult> Create(
        ContractEditViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadProjectsAsync(cancellationToken);
            return View(model);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await contractService.CreateAsync(
            ToEntity(model),
            user.Id,
            User.IsInRole("Admin") || User.IsInRole("Project Director"),
            cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await LoadProjectsAsync(cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = "Đã tạo hợp đồng.";
        return RedirectToAction(nameof(Details), new { id = result.Contract!.Id });
    }

    [Authorize(Roles = "Admin,Project Director,Project Manager")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var contract = await contractService.GetByIdAsync(
            id,
            user.Id,
            User.IsInRole("Admin"),
            cancellationToken);
        if (contract is null)
        {
            return await NotFoundOrForbidAsync(id, cancellationToken);
        }
        if (contract.IsArchived) return Forbid();

        await LoadProjectsAsync(cancellationToken);
        return View(ToViewModel(contract));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Project Director,Project Manager")]
    public async Task<IActionResult> Edit(
        int id,
        ContractEditViewModel model,
        CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await LoadProjectsAsync(cancellationToken);
            return View(model);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await contractService.UpdateAsync(
            ToEntity(model, id),
            user.Id,
            User.IsInRole("Admin") || User.IsInRole("Project Director"),
            cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await LoadProjectsAsync(cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = "Đã cập nhật hợp đồng.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Project Director,Project Manager")]
    public async Task<IActionResult> UploadDocument(
        int contractId,
        string category,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var contract = await contractService.GetByIdAsync(
            contractId,
            user.Id,
            User.IsInRole("Admin"),
            cancellationToken);
        if (contract is null)
        {
            return await NotFoundOrForbidAsync(contractId, cancellationToken);
        }
        if (contract.IsArchived) return Forbid();

        var result = await documentService.UploadAsync(
            contract.ProjectId,
            contract.Id,
            category,
            file,
            user.Id,
            cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Error;
        }
        else
        {
            TempData["SuccessMessage"] = "Đã tải tài liệu lên.";
        }

        return RedirectToAction(nameof(Details), new { id = contractId });
    }

    public async Task<IActionResult> DownloadDocument(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await documentService.GetDownloadAsync(
            id,
            user.Id,
            User.IsInRole("Admin") || User.IsInRole("Project Director"),
            cancellationToken);
        if (result.Document is null || result.FilePath is null)
        {
            return NotFound();
        }

        return PhysicalFile(result.FilePath, result.Document.ContentType, result.Document.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await contractService.SubmitAsync(id, user.Id, User.IsInRole("Admin"), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Đã gửi hợp đồng duyệt." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await contractService.ReviewAsync(id, user.Id, User.IsInRole("Admin"), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Đã kiểm tra bước 1." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await contractService.ApproveAsync(id, user.Id, User.IsInRole("Admin"), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Đã phê duyệt hợp đồng." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await contractService.RejectAsync(id, reason, user.Id, User.IsInRole("Admin"), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Đã từ chối hợp đồng." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Project Director,Project Manager")]
    public async Task<IActionResult> CreateAmendment(
        int contractId,
        string title,
        DateTime effectiveDate,
        decimal valueAdjustment,
        DateTime? newExpirationDate,
        string changeDescription,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var amendment = new ContractAmendment
        {
            ContractId = contractId,
            AmendmentNumber = "Tự động",
            Title = title,
            EffectiveDate = effectiveDate,
            ValueAdjustment = valueAdjustment,
            NewExpirationDate = newExpirationDate,
            ChangeDescription = changeDescription,
            CreatedById = user.Id
        };
        var result = await contractService.CreateAmendmentAsync(amendment, user.Id, false, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Đã tạo phụ lục hợp đồng." : result.Error;
        return RedirectToAction(nameof(Details), new { id = contractId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitAmendment(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await contractService.SubmitAmendmentAsync(id, user.Id, false, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Đã gửi phụ lục duyệt." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewAmendment(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await contractService.ReviewAmendmentAsync(id, user.Id, false, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Đã kiểm tra phụ lục." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveAmendment(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await contractService.ApproveAmendmentAsync(id, user.Id, false, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Đã phê duyệt phụ lục." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectAmendment(int id, string reason, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await contractService.RejectAmendmentAsync(id, reason, user.Id, false, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Đã từ chối phụ lục." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Project Director,Project Manager")]
    public async Task<IActionResult> Archive(int id, string reason, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await contractService.ArchiveAsync(id, reason, user.Id, User.IsInRole("Admin"), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Đã lưu trữ hợp đồng." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Project Director,Project Manager")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var contract = await contractService.GetByIdAsync(
            id, user.Id, User.IsInRole("Admin"), cancellationToken);
        if (contract is null)
        {
            return await NotFoundOrForbidAsync(id, cancellationToken);
        }

        var result = await contractService.RestoreAsync(id, user.Id, User.IsInRole("Admin"), cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Đã khôi phục hợp đồng." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Project Director,Project Manager")]
    public async Task<IActionResult> Statistics(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var contracts = await contractService.GetVisibleAsync(user.Id, User.IsInRole("Admin"), cancellationToken);
        ViewBag.ByStatus = contracts.GroupBy(item => item.Status)
            .ToDictionary(item => item.Key.ToString(), item => item.Count());
        ViewBag.ByType = contracts.GroupBy(item => item.ContractType)
            .ToDictionary(item => item.Key.ToString(), item => item.Count());
        ViewBag.OriginalValue = contracts.Sum(item => item.OriginalValue);
        ViewBag.CurrentValue = contracts.Sum(item => item.CurrentValue);
        ViewBag.Upcoming = contracts.Count(item => item.EndDate.HasValue &&
            item.EndDate.Value.Date >= DateTime.Today &&
            item.EndDate.Value.Date <= DateTime.Today.AddDays(30));
        ViewBag.Expired = contracts.Count(item => item.EndDate.HasValue && item.EndDate.Value.Date < DateTime.Today);
        return View(contracts);
    }

    [Authorize(Roles = "Admin,Project Director,Project Manager")]
    public async Task<IActionResult> ExportExcel(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var contracts = await contractService.GetVisibleAsync(user.Id, User.IsInRole("Admin"), cancellationToken);
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("HopDong");
        sheet.Cell(1, 1).Value = "So hop dong";
        sheet.Cell(1, 2).Value = "Ten";
        sheet.Cell(1, 3).Value = "Trang thai";
        sheet.Cell(1, 4).Value = "Gia tri goc";
        sheet.Cell(1, 5).Value = "Gia tri hien tai";
        var row = 2;
        foreach (var contract in contracts)
        {
            sheet.Cell(row, 1).Value = contract.ContractNumber;
            sheet.Cell(row, 2).Value = contract.Title;
            sheet.Cell(row, 3).Value = contract.Status.ToString();
            sheet.Cell(row, 4).Value = (double)contract.OriginalValue;
            sheet.Cell(row, 5).Value = (double)contract.CurrentValue;
            row++;
        }
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "fake-task03-contracts.xlsx");
    }

    [Authorize(Roles = "Admin,Project Director,Project Manager")]
    public async Task<IActionResult> ExportPdf(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var contracts = await contractService.GetVisibleAsync(user.Id, User.IsInRole("Admin"), cancellationToken);
        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text("Bao cao hop dong [FAKE-TASK03]").FontSize(18);
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns => { columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn(); });
                    table.Header(header =>
                    {
                        header.Cell().Text("So hop dong");
                        header.Cell().Text("Ten");
                        header.Cell().Text("Trang thai");
                    });
                    foreach (var contract in contracts)
                    {
                        table.Cell().Text(contract.ContractNumber);
                        table.Cell().Text(contract.Title);
                        table.Cell().Text(contract.Status.ToString());
                    }
                });
            });
        });
        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return File(stream.ToArray(), "application/pdf", "fake-task03-contracts.pdf");
    }

    private async Task LoadProjectsAsync(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return;
        }

        ViewBag.Projects = await projectService.GetVisibleProjectsAsync(
            user.Id,
            User.IsInRole("Admin") || User.IsInRole("Project Director"),
            cancellationToken);
        ViewBag.Parties = await dbContext.ContractParties
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
    }

    private static Contract ToEntity(ContractEditViewModel model, int? id = null)
    {
        return new Contract
        {
            Id = id ?? 0,
            ContractNumber = model.ContractNumber,
            Title = model.Title,
            ContractType = model.ContractType,
            ClientPartyId = model.ClientPartyId,
            ContractorPartyId = model.ContractorPartyId,
            OriginalValue = model.ContractValue,
            CurrentValue = model.ContractValue,
            ContractValue = model.ContractValue,
            Currency = model.Currency,
            ScopeOfWork = model.ScopeOfWork,
            ProgressPercentage = model.ProgressPercentage,
            SignedDate = model.SignedDate,
            EffectiveDate = model.EffectiveDate,
            EndDate = model.EndDate,
            Status = model.Status,
            ContractProjects = model.ProjectIds.Select(projectId => new ContractProject
            {
                ProjectId = projectId,
                AllocatedValue = model.AllocatedValues.TryGetValue(projectId, out var value) ? value : 0
            }).ToList()
        };
    }

    private static ContractEditViewModel ToViewModel(Contract contract)
    {
        return new ContractEditViewModel
        {
            Id = contract.Id,
            ContractNumber = contract.ContractNumber,
            Title = contract.Title,
            ContractType = contract.ContractType,
            ClientPartyId = contract.ClientPartyId,
            ContractorPartyId = contract.ContractorPartyId,
            ContractValue = contract.CurrentValue,
            Currency = contract.Currency,
            ScopeOfWork = contract.ScopeOfWork,
            ProgressPercentage = contract.ProgressPercentage,
            SignedDate = contract.SignedDate,
            EffectiveDate = contract.EffectiveDate,
            EndDate = contract.EndDate,
            ProjectIds = contract.ContractProjects.Select(item => item.ProjectId).ToList(),
            AllocatedValues = contract.ContractProjects.ToDictionary(item => item.ProjectId, item => item.AllocatedValue),
            Status = contract.Status
        };
    }

    private async Task<IActionResult> NotFoundOrForbidAsync(int id, CancellationToken cancellationToken)
    {
        return await contractService.ExistsAsync(id, cancellationToken)
            ? Forbid()
            : NotFound();
    }
}
