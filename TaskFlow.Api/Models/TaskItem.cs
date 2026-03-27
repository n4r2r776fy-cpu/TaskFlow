using System;
using System.Collections.Generic;

namespace TaskFlow.Api.Models
{
    public class TaskItem
    {
        public long Id { get; set; } // Унікальний ідентифікатор
        public long UserId { get; set; } // FK -> users.id
        public long? ProjectId { get; set; } // FK -> projects.id (може бути NULL)
        
        public string Title { get; set; } = string.Empty; // Назва
        public string Description { get; set; } = string.Empty; // Опис
        public string Status { get; set; } = "todo"; // todo, in_progress, done
        public string Priority { get; set; } = "medium"; // low, medium, high
        
        public DateTime? DueDate { get; set; } // Дедлайн (може бути ще не встановлений)
        public DateTime? CompletedAt { get; set; } // Фактична дата завершення
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Навігаційні властивості для зв'язків
        public User? User { get; set; }
        public Project? Project { get; set; }
        public List<TimeLog> TimeLogs { get; set; } = new();
    }
}