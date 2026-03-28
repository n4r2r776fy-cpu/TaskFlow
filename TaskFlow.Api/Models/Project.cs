using System;

namespace TaskFlow.Api.Models
{
    public class Project
    {
        public long Id { get; set; } // Унікальний ідентифікатор проєкту

        // Залишаємо long, якщо Id користувача теж long
        public long UserId { get; set; } // FK -> users.id

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Навігаційна властивість
        public User? User { get; set; }
    }
}