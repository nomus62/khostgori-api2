using Microsoft.EntityFrameworkCore;

using KhostgoriAPI.Models;

namespace KhostgoriAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Like> Likes { get; set; }  // ✅ ДОБАВЛЕНО
}