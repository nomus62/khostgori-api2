using Microsoft.EntityFrameworkCore;
using KhostgoriAPI.Data;
var builder = WebApplication.CreateBuilder(args);

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

// Swagger (если нужен)
// builder.Services.AddSwaggerGen();
// builder.Services.AddEndpointsApiExplorer();

// ========== 2. ПОСТРОЕНИЕ ПРИЛОЖЕНИЯ ==========
var app = builder.Build();

// ⭐ СОЗДАЁМ ПАПКУ ПРИ КАЖДОМ ЗАПУСКЕ
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
    // Не падаем, если миграция не удалась
}

// ========== 4. SWAGGER ==========
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

// ========== 5. MIDDLEWARE ==========
app.UseCors("AllowAll");
app.MapControllers();

// ========== 6. ЗАПУСК ==========
app.Run();