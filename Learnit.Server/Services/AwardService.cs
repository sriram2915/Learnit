using Microsoft.EntityFrameworkCore;
using Learnit.Server.Data;
using Learnit.Server.Models;
using System.Linq;

namespace Learnit.Server.Services
{
    public class AwardService
    {
        private readonly AppDbContext _db;

        public AwardService(AppDbContext db)
        {
            _db = db;
        }

        // Check and grant awards based on user progress
        public async Task<List<Award>> CheckAndGrantAwards(int userId)
        {
            var newlyEarnedAwards = new List<Award>();

            // Get user's current stats
            var stats = await GetUserStats(userId);
            var earnedAwardCodes = await _db.UserAwards
                .Where(ua => ua.UserId == userId)
                .Select(ua => ua.Award.Code)
                .ToListAsync();

            // Get all active awards
            var allAwards = await _db.Awards
                .Where(a => a.IsActive)
                .ToListAsync();

            foreach (var award in allAwards)
            {
                // Skip if already earned
                if (earnedAwardCodes.Contains(award.Code))
                    continue;

                bool shouldGrant = false;
                int? progressValue = null;

                switch (award.Category.ToLower())
                {
                    case "hours":
                        if (award.Threshold.HasValue && stats.TotalHours >= award.Threshold.Value)
                        {
                            shouldGrant = true;
                            progressValue = (int)stats.TotalHours;
                        }
                        break;

                    case "courses":
                        if (award.Threshold.HasValue && stats.CompletedCourses >= award.Threshold.Value)
                        {
                            shouldGrant = true;
                            progressValue = stats.CompletedCourses;
                        }
                        break;

                    case "streak":
                    case "consistency":
                        if (award.Threshold.HasValue && stats.CurrentStreak >= award.Threshold.Value)
                        {
                            shouldGrant = true;
                            progressValue = stats.CurrentStreak;
                        }
                        break;

                    case "longeststreak":
                        if (award.Threshold.HasValue && stats.LongestStreak >= award.Threshold.Value)
                        {
                            shouldGrant = true;
                            progressValue = stats.LongestStreak;
                        }
                        break;
                }

                if (shouldGrant)
                {
                    // Grant the award
                    var userAward = new UserAward
                    {
                        UserId = userId,
                        AwardId = award.Id,
                        EarnedAt = DateTime.UtcNow,
                        ProgressValue = progressValue
                    };

                    _db.UserAwards.Add(userAward);
                    newlyEarnedAwards.Add(award);
                }
            }

            if (newlyEarnedAwards.Any())
            {
                await _db.SaveChangesAsync();
            }

            return newlyEarnedAwards;
        }

        // Get user statistics for award checking
        private async Task<UserStats> GetUserStats(int userId)
        {
            // Total hours completed
            var totalHours = await _db.StudySessions
                .Join(_db.Courses.Where(c => c.UserId == userId), s => s.CourseId, c => c.Id, (s, _) => s)
                .Where(s => s.IsCompleted)
                .SumAsync(s => (decimal?)s.DurationHours) ?? 0;

            // Completed courses (all modules completed)
            var completedCourses = await _db.Courses
                .Where(c => c.UserId == userId)
                .Include(c => c.Modules)
                .ToListAsync();

            var completedCount = completedCourses.Count(c =>
                c.Modules.Any() && c.Modules.All(m => m.IsCompleted));

            // Current streak
            var today = DateTime.UtcNow.Date;
            var completedDates = await _db.StudySessions
                .Join(_db.Courses.Where(c => c.UserId == userId), s => s.CourseId, c => c.Id, (s, _) => s)
                .Where(s => s.IsCompleted)
                .Select(s => s.StartTime.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToListAsync();

            var currentStreak = 0;
            if (completedDates.Any())
            {
                var dateSet = completedDates.ToHashSet();
                var checkDate = dateSet.Contains(today) ? today : today.AddDays(-1);
                while (dateSet.Contains(checkDate))
                {
                    currentStreak++;
                    checkDate = checkDate.AddDays(-1);
                }
            }

            // Longest streak
            var longestStreak = 1;
            if (completedDates.Count > 1)
            {
                var sortedDates = completedDates.OrderBy(d => d).ToList();
                var current = 1;
                for (int i = 1; i < sortedDates.Count; i++)
                {
                    var daysDiff = (sortedDates[i] - sortedDates[i - 1]).TotalDays;
                    if (daysDiff == 1)
                    {
                        current++;
                        longestStreak = Math.Max(longestStreak, current);
                    }
                    else
                    {
                        current = 1;
                    }
                }
            }
            else if (completedDates.Count == 1)
            {
                longestStreak = 1;
            }
            else
            {
                longestStreak = 0;
            }

            return new UserStats
            {
                TotalHours = totalHours,
                CompletedCourses = completedCount,
                CurrentStreak = currentStreak,
                LongestStreak = longestStreak
            };
        }

        private class UserStats
        {
            public decimal TotalHours { get; set; }
            public int CompletedCourses { get; set; }
            public int CurrentStreak { get; set; }
            public int LongestStreak { get; set; }
        }
    }
}

