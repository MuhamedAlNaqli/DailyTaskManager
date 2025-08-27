using DailyTaskManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace DailyTaskManager.Core.Services
{
    public static class DatabaseService
    {
        private static int GetCurrentUserId()
        {
            var authService = ServiceContainer.Instance.Resolve<IAuthenticationService>();
            if (!authService.IsAuthenticated || authService.CurrentUser == null)
                throw new InvalidOperationException("User must be authenticated to access tasks");
            
            return authService.CurrentUser.Id;
        }

        public static async Task AddTaskAsync(DailyTask task)
        {
            task.UserId = GetCurrentUserId();
            
            using (var context = new DailyTaskManagerDbContext())
            {
                context.DailyTasks.Add(task);
                await context.SaveChangesAsync();
            }
        }

        public static async Task<List<DailyTask>> GetTasksAsync()
        {
            var userId = GetCurrentUserId();
            
            using (var context = new DailyTaskManagerDbContext())
            {
                return await context.DailyTasks
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.Date)
                    .ThenByDescending(t => t.Id)
                    .ToListAsync();
            }
        }

        public static async Task RemoveTaskAsync(int id)
        {
            var userId = GetCurrentUserId();
            
            using (var context = new DailyTaskManagerDbContext())
            {
                var task = await context.DailyTasks
                    .Where(x => x.Id == id && x.UserId == userId)
                    .FirstOrDefaultAsync();
                
                if (task != null)
                {
                    context.DailyTasks.Remove(task);
                    await context.SaveChangesAsync();
                }
            }
        }

        public static async Task UpdateTaskAsync(DailyTask task)
        {
            var userId = GetCurrentUserId();
            
            using (var db = new DailyTaskManagerDbContext())
            {
                var existing = await db.DailyTasks
                    .Where(t => t.Id == task.Id && t.UserId == userId)
                    .FirstOrDefaultAsync();
                
                if (existing != null)
                {
                    existing.Description = task.Description;
                    existing.Hours = task.Hours;
                    existing.Date = task.Date;
                    await db.SaveChangesAsync();
                }
            }
        }

    }
}
