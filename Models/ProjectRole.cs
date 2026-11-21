using System.ComponentModel.DataAnnotations;

namespace ConstructionProjectManagement.Models;

public class ProjectRole
{
    [MaxLength(40)]
    public required string Code { get; set; }

    [MaxLength(100)]
    public required string Name { get; set; }

    public int? MaximumConcurrentProjects { get; set; }

    public ICollection<ProjectMemberRoleAssignment> Assignments { get; set; } = [];
}
