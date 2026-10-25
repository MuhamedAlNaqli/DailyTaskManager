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
        private readonly DailyTaskManagerDbContext _context;
        private readonly IAuthenticationService _authService;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(
            DailyTaskManagerDbContext context, 
            IAuthenticationService authService,
            ILogger<DatabaseService> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        public async Task<DailyTask> AddTaskAsync(DailyTask task)
        {
            try
            {
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

        public async Task<DailyTask?> GetTaskByIdAsync(int id)
        {
            try
            {
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