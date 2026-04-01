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
    [Authorize] // Тепер доступно всім авторизованим юзерам, не тільки адмінам
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
            var userId = GetUserId();
            if (userId == -1) return Unauthorized();

            // ПЕРЕВІРКА ЗА ТЗ: Чи належить завдання цьому юзеру?
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == dto.TaskId && t.UserId == userId);

            if (task == null) 
                return BadRequest("Завдання не знайдено або у вас немає прав доступу до нього.");

            // ВАЛІДАЦІЯ ЗА ТЗ: Час має бути додатним
            if (dto.TimeSpent <= 0)
                return BadRequest("Час має бути більшим за 0.");

            var newLog = new TimeLog
            {
                TaskId = dto.TaskId,
                UserId = userId, 
                TimeSpent = dto.TimeSpent, // У хвилинах (INT)
                Comment = dto.Comment,
                LoggedAt = DateTime.UtcNow
            };

            _context.TimeLogs.Add(newLog);
            
            // Оновлюємо дату зміни завдання
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(newLog);
        }

        // 2. ОТРИМАННЯ ЛОГІВ (ДЛЯ ТРЕКІНГУ В ДЕТАЛЯХ ЗАВДАННЯ)
        [HttpGet]
        public async Task<IActionResult> GetMyLogs()
        {
            var userId = GetUserId();
            if (userId == -1) return Unauthorized();

            // Юзер бачить тільки СВОЇ записи часу
            var logs = await _context.TimeLogs
                .Where(l => l.UserId == userId)
                .Include(l => l.Task) 
                .OrderByDescending(l => l.LoggedAt)
                .ToListAsync();

            return Ok(logs);
        }

        // 3. ВИДАЛЕННЯ ЗАПИСУ ЧАСУ
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLog(long id)
        {
            var userId = GetUserId();
            
            // Шукаємо лог, який належить саме цьому юзеру
            var log = await _context.TimeLogs.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

            if (log == null) 
                return NotFound("Запис не знайдено або ви не маєте прав на його видалення.");

            _context.TimeLogs.Remove(log);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Запис часу видалено." });
        }

        private long GetUserId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdString, out long userId) ? userId : -1;
        }
    }
}