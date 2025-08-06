using PadelPass.Core.Common.Enums;

namespace PadelPass.Core.Entities;

public class DiscountCode : BaseEntity
{
    public string Code { get; set; }
    public string Description { get; set; }
    public DiscountType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MinimumAmount { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; }
}