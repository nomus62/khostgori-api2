using KhostgoriAPI.Data;
using KhostgoriAPI.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace KhostgoriAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    private readonly IMemoryCache _cache;

    public AuthController(AppDbContext context, IConfiguration configuration, IMemoryCache cache)
    {
        _context = context;
        _configuration = configuration;
        _cache = cache;
    }

    // ========== ТЕСТ ==========
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { message = "Server is running!" });
    }

    // ========== РЕГИСТРАЦИЯ ==========
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Проверка существования
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == request.Login);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Пользователь уже существует" });
            }

            // Хеширование пароля
            var passwordHash = HashPassword(request.Password);

            // Создание пользователя
            var user = new User
            {
                Login = request.Login,
                PasswordHash = passwordHash,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Создание профиля
            var profile = request.Profile;
            profile.UserId = user.Id;
            profile.IsActive = true;
            profile.CreatedDate = DateTime.UtcNow;

            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync();

            // Обновление пользователя (связь с профилем)
            user.ProfileId = profile.Id;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Регистрация успешна" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
        }
    }

    // ========== ВХОД ==========
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == request.Login);

            if (user == null)
            {
                return Unauthorized(new { success = false, message = "Пользователь не найден" });
            }

            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { success = false, message = "Неверный пароль" });
            }

            var token = GenerateJwtToken(user);

            return Ok(new LoginResponse
            {
                Success = true,
                Token = token,
                UserId = user.Id,
                Message = "Вход выполнен"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
        }
    }


    [HttpGet("typing/{receiverProfileId}")]
    public IActionResult GetTypingStatus(int receiverProfileId)
    {
        try
        {
            var key = $"typing_{receiverProfileId}";
            var isTyping = _cache.TryGetValue(key, out int _);

            return Ok(new { isTyping });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
        }
    }


    // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ========== ОНЛАЙН СТАТУС (ДОБАВИТЬ В КОНЦЕ ФАЙЛА) ==========

    [HttpPost("online")]
    public async Task<IActionResult> SetOnline([FromQuery] int profileId, [FromQuery] bool isOnline)  // ⬅️ profileId
    {
        try
        {
            // ✅ НАХОДИМ ПОЛЬЗОВАТЕЛЯ ПО ProfileId
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ProfileId == profileId);  // ⬅️ ИЩЕМ ПО ProfileId

            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            user.IsOnline = isOnline;
            user.LastSeen = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
        }
    }

    [HttpGet("online/{profileId}")]
    public async Task<IActionResult> GetOnlineStatus(int profileId)
    {
        try
        {
            // ✅ СНАЧАЛА НАХОДИМ USER ПО PROFILEID
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ProfileId == profileId);

            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            return Ok(new
            {
                isOnline = user.IsOnline,
                lastSeen = user.LastSeen
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
        }
    }

    // ========== СТАТУС "ПЕЧАТАЕТ" (ИСПРАВЛЕННЫЙ) ==========

    [HttpPost("typing")]
    public IActionResult SetTyping(
      [FromQuery] int senderProfileId,
      [FromQuery] int receiverProfileId,
      [FromQuery] bool isTyping)
    {
        try
        {
            var key = $"typing_{senderProfileId}_{receiverProfileId}";

            if (isTyping)
            {
                _cache.Set(key, DateTime.UtcNow, TimeSpan.FromSeconds(5));
            }
            else
            {
                _cache.Remove(key);
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
        }
    }

    [HttpGet("typing/{senderProfileId}/{receiverProfileId}")]
    public IActionResult GetTypingStatus(int senderProfileId, int receiverProfileId)
    {
        try
        {
            var key = $"typing_{senderProfileId}_{receiverProfileId}";

            if (_cache.TryGetValue(key, out DateTime timestamp))
            {
                var elapsed = (DateTime.UtcNow - timestamp).TotalSeconds;
                if (elapsed < 5)
                {
                    return Ok(new { isTyping = true });
                }
                _cache.Remove(key);
            }

            return Ok(new { isTyping = false });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Ошибка: {ex.Message}" });
        }
    }

    // ========== DTO ==========

    public class LoginRequest
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserProfile Profile { get; set; } = new();
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
        public int UserId { get; set; }
    }
}