using System.ComponentModel.DataAnnotations;

namespace ConstructionProjectManagement.Models;

public class ProjectMemberRoleAssignment
{
    public int Id { get; set; }

    public int ProjectMemberId { get; set; }

    public ProjectMember ProjectMember { get; set; } = null!;

    [MaxLength(40)]
    public required string ProjectRoleCode { get; set; }

    public ProjectRole ProjectRole { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }
}
