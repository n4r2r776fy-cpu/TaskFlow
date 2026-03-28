using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Потрібен токен!
    public class TimeLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TimeLogsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. ДОДАВАННЯ ВИТРАЧЕНОГО ЧАСУ
        [HttpPost]
        public async Task<IActionResult> AddTimeLog([FromBody] TimeLogCreateDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdString, out long userId)) return Unauthorized();

            // Перевіряємо, чи існує завдання і чи належить воно цьому користувачу
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == dto.TaskId && t.UserId == userId);
            if (task == null) return BadRequest("Завдання не знайдено, або у вас немає до нього доступу.");

            var newLog = new TimeLog
            {
                TaskId = dto.TaskId,
                UserId = userId,
                TimeSpent = dto.TimeSpent,
                Comment = dto.Comment,
                LoggedAt = DateTime.UtcNow
            };

            _context.TimeLogs.Add(newLog);
            await _context.SaveChangesAsync();

            return Ok(newLog);
        }

        // 2. ОТРИМАННЯ ВСІХ ЗАПИСІВ ЧАСУ ДЛЯ КОНКРЕТНОГО ЗАВДАННЯ
        [HttpGet("task/{taskId}")]
        public async Task<IActionResult> GetLogsForTask(long taskId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdString, out long userId)) return Unauthorized();

            // Перевіряємо, чи має юзер доступ до цього завдання
            var taskExists = await _context.Tasks.AnyAsync(t => t.Id == taskId && t.UserId == userId);
            if (!taskExists) return NotFound("Завдання не знайдено.");

            var logs = await _context.TimeLogs
                .Where(l => l.TaskId == taskId)
                .OrderByDescending(l => l.LoggedAt) // Найновіші записи зверху
                .ToListAsync();

            return Ok(logs);
        }
    }
}