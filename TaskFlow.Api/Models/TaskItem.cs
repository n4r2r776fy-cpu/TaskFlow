using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TaskFlow.Api.Models
{
    public class TaskItem
    {
        public long Id { get; set; } 
        
        // Зв'язок з користувачем (тепер обов'язковий)
        public long UserId { get; set; } 
        
        // Зв'язок з проєктом (може бути порожнім, якщо завдання "загальне")
        public long? ProjectId { get; set; } 
        
        public string Title { get; set; } = string.Empty; 
        public string Description { get; set; } = string.Empty; 
        public string Status { get; set; } = "todo"; 
        public string Priority { get; set; } = "medium"; 
        
        public DateTime? DueDate { get; set; } 
        public DateTime? CompletedAt { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Ігноруємо ці поля при відправці JSON на фронтенд, 
        // щоб не було нескінченних циклів
        
        [JsonIgnore]
        public User? User { get; set; }
        
        [JsonIgnore]
        public Project? Project { get; set; }
        
        [JsonIgnore]
        public List<TimeLog> TimeLogs { get; set; } = new();
    }
}