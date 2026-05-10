using Microsoft.AspNetCore.Identity;

namespace LiftAI.Api.Data.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public bool IsPremium { get; set; } = false; 
    public int ChatDailyCount { get; set; } = 0;
    public DateTime? ChatDailyLastResetUtc { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}