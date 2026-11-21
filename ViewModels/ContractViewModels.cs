using System.ComponentModel.DataAnnotations;
using ConstructionProjectManagement.Models;

namespace ConstructionProjectManagement.ViewModels;

public sealed class ContractEditViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Danh sách dự án")]
    public List<int> ProjectIds { get; set; } = [];
    public Dictionary<int, decimal> AllocatedValues { get; set; } = [];

    [Required(ErrorMessage = "Vui lòng nhập tên hợp đồng.")]
    [StringLength(200)]
    [Display(Name = "Tên hợp đồng")]
    public string Title { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Số hợp đồng")]
    public string ContractNumber { get; set; } = "Tự động sinh";

    [Display(Name = "Loại hợp đồng")]
    public ContractType ContractType { get; set; } = ContractType.Construction;

    [Display(Name = "Bên chủ đầu tư")]
    public int? ClientPartyId { get; set; }
    [Display(Name = "Bên nhà thầu")]
    public int? ContractorPartyId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá trị không được âm.")]
    [Display(Name = "Giá trị hợp đồng")]
    public decimal ContractValue { get; set; }
    [Display(Name = "Tiền tệ")]
    public string Currency { get; set; } = "VND";
    [Display(Name = "Phạm vi công việc")]
    [StringLength(4000)]
    public string? ScopeOfWork { get; set; }
    [Range(0, 100)]
    [Display(Name = "Tiến độ")]
    public int ProgressPercentage { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày ký")]
    public DateTime? SignedDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày hiệu lực")]
    public DateTime? EffectiveDate { get; set; }
    [DataType(DataType.Date)]
    [Display(Name = "Ngày hết hạn")]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Trạng thái")]
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
}

public sealed class ContractListViewModel
{
    public required IReadOnlyCollection<Contract> Contracts { get; init; }
    public string? Keyword { get; init; }
    public ContractStatus? Status { get; init; }
    public ContractType? ContractType { get; init; }
    public bool ShowArchived { get; init; }
}

public sealed class ContractDetailsViewModel
{
    public required Contract Contract { get; init; }
}
