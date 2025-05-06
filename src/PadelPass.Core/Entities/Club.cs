using System.ComponentModel.DataAnnotations;

namespace PadelPass.Core.Entities;

public class Club : BaseEntity
{
    [Required] [StringLength(200)] public string Name { get; set; }

    [StringLength(500)] public string Address { get; set; }

    [Range(-90, 90)] public double? Latitude { get; set; }

    [Range(-180, 180)] public double? Longitude { get; set; }

    public ICollection<NonPeakSlot> NonPeakSlots { get; set; } = new List<NonPeakSlot>();
}