using DailyTaskManager.Core.Models;
using System;
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
                return await ctx.Settings
                    .OrderBy(s => s.Key)
                    .ToListAsync();
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
                        // Check if setting already exists
                        var existing = await ctx.Settings
                            .FirstOrDefaultAsync(setting => setting.Key == s.Key);
                        
                        if (existing != null)
                        {
                            // Update existing setting
                            existing.Value = s.Value;
                            ctx.Entry(existing).State = EntityState.Modified;
                        }
                        else
                        {
                            // Add new setting
                            ctx.Settings.Add(s);
                        }
                    }
                    else
                    {
                        // Update existing setting by ID
                        var existing = await ctx.Settings
                            .FirstOrDefaultAsync(setting => setting.Id == s.Id);
                        
                        if (existing != null)
                        {
                            existing.Value = s.Value;
                            ctx.Entry(existing).State = EntityState.Modified;
                        }
                    }
                }
                await ctx.SaveChangesAsync();
            }
        }
    }
}