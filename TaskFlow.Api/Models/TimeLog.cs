using System;

namespace TaskFlow.Api.Models
{
    public class TimeLog
    {
        public long Id { get; set; } // Унікальний ідентифікатор логу
        public long TaskId { get; set; } // FK -> tasks.id
        public long UserId { get; set; } // FK -> users.id
        
        public int TimeSpent { get; set; } // Витрачений час у хвилинах
        public string Comment { get; set; } = string.Empty; // Коментар до роботи
        public DateTime LoggedAt { get; set; } = DateTime.UtcNow; // Час створення логу

        // Навігаційні властивості
        public TaskItem? Task { get; set; }
        public User? User { get; set; }
    }
}