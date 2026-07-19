using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using KhostgoriAPI.Data;
using KhostgoriAPI.Models;

namespace KhostgoriAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProfileController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetProfile(int userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.Id == user.ProfileId);

            if (profile == null)
                return NotFound(new { message = "Профиль не найден" });

            return Ok(profile);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UserProfile profile)
    {
        try
        {
            var existing = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.Id == profile.Id);

            if (existing == null)
                return NotFound();

            // ✅ ОБНОВЛЯЕМ ВСЕ ПОЛЯ
            existing.Name = profile.Name;
            existing.Age = profile.Age;
            existing.Gender = profile.Gender;
            existing.City = profile.City;
            existing.About = profile.About;
            existing.ZodiacSign = profile.ZodiacSign;
            existing.Purpose = profile.Purpose;
            existing.PhotoPaths = profile.PhotoPaths; // ⬅️ ЭТО ВАЖНО!
            existing.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] int userId,
        [FromQuery] string? gender = null,
        [FromQuery] int? minAge = null,
        [FromQuery] int? maxAge = null)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            var query = _context.UserProfiles
                .Where(p => p.IsActive && p.Id != user.ProfileId);

            if (!string.IsNullOrEmpty(gender))
                query = query.Where(p => p.Gender == gender);

            if (minAge.HasValue)
                query = query.Where(p => p.Age >= minAge.Value);

            if (maxAge.HasValue)
                query = query.Where(p => p.Age <= maxAge.Value);

            var profiles = await query.ToListAsync();

            return Ok(profiles);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
        }
    }

    [HttpPost("photo")]
    public async Task<IActionResult> UploadPhoto([FromForm] IFormFile photo, [FromQuery] int userId)
    {
        try
        {
            if (photo == null || photo.Length == 0)
                return BadRequest(new { message = "Фото не выбрано" });

            // ⭐ ГЕНЕРИРУЕМ УНИКАЛЬНОЕ ИМЯ
            var fileName = $"{Guid.NewGuid()}.jpg";
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos");

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, fileName);

            // ⭐ СОХРАНЯЕМ ФАЙЛ
            using var stream = System.IO.File.Create(filePath);
            await photo.CopyToAsync(stream);

            // ⭐ ВОЗВРАЩАЕМ ИМЯ ФАЙЛА
            return Ok(new { fileName = fileName });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
        }
    }
}