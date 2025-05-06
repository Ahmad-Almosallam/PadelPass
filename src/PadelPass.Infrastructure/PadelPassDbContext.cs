using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PadelPass.Core.Entities;
using PadelPass.Core.Services;

namespace PadelPass.Infrastructure;

public class PadelPassDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentUserService _currentUserService;
    
    
    public PadelPassDbContext(DbContextOptions<PadelPassDbContext> options,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    // public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<Club> Clubs { get; set; }
    public DbSet<NonPeakSlot> NonPeakSlots { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var userId = _currentUserService.UserId ?? string.Empty;// resolve current user
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }
        return await base.SaveChangesAsync(ct);
    }
}