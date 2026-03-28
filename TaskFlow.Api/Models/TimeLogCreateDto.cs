namespace TaskFlow.Api.Models
{
    public class TimeLogCreateDto
    {
        public long TaskId { get; set; } // ID завдання, над яким працювали
        public int TimeSpent { get; set; } // Витрачений час у хвилинах (наприклад, 120 = 2 години)
        public string Comment { get; set; } = string.Empty; // Що саме робили
    }
}