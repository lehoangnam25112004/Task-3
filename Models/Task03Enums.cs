namespace ConstructionProjectManagement.Models;

public enum ContractType
{
    Construction,
    DesignConsulting,
    SupervisionConsulting,
    Survey,
    ProjectManagement,
    MaterialSupply,
    TestingInspection,
    Maintenance,
    Other
}

public enum ContractPartyRole
{
    Client,
    Contractor,
    Consultant,
    Supplier,
    Other
}

public enum DocumentApprovalStatus
{
    Draft,
    PendingReview,
    PendingApproval,
    Approved,
    Rejected
}

public enum AmendmentApprovalStatus
{
    Draft,
    PendingReview,
    PendingApproval,
    Approved,
    Rejected
}
