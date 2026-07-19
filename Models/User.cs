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


  //  public bool IsTyping { get; set; } = false;
   // public int? TypingToUserId { get; set; }
   // public DateTime? TypingUpdatedAt { get; set; }
}