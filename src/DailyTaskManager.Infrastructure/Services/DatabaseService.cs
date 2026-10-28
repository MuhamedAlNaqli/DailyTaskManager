using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;
using DailyTaskManager.Infrastructure.Data;

namespace DailyTaskManager.Infrastructure.Services
{
    /// <summary>
    /// Service for database operations related to daily tasks
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private readonly IDbContextFactory<DailyTaskManagerDbContext> _contextFactory;
        private readonly IAuthenticationService _authService;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(
            IDbContextFactory<DailyTaskManagerDbContext> contextFactory, 
            IAuthenticationService authService,
            ILogger<DatabaseService> logger)
        {
            _contextFactory = contextFactory;
            _authService = authService;
            _logger = logger;
        }

        public async Task<DailyTask> AddTaskAsync(DailyTask task)
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                task.UserId = GetCurrentUserId();
                task.CreatedAt = DateTime.UtcNow;
                task.UpdatedAt = DateTime.UtcNow;
                
                // Convert Date to UTC to avoid PostgreSQL timezone issues
                task.Date = DateTime.SpecifyKind(task.Date.Date, DateTimeKind.Utc);

                _context.DailyTasks.Add(task);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Added new task with ID {TaskId} for user {UserId}", 
                    task.Id, task.UserId);

                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding task for user {UserId}", GetCurrentUserId());
                throw;
            }
        }

        public async Task<List<DailyTask>> GetTasksAsync()
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                var userId = GetCurrentUserId();

                var tasks = await _context.DailyTasks
                    .AsNoTracking()
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.Date)
                    .ThenByDescending(t => t.CreatedAt)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {TaskCount} tasks for user {UserId}", 
                    tasks.Count, userId);

                return tasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks for user {UserId}", GetCurrentUserId());
                throw;
            }
        }

        public async Task<List<DailyTask>> GetTasksAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                var userId = GetCurrentUserId();

                var tasks = await _context.DailyTasks
                    .AsNoTracking()
                    .Where(t => t.UserId == userId && 
                               t.Date >= startDate.Date && 
                               t.Date <= endDate.Date)
                    .OrderByDescending(t => t.Date)
                    .ThenByDescending(t => t.CreatedAt)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {TaskCount} tasks for user {UserId} between {StartDate} and {EndDate}", 
                    tasks.Count, userId, startDate, endDate);

                return tasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks for user {UserId} in date range {StartDate}-{EndDate}", 
                    GetCurrentUserId(), startDate, endDate);
                throw;
            }
        }

        public async Task<PagedResult<DailyTask>> GetTasksPageAsync(TaskQuery query, int page, int pageSize)
        {
            await using var _context = _contextFactory.CreateDbContext();
            var userId = GetCurrentUserId();
            page = Math.Max(1, page);
            pageSize = Math.Max(1, pageSize);

            var filtered = ApplyQuery(_context.DailyTasks.AsNoTracking().Where(t => t.UserId == userId), query);
            var total = await filtered.CountAsync();

            // Stay on the last page when entries were deleted or the filter narrowed
            var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Min(page, lastPage);

            var items = await filtered
                .Include(t => t.Client)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<DailyTask>(items, total, page, pageSize);
        }

        public async Task<List<DailyTask>> GetTasksAsync(TaskQuery query)
        {
            await using var _context = _contextFactory.CreateDbContext();
            var userId = GetCurrentUserId();

            return await ApplyQuery(_context.DailyTasks.AsNoTracking().Where(t => t.UserId == userId), query)
                .Include(t => t.Client)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<TaskSummary> GetTaskSummaryAsync()
        {
            await using var _context = _contextFactory.CreateDbContext();
            var userId = GetCurrentUserId();

            var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var thirtyDaysAgo = today.AddDays(-30);
            var tomorrow = today.AddDays(1);
            var since = startOfWeek < thirtyDaysAgo ? startOfWeek : thirtyDaysAgo;

            // One small query over the recent window instead of the whole table
            var rows = await _context.DailyTasks
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.Date >= since && t.Date < tomorrow)
                .Select(t => new { t.Date, t.Hours })
                .ToListAsync();

            var todayHours = rows.Where(t => t.Date.Date == today).Sum(t => t.Hours);
            var weekHours = rows.Where(t => t.Date.Date >= startOfWeek).Sum(t => t.Hours);
            var last30 = rows.Where(t => t.Date.Date >= thirtyDaysAgo).ToList();
            var days = last30.Select(t => t.Date.Date).Distinct().Count();
            var average = days > 0 ? last30.Sum(t => t.Hours) / days : 0m;

            return new TaskSummary(todayHours, weekHours, average);
        }

        private static IQueryable<DailyTask> ApplyQuery(IQueryable<DailyTask> source, TaskQuery query)
        {
            if (!string.IsNullOrWhiteSpace(query.Search) && query.Search.Trim().Length >= 3)
            {
                // Escape LIKE wildcards so "50%" or "a_b" match literally
                var term = query.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
                var pattern = $"%{term}%";
                source = source.Where(t => EF.Functions.ILike(t.Description, pattern));
            }

            if (query.ClientId == TaskQuery.Unassigned)
            {
                source = source.Where(t => t.ClientId == null);
            }
            else if (query.ClientId.HasValue)
            {
                var clientId = query.ClientId.Value;
                source = source.Where(t => t.ClientId == clientId);
            }

            if (query.DateFrom.HasValue)
            {
                // task_date is a SQL date: Unspecified maps to "timestamp", so no time zone shifts the comparison
                var from = DateTime.SpecifyKind(query.DateFrom.Value.Date, DateTimeKind.Unspecified);
                source = source.Where(t => t.Date >= from);
            }

            if (query.DateTo.HasValue)
            {
                var toExclusive = DateTime.SpecifyKind(query.DateTo.Value.Date.AddDays(1), DateTimeKind.Unspecified);
                source = source.Where(t => t.Date < toExclusive);
            }

            return source;
        }

        public async Task<List<Client>> GetClientsAsync()
        {
            await using var _context = _contextFactory.CreateDbContext();
            var userId = GetCurrentUserId();

            return await _context.Clients.AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Client> GetOrCreateClientAsync(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0) throw new ArgumentException("Client name is required", nameof(name));
            if (name.Length > 150) name = name.Substring(0, 150);

            await using var _context = _contextFactory.CreateDbContext();
            var userId = GetCurrentUserId();

            var lower = name.ToLower();
            var existing = await _context.Clients.AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Name.ToLower() == lower);
            if (existing != null) return existing;

            var client = new Client { UserId = userId, Name = name, CreatedAt = DateTime.UtcNow };
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created client {ClientId} for user {UserId}", client.Id, userId);
            return client;
        }

        public async Task<int> AssignClientAsync(TaskQuery query, int? clientId)
        {
            await using var _context = _contextFactory.CreateDbContext();
            var userId = GetCurrentUserId();

            if (clientId.HasValue && !await _context.Clients.AnyAsync(c => c.Id == clientId.Value && c.UserId == userId))
            {
                throw new InvalidOperationException("Client not found for current user");
            }

            var count = await ApplyQuery(_context.DailyTasks.Where(t => t.UserId == userId), query)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.ClientId, clientId));

            _logger.LogInformation("Assigned client {ClientId} to {Count} tasks for user {UserId}", clientId, count, userId);
            return count;
        }

        public async Task<DailyTask> RestoreTaskAsync(DailyTask task)
        {
            await using var _context = _contextFactory.CreateDbContext();
            var restored = new DailyTask
            {
                UserId = GetCurrentUserId(),
                Date = DateTime.SpecifyKind(task.Date.Date, DateTimeKind.Utc),
                Description = task.Description,
                Hours = task.Hours,
                ClientId = task.ClientId,
                CreatedAt = task.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };
            _context.DailyTasks.Add(restored);
            await _context.SaveChangesAsync();
            return restored;
        }

        public async Task<DailyTask?> GetTaskByIdAsync(int id)
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                var userId = GetCurrentUserId();

                var task = await _context.DailyTasks
                    .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

                if (task == null)
                {
                    _logger.LogWarning("Task with ID {TaskId} not found for user {UserId}", id, userId);
                }

                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task {TaskId} for user {UserId}", id, GetCurrentUserId());
                throw;
            }
        }

        public async Task<DailyTask> UpdateTaskAsync(DailyTask task)
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                var userId = GetCurrentUserId();
                var existingTask = await _context.DailyTasks
                    .FirstOrDefaultAsync(t => t.Id == task.Id && t.UserId == userId);

                if (existingTask == null)
                {
                    throw new InvalidOperationException($"Task with ID {task.Id} not found for current user");
                }

                // Update properties
                existingTask.Date = DateTime.SpecifyKind(task.Date.Date, DateTimeKind.Utc);
                existingTask.Description = task.Description;
                existingTask.Hours = task.Hours;
                existingTask.ClientId = task.ClientId;
                existingTask.UpdateTimestamp();

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated task with ID {TaskId} for user {UserId}", 
                    task.Id, userId);

                return existingTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task {TaskId} for user {UserId}", 
                    task.Id, GetCurrentUserId());
                throw;
            }
        }

        public async Task<bool> RemoveTaskAsync(int id)
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                var userId = GetCurrentUserId();
                var task = await _context.DailyTasks
                    .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

                if (task == null)
                {
                    _logger.LogWarning("Task with ID {TaskId} not found for user {UserId}", id, userId);
                    return false;
                }

                _context.DailyTasks.Remove(task);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Removed task with ID {TaskId} for user {UserId}", id, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing task {TaskId} for user {UserId}", id, GetCurrentUserId());
                throw;
            }
        }

        public async Task<decimal> GetTotalHoursAsync(DateTime date)
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                var userId = GetCurrentUserId();

                var totalHours = await _context.DailyTasks
                    .Where(t => t.UserId == userId && t.Date.Date == date.Date)
                    .SumAsync(t => t.Hours);

                _logger.LogDebug("Total hours for user {UserId} on {Date}: {TotalHours}", 
                    userId, date.Date, totalHours);

                return totalHours;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total hours for user {UserId} on {Date}", 
                    GetCurrentUserId(), date);
                throw;
            }
        }

        public async Task<(int TotalTasks, decimal TotalHours, decimal AverageHours)> GetTaskStatsAsync()
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                var userId = GetCurrentUserId();

                var tasks = await _context.DailyTasks
                    .AsNoTracking()
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                var totalTasks = tasks.Count;
                var totalHours = tasks.Sum(t => t.Hours);
                var averageHours = totalTasks > 0 ? totalHours / totalTasks : 0;

                _logger.LogDebug("Task stats for user {UserId}: {TotalTasks} tasks, {TotalHours} total hours, {AverageHours} average hours", 
                    userId, totalTasks, totalHours, averageHours);

                return (totalTasks, totalHours, averageHours);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating task statistics for user {UserId}", GetCurrentUserId());
                throw;
            }
        }

        public async Task<List<string>> GetUniqueDescriptionsAsync()
        {
            try
            {
                await using var _context = _contextFactory.CreateDbContext();
                var userId = GetCurrentUserId();

                var descriptions = await _context.DailyTasks
                    .Where(t => t.UserId == userId)
                    .Select(t => t.Description.Trim())
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {Count} unique descriptions for user {UserId}",
                    descriptions.Count, userId);

                return descriptions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unique descriptions");
                throw;
            }
        }

        private int GetCurrentUserId()
        {
            if (!_authService.IsAuthenticated || _authService.CurrentUser == null)
            {
                throw new InvalidOperationException("User must be authenticated to access tasks");
            }

            return _authService.CurrentUser.Id;
        }
    }
}