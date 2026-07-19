namespace KhostgoriAPI.Models;

public class Like
{
    public int Id { get; set; }
    public int FromProfileId { get; set; }
    public int ToProfileId { get; set; }
    public string Status { get; set; } = "Pending";  // Pending, Matched, Rejected
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? MatchedDate { get; set; }
}