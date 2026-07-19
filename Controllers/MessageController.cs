using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using KhostgoriAPI.Data;
using KhostgoriAPI.Models;

namespace KhostgoriAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly AppDbContext _context;

    public MessageController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetMessages([FromQuery] int userId, [FromQuery] int partnerId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            var partner = await _context.Users.FirstOrDefaultAsync(u => u.Id == partnerId);
            if (partner == null) return NotFound();

            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.Id == user.ProfileId);
            var partnerProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.Id == partner.ProfileId);

            if (userProfile == null || partnerProfile == null) return NotFound();

            var chatId = GetChatId(userProfile.Id, partnerProfile.Id);

            var messages = await _context.Messages
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.SentDate)
                .ToListAsync();

            return Ok(messages);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] Message message)
    {
        try
        {
            message.SentDate = DateTime.UtcNow;
            message.IsRead = false;
            message.IsDelivered = true;
            message.ChatId = GetChatId(message.SenderProfileId, message.ReceiverProfileId);

            if (message.ReplyToId.HasValue)
            {
                var original = await _context.Messages
                    .FirstOrDefaultAsync(m => m.Id == message.ReplyToId.Value);
                if (original == null)
                {
                    message.ReplyToId = null;
                }
            }

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, messageId = message.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
        }
    }

    [HttpPut("read")]
    public async Task<IActionResult> MarkAsRead([FromQuery] int profileId, [FromQuery] int partnerId)
    {
        try
        {
            var chatId = GetChatId(profileId, partnerId);
            var messages = await _context.Messages
                .Where(m => m.ChatId == chatId && m.ReceiverProfileId == profileId && !m.IsRead)
                .ToListAsync();

            foreach (var msg in messages)
            {
                msg.IsRead = true;
            }
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
        }
    }

    private int GetChatId(int profileId1, int profileId2)
    {
        return profileId1 < profileId2
            ? int.Parse($"{profileId1}{profileId2:000000}")
            : int.Parse($"{profileId2}{profileId1:000000}");
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteChat([FromQuery] int profileId1, [FromQuery] int profileId2)
    {
        try
        {
            var chatId = GetChatId(profileId1, profileId2);

            // 1. Удаляем все сообщения
            var messages = await _context.Messages
                .Where(m => m.ChatId == chatId)
                .ToListAsync();

            if (messages.Any())
            {
                _context.Messages.RemoveRange(messages);
            }

            // 2. Удаляем лайки (мэтчи)
            var likes = await _context.Likes
                .Where(l => (l.FromProfileId == profileId1 && l.ToProfileId == profileId2) ||
                            (l.FromProfileId == profileId2 && l.ToProfileId == profileId1))
                .ToListAsync();

            if (likes.Any())
            {
                _context.Likes.RemoveRange(likes);
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Чат нест карда шуд" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
        }
    }
}