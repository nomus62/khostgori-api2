namespace KhostgoriAPI.Models;

public class Message
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public int SenderProfileId { get; set; }
    public int ReceiverProfileId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentDate { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public bool IsDelivered { get; set; }

    public int? ReplyToId { get; set; } // Nullable for messages that are not replies
}