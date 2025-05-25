using System.ComponentModel.DataAnnotations.Schema;

namespace PadelPass.Core.Entities;

public class ClubUser : BaseEntity
{
    public string UserId { get; set; }
    [ForeignKey(nameof(UserId))] public ApplicationUser User { get; set; }

    public int ClubId { get; set; }
    [ForeignKey(nameof(ClubId))] public Club Club { get; set; }
    
    public bool IsActive { get; set; } = true;
}