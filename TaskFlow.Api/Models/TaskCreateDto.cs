namespace TaskFlow.Api.Models
{
    public class TaskCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long? ProjectId { get; set; } // Може бути порожнім
        public string Priority { get; set; } = "medium"; // low, medium, high
    }
}