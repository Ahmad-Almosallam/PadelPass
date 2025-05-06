using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PadelPass.Core.Entities;

public class PaymentTransaction : BaseEntity
{
    [Required] public int SubscriptionId { get; set; }

    [ForeignKey(nameof(SubscriptionId))] public Subscription Subscription { get; set; }

    [Required] [StringLength(50)] public string TransactionId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required] [StringLength(20)] public string Status { get; set; }

    [Required] public DateTime TransactionDate { get; set; }
}