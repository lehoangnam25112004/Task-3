using System.ComponentModel.DataAnnotations;

namespace ConstructionProjectManagement.Models;

public class ContractParty
{
    public int Id { get; set; }
    [MaxLength(50)]
    public required string Code { get; set; }
    [MaxLength(250)]
    public required string Name { get; set; }
    public ContractPartyRole PartyRole { get; set; }
    [MaxLength(50)]
    public string? TaxCode { get; set; }
    [MaxLength(150)]
    public string? RepresentativeName { get; set; }
    [MaxLength(50)]
    public string? Phone { get; set; }
    [MaxLength(150)]
    public string? Email { get; set; }
    [MaxLength(500)]
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ContractProject
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public decimal AllocatedValue { get; set; }
    [MaxLength(500)]
    public string? ScopeNote { get; set; }
}

public class ContractAmendment
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
    [MaxLength(60)]
    public required string AmendmentNumber { get; set; }
    [MaxLength(250)]
    public required string Title { get; set; }
    public DateTime? SignedDate { get; set; }
    public DateTime EffectiveDate { get; set; }
    [MaxLength(4000)]
    public required string ChangeDescription { get; set; }
    public decimal ValueAdjustment { get; set; }
    public DateTime? NewExpirationDate { get; set; }
    public AmendmentApprovalStatus ApprovalStatus { get; set; } = AmendmentApprovalStatus.Draft;
    [MaxLength(1000)]
    public string? RejectionReason { get; set; }
    public required string CreatedById { get; set; }
    public ApplicationUser CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ReviewedById { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ApprovedById { get; set; }
    public ApplicationUser? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool IsApplied { get; set; }
}

public class ContractValueHistory
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
    public int? AmendmentId { get; set; }
    public ContractAmendment? Amendment { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public decimal NewValue { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public required string RecordedById { get; set; }
    public ApplicationUser RecordedBy { get; set; } = null!;
    [MaxLength(500)]
    public string? Note { get; set; }
}

public class ContractAuditLog
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
    public int? AmendmentId { get; set; }
    public ContractAmendment? Amendment { get; set; }
    [MaxLength(50)]
    public required string Action { get; set; }
    public required string PerformedById { get; set; }
    public ApplicationUser PerformedBy { get; set; } = null!;
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    [MaxLength(1000)]
    public string? Comment { get; set; }
}

public class ContractEditLock
{
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
    public required string LockedById { get; set; }
    public ApplicationUser LockedBy { get; set; } = null!;
    public DateTime LockedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
