using System;
using System.Collections.Generic;
using System.Text.Json.Serialization; // ДОДАЛИ ЦЕ

namespace TaskFlow.Api.Models
{
    public class TaskItem
    {
        public long Id { get; set; } 
        public long UserId { get; set; } 
        public long? ProjectId { get; set; } 
        
        public string Title { get; set; } = string.Empty; 
        public string Description { get; set; } = string.Empty; 
        public string Status { get; set; } = "todo"; 
        public string Priority { get; set; } = "medium"; 
        
        public DateTime? DueDate { get; set; } 
        public DateTime? CompletedAt { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ДОДАЛИ [JsonIgnore] сюди:
        [JsonIgnore]
        public User? User { get; set; }
        
        [JsonIgnore]
        public Project? Project { get; set; }
        
        [JsonIgnore]
        public List<TimeLog> TimeLogs { get; set; } = new();
    }
}