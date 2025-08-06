using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using PadelPass.Core.Common.Enums;

namespace PadelPass.Core.Entities;

public class ApplicationUser : IdentityUser
{
    [Required, MaxLength(20)]
    public string PhoneNumber { get; set; }
    
    [StringLength(100)]
    public string FullName { get; set; }

    public UserType UserType { get; set; }
    public int? CurrentSubscriptionId { get; set; }

    [ForeignKey(nameof(CurrentSubscriptionId))]
    public Subscription CurrentSubscription { get; set; }
}