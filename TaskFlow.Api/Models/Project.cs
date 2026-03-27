using System;

namespace TaskFlow.Api.Models
{
    public class Project
    {
        public long Id { get; set; } // Унікальний ідентифікатор проєкту
        public long UserId { get; set; } // FK -> users.id (Юзер, який створив проєкт)
        public string Title { get; set; } = string.Empty; // Назва проєкту
        public string Description { get; set; } = string.Empty; // Опис проєкту
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Навігаційна властивість для зв'язку з User
        public User? User { get; set; }
    }
}