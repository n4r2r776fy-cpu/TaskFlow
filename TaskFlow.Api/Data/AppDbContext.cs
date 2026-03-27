using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Тут ми кажемо, які таблиці будуть у нашій базі
        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<TimeLog> TimeLogs { get; set; }

        // Налаштовуємо точні назви таблиць, як у твоєму ТЗ
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Project>().ToTable("projects");
            modelBuilder.Entity<TaskItem>().ToTable("tasks");
            modelBuilder.Entity<TimeLog>().ToTable("time_logs");
        }
    }
}