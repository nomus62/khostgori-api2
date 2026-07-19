using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using KhostgoriAPI.Data;
using KhostgoriAPI.Models;

namespace KhostgoriAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LikeController : ControllerBase
{
    private readonly AppDbContext _context;

    public LikeController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> AddLike([FromQuery] int fromUserId, [FromQuery] int toUserId)
    {
        try
        {
            // ✅ НАХОДИМ ПОЛЬЗОВАТЕЛЕЙ ПО Id (из таблицы Users)
            var fromUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == fromUserId);

            var toUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == toUserId);

            if (fromUser == null || toUser == null)
                return NotFound(new { message = "Пользователь не найден" });

            // ✅ ПОЛУЧАЕМ ПРОФИЛИ
            var fromProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.Id == fromUser.ProfileId);

            var toProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.Id == toUser.ProfileId);

            if (fromProfile == null || toProfile == null)
                return NotFound(new { message = "Профиль не найден" });

            // ✅ ПРОВЕРЯЕМ, ЕСТЬ ЛИ УЖЕ ЛАЙК
            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.FromProfileId == fromProfile.Id && l.ToProfileId == toProfile.Id);

            if (existingLike != null)
                return Ok(new { success = false, message = "Лайк уже отправлен" });

            // ✅ СОЗДАЁМ ЛАЙК
            var like = new Like
            {
                FromProfileId = fromProfile.Id,
                ToProfileId = toProfile.Id,
                Status = "Pending",
                CreatedDate = DateTime.UtcNow
            };

            _context.Likes.Add(like);
            await _context.SaveChangesAsync();

            // ✅ ПРОВЕРЯЕМ ВЗАИМНЫЙ ЛАЙК
            var mutualLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.FromProfileId == toProfile.Id && l.ToProfileId == fromProfile.Id);

            if (mutualLike != null)
            {
                like.Status = "Matched";
                mutualLike.Status = "Matched";
                like.MatchedDate = DateTime.UtcNow;
                mutualLike.MatchedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Это взаимный лайк! 🎉" });
            }

            return Ok(new { success = true, message = "Лайк отправлен" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
        }
    }

    [HttpGet("matches")]
    public async Task<IActionResult> GetMatches([FromQuery] int profileId)
    {
        try
        {
            // Находим все мэтчи
            var likes = await _context.Likes
                .Where(l => (l.FromProfileId == profileId || l.ToProfileId == profileId)
                            && l.Status == "Matched")
                .ToListAsync();

            var matchedIds = new List<int>();
            foreach (var like in likes)
            {
                if (like.FromProfileId == profileId && !matchedIds.Contains(like.ToProfileId))
                    matchedIds.Add(like.ToProfileId);
                else if (like.ToProfileId == profileId && !matchedIds.Contains(like.FromProfileId))
                    matchedIds.Add(like.FromProfileId);
            }

            // Загружаем профили
            var profiles = await _context.UserProfiles
                .Where(p => matchedIds.Contains(p.Id))
                .ToListAsync();

            return Ok(profiles);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
        }
    }

    [HttpGet("hasLiked")]
    public async Task<IActionResult> HasLiked([FromQuery] int fromUserId, [FromQuery] int toUserId)
    {
        try
        {
            // ✅ ИЩЕМ ПО Id (из таблицы Users)
            var fromUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == fromUserId);

            var toUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == toUserId);

            if (fromUser == null || toUser == null)
                return Ok(false);

            // ✅ ПОЛУЧАЕМ ПРОФИЛИ
            var fromProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.Id == fromUser.ProfileId);

            var toProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.Id == toUser.ProfileId);

            if (fromProfile == null || toProfile == null)
                return Ok(false);

            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.FromProfileId == fromProfile.Id && l.ToProfileId == toProfile.Id);

            return Ok(like != null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HasLiked error: {ex.Message}");
            return Ok(false);
        }
    }
}