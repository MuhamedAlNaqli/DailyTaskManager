using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Core.Interfaces
{
    /// <summary>
    /// Interface for database operations related to daily tasks
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// Adds a new task to the database
        /// </summary>
        Task<DailyTask> AddTaskAsync(DailyTask task);

        /// <summary>
        /// Gets all tasks for the current authenticated user
        /// </summary>
        Task<List<DailyTask>> GetTasksAsync();

        /// <summary>
        /// Gets tasks for a specific date range
        /// </summary>
        Task<List<DailyTask>> GetTasksAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets one page of the current user's tasks matching the query (newest first)
        /// </summary>
        Task<PagedResult<DailyTask>> GetTasksPageAsync(TaskQuery query, int page, int pageSize);

        /// <summary>
        /// Gets every task matching the query (used by exports)
        /// </summary>
        Task<List<DailyTask>> GetTasksAsync(TaskQuery query);

        /// <summary>
        /// Gets today / this week / 30-day average hours, computed in the database
        /// </summary>
        Task<TaskSummary> GetTaskSummaryAsync();

        /// <summary>
        /// Gets a specific task by ID
        /// </summary>
        Task<DailyTask?> GetTaskByIdAsync(int id);

        /// <summary>
        /// Updates an existing task
        /// </summary>
        Task<DailyTask> UpdateTaskAsync(DailyTask task);

        /// <summary>
        /// Removes a task by ID
        /// </summary>
        Task<bool> RemoveTaskAsync(int id);

        /// <summary>
        /// Gets total hours for a specific date
        /// </summary>
        Task<decimal> GetTotalHoursAsync(DateTime date);

        /// <summary>
        /// Gets task statistics for the current user
        /// </summary>
        Task<(int TotalTasks, decimal TotalHours, decimal AverageHours)> GetTaskStatsAsync();

        /// <summary>
        /// Gets unique task descriptions for auto-complete functionality
        /// </summary>
        Task<List<string>> GetUniqueDescriptionsAsync();
    }
}