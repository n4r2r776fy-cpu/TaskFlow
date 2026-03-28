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
    [Authorize] // Тільки для залогінених користувачів
    public class TimeLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TimeLogsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. ДОДАВАННЯ ЗАПИСУ ЧАСУ
        [HttpPost]
        public async Task<IActionResult> AddTimeLog([FromBody] TimeLogCreateDto dto)
        {
            // Дістаємо ID користувача з токена
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdString, out long userId)) 
                return Unauthorized();

            // Перевіряємо: чи існує завдання І чи належить воно саме цьому юзеру
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == dto.TaskId && t.UserId == userId);

            if (task == null) 
                return BadRequest("Завдання не знайдено або у вас немає до нього доступу.");

            var newLog = new TimeLog
            {
                TaskId = dto.TaskId,
                UserId = userId, // Обов'язково записуємо, хто додав час
                TimeSpent = dto.TimeSpent,
                Comment = dto.Comment,
                LoggedAt = DateTime.UtcNow
            };

            _context.TimeLogs.Add(newLog);
            await _context.SaveChangesAsync();

            return Ok(newLog);
        }

        // 2. ОТРИМАННЯ ВСІХ ЛОГІВ ДЛЯ КОНКРЕТНОГО ЗАВДАННЯ
        [HttpGet("task/{taskId}")]
        public async Task<IActionResult> GetLogsForTask(long taskId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdString, out long userId)) 
                return Unauthorized();

            // Повертаємо логи тільки якщо вони належать цьому завданню ТА цьому юзеру
            var logs = await _context.TimeLogs
                .Where(l => l.TaskId == taskId && l.UserId == userId)
                .OrderByDescending(l => l.LoggedAt)
                .ToListAsync();

            return Ok(logs);
        }

        // 3. ВИДАЛЕННЯ ЗАПИСУ ЧАСУ (якщо помилився при введенні)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLog(long id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdString, out long userId)) 
                return Unauthorized();

            var log = await _context.TimeLogs
                .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

            if (log == null) 
                return NotFound("Запис не знайдено.");

            _context.TimeLogs.Remove(log);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Запис часу видалено." });
        }
    }
}