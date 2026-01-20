using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Entities;

public class EmailAccount
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }

    // Navigation
    public Profile Profile { get; set; } = null!;
}
