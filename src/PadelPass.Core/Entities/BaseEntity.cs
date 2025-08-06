using System.ComponentModel.DataAnnotations;

namespace PadelPass.Core.Entities;

public abstract class BaseEntity
{
    [Key]
    public int Id { get; protected set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [StringLength(50)]
    public string? UpdatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}