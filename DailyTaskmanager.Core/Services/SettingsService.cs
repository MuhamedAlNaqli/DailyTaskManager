using DailyTaskManager.Core.Models;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace DailyTaskManager.Core.Services
{
    public class SettingsService
    {
        public async Task<List<Setting>> GetAllAsync()
        {
            using (var ctx = new DailyTaskManagerDbContext())
            {
                return await ctx.Settings.OrderBy(s => s.Key).ToListAsync();
            }
        }

        public async Task UpsertAsync(IEnumerable<Setting> settings)
        {
            using (var ctx = new DailyTaskManagerDbContext())
            {
                foreach (var s in settings)
                {
                    if (s.Id == 0)
                    {
                        ctx.Settings.Add(s);
                    }
                    else
                    {
                        ctx.Entry(s).State = EntityState.Modified;
                    }
                }
                await ctx.SaveChangesAsync();
            }
        }
    }
}