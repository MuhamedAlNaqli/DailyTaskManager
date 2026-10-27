using System;
using System.Collections.Generic;

namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// Filter applied to the task list on the database side
    /// </summary>
    public class TaskQuery
    {
        public TaskQuery(string? search = null, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            Search = search;
            DateFrom = dateFrom;
            DateTo = dateTo;
        }

        public string? Search { get; }
        public DateTime? DateFrom { get; }
        public DateTime? DateTo { get; }
    }

    /// <summary>
    /// One page of tasks plus the total number of tasks matching the query
    /// </summary>
    public class PagedResult<T>
    {
        public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
        }

        public IReadOnlyList<T> Items { get; }
        public int TotalCount { get; }
        public int Page { get; }
        public int PageSize { get; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    /// <summary>
    /// Hours summary shown on the task list header cards
    /// </summary>
    public class TaskSummary
    {
        public TaskSummary(decimal todayHours, decimal thisWeekHours, decimal dailyAverage30Days)
        {
            TodayHours = todayHours;
            ThisWeekHours = thisWeekHours;
            DailyAverage30Days = dailyAverage30Days;
        }

        public decimal TodayHours { get; }
        public decimal ThisWeekHours { get; }
        public decimal DailyAverage30Days { get; }
    }
}
