using System.ComponentModel.DataAnnotations;

namespace ConstructionProjectManagement.Models;

public class DocumentCategory
{
    public int Id { get; set; }
    [MaxLength(32)]
    public required string Code { get; set; }
    [MaxLength(200)]
    public required string Name { get; set; }
    [MaxLength(500)]
    public string? Description { get; set; }
    [Range(0, 2)]
    public int ApprovalLevels { get; set; }
    [MaxLength(200)]
    public string? AllowedExtensions { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
