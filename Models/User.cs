namespace KhostgoriAPI.Models;

public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int ProfileId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // ⭐ НОВЫЕ ПОЛЯ
    public bool IsOnline { get; set; } = false;
    public DateTime? LastSeen { get; set; }


    // ⭐ НОВЫЕ ПОЛЯ
    public string? PhoneNumber { get; set; }
    public bool IsPhoneVerified { get; set; } = false;
    public string? PhoneVerificationCode { get; set; }
    public DateTime? PhoneVerificationExpiry { get; set; }
}