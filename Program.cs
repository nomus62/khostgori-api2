using Microsoft.EntityFrameworkCore;

using KhostgoriAPI.Data;

// ========== 1. СОЗДАНИЕ BUILDER С ОПЦИЯМИ ==========
var options = new WebApplicationOptions
{
    Args = args,
    WebRootPath = "wwwroot" // ⬅️ ПРАВИЛЬНОЕ МЕСТО ДЛЯ УКАЗАНИЯ WWWROOT
};
var builder = WebApplication.CreateBuilder(options);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// База данных
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Контроллеры
builder.Services.AddControllers();

// MemoryCache
builder.Services.AddMemoryCache();

// ========== 2. ПОСТРОЕНИЕ ПРИЛОЖЕНИЯ ==========
var app = builder.Build();

// ⭐ СОЗДАЁМ ПАПКУ ДЛЯ ФОТО
var photosPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos");
if (!Directory.Exists(photosPath))
{
    Directory.CreateDirectory(photosPath);
    Console.WriteLine($"=== ПАПКА СОЗДАНА: {photosPath} ===");
}

// ========== 3. МИГРАЦИИ ==========
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        Console.WriteLine("✅ Миграции успешно применены.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Ошибка при миграции: {ex.Message}");
}

// ========== 4. ВКЛЮЧАЕМ СТАТИЧЕСКИЕ ФАЙЛЫ ==========
app.UseStaticFiles();

// ========== 5. MIDDLEWARE ==========
app.UseCors("AllowAll");
app.MapControllers();

// ========== 6. ЗАПУСК ==========
app.Run();