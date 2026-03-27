using System;
using System.Collections.Generic;

namespace TaskFlow.Api.Models
{
    public class User
    {
        public long Id { get; set; } // Унікальний ідентифікатор користувача
        public string Username { get; set; } = string.Empty; // Нікнейм
        public string Email { get; set; } = string.Empty; // Ел. пошта
        public string PasswordHash { get; set; } = string.Empty; // Хеш пароля
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Зв'язок: Один користувач може мати багато проєктів
        public List<Project> Projects { get; set; } = new();
    }
}