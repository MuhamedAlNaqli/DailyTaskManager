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
        private int GetCurrentUserId()
        {
            var authService = ServiceContainer.Instance.Resolve<IAuthenticationService>();
            if (!authService.IsAuthenticated || authService.CurrentUser == null)
                throw new InvalidOperationException("User must be authenticated to access settings");
            
            return authService.CurrentUser.Id;
        }

        public async Task<List<Setting>> GetAllAsync()
        {
            var userId = GetCurrentUserId();
            
            using (var ctx = new DailyTaskManagerDbContext())
            {
                return await ctx.Settings
                    .Where(s => s.UserId == userId)
                    .OrderBy(s => s.Key)
                    .ToListAsync();
            }
        }

        public async Task UpsertAsync(IEnumerable<Setting> settings)
        {
            var userId = GetCurrentUserId();
            
            using (var ctx = new DailyTaskManagerDbContext())
            {
                foreach (var s in settings)
                {
                    s.UserId = userId;
                    
                    if (s.Id == 0)
                    {
                        // Check if setting already exists for this user
                        var existing = await ctx.Settings
                            .FirstOrDefaultAsync(setting => setting.Key == s.Key && setting.UserId == userId);
                        
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
                        // Update setting if it belongs to current user
                        var existing = await ctx.Settings
                            .FirstOrDefaultAsync(setting => setting.Id == s.Id && setting.UserId == userId);
                        
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