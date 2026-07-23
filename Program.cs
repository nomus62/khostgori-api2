using Microsoft.EntityFrameworkCore;

using Supabase;
using KhostgoriAPI.Data;


var builder = WebApplication.CreateBuilder(args);

// ⭐ ДОБАВЛЯЕМ SUPABASE CLIENT
var supabaseUrl = builder.Configuration["Supabase:Url"]!;
var supabaseKey = builder.Configuration["Supabase:Key"]!;
builder.Services.AddScoped(_ => new Supabase.Client(supabaseUrl, supabaseKey));

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

// ✅ MemoryCache (ДО Build!)
builder.Services.AddMemoryCache();

// ========== ПОСТРОЕНИЕ ПРИЛОЖЕНИЯ ==========
var app = builder.Build();

// ⭐ СОЗДАЁМ ПАПКУ ДЛЯ ФОТО
var photosPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos");
if (!Directory.Exists(photosPath))
{
    Directory.CreateDirectory(photosPath);
    Console.WriteLine($"=== ПАПКА СОЗДАНА: {photosPath} ===");
}

// ========== МИГРАЦИИ ==========
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

// ========== ⭐ КЛЮЧЕВОЙ МОМЕНТ: ВКЛЮЧАЕМ СТАТИЧЕСКИЕ ФАЙЛЫ ==========
app.UseStaticFiles(); // ⬅️ БЕЗ ЭТОГО ФОТО НЕ БУДУТ ОТДАВАТЬСЯ!

// ========== MIDDLEWARE ==========
app.UseCors("AllowAll");
app.MapControllers();

// ========== ЗАПУСК ==========
app.Run();