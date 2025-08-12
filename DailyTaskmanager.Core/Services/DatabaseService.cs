using DailyTaskManager.Core.Models;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace DailyTaskManager.Core.Services
{
    public static class DatabaseService
    {
        public static async Task AddTaskAsync(DailyTask task)
        {
            using (var context = new DailyTaskManagerDbContext())
            {
                context.DailyTasks.Add(task);
                await context.SaveChangesAsync();
            }
        }

        public static async Task<List<DailyTask>> GetTasksAsync()
        {
            using (var context = new DailyTaskManagerDbContext())
            {
                return await context.DailyTasks.ToListAsync();
            }
        }

        public static async Task RemoveTaskAsync(int id)
        {
            using (var context = new DailyTaskManagerDbContext())
            {
                var task = await context.DailyTasks.Where(x => x.Id == id).FirstOrDefaultAsync();
                context.DailyTasks.Remove(task);
                await context.SaveChangesAsync();
            }
        }
    }
}
