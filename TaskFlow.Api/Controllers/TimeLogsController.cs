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
    [Authorize(Roles = "Admin")] // ТІЛЬКИ ДЛЯ АДМІНІВ
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
            // Дістаємо ID адміністратора з токена
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdString, out long userId)) 
                return Unauthorized();

            // Оскільки це Адмін, він може додавати час до БУДЬ-ЯКОГО завдання
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == dto.TaskId);

            if (task == null) 
                return BadRequest("Завдання не знайдено.");

            var newLog = new TimeLog
            {
                TaskId = dto.TaskId,
                UserId = userId, // Записуємо, що саме цей адмін додав час
                TimeSpent = dto.TimeSpent, 
                Comment = dto.Comment,
                LoggedAt = DateTime.UtcNow
            };

            _context.TimeLogs.Add(newLog);
            await _context.SaveChangesAsync();

            return Ok(newLog);
        }

        // 2. ОТРИМАННЯ ВСІХ ЛОГІВ (ДЛЯ ТАБЛИЦІ АДМІНА)
        [HttpGet]
        public async Task<IActionResult> GetAllLogs()
        {
            // Адмін бачить ВСІ записи часу. 
            // Використовуємо Include, щоб підтягнути дані про завдання (для відображення назви)
            var logs = await _context.TimeLogs
                .Include(l => l.Task) 
                .OrderByDescending(l => l.LoggedAt)
                .ToListAsync();

            return Ok(logs);
        }

        // 3. ВИДАЛЕННЯ ЗАПИСУ ЧАСУ
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLog(long id)
        {
            // Адмін може видалити БУДЬ-ЯКИЙ запис, тому шукаємо лише за Id логу
            var log = await _context.TimeLogs.FirstOrDefaultAsync(l => l.Id == id);

            if (log == null) 
                return NotFound("Запис не знайдено.");

            _context.TimeLogs.Remove(log);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Запис часу видалено." });
        }
    }
}