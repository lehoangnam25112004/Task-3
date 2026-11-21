using Microsoft.AspNetCore.Identity;

namespace ConstructionProjectManagement.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    public string? OrganizationName { get; set; }

    public bool IsActive { get; set; } = true;
}
