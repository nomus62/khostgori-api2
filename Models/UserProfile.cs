namespace KhostgoriAPI.Models;

public class UserProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string About { get; set; } = string.Empty;
    public string ZodiacSign { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string PhotoPaths { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; } // ⬅️ ДОБАВЛЯЕМ
}