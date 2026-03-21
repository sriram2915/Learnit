using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Learnit.Server.Data;
using Learnit.Server.Models;
using Learnit.Server.Services;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Learnit.Server.Controllers
{
    [ApiController]
    [Route("api/awards")]
    [Authorize]
    public class AwardsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly AwardService _awardService;

        public AwardsController(AppDbContext db, AwardService awardService)
        {
            _db = db;
            _awardService = awardService;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid user token");
            }

            return userId;
        }

        // GET /api/awards - Get all awards with user's progress
        [HttpGet]
        public async Task<ActionResult<UserAwardsSummaryDto>> GetAwards()
        {
            var userId = GetUserId();

            // Get all active awards
            var allAwards = await _db.Awards
                .Where(a => a.IsActive)
                .OrderBy(a => a.Category)
                .ThenBy(a => a.Order)
                .ThenBy(a => a.Threshold)
                .ToListAsync();

            // Get user's earned awards
            var earnedAwards = await _db.UserAwards
                .Where(ua => ua.UserId == userId)
                .Include(ua => ua.Award)
                .ToListAsync();

            var earnedAwardIds = earnedAwards.Select(ea => ea.AwardId).Distinct().ToHashSet();
            // Handle potential duplicates by taking the first one (shouldn't happen due to unique constraint, but safe guard)
            var earnedAwardsDict = earnedAwards
                .GroupBy(ea => ea.AwardId)
                .ToDictionary(g => g.Key, g => g.First());

            // Get user stats for progress calculation
            var stats = await GetUserStats(userId);

            var awardDtos = new List<AwardDto>();

            foreach (var award in allAwards)
            {
                var isEarned = earnedAwardIds.Contains(award.Id);
                var earnedAward = isEarned ? earnedAwardsDict[award.Id] : null;

                double? progressPercentage = null;
                if (!isEarned && award.Threshold.HasValue)
                {
                    double currentValue = 0;
                    switch (award.Category.ToLower())
                    {
                        case "hours":
                            currentValue = (double)stats.TotalHours;
                            break;
                        case "courses":
                            currentValue = stats.CompletedCourses;
                            break;
                        case "streak":
                        case "consistency":
                            currentValue = stats.CurrentStreak;
                            break;
                        case "longeststreak":
                            currentValue = stats.LongestStreak;
                            break;
                    }
                    progressPercentage = Math.Min(100, (currentValue / award.Threshold.Value) * 100);
                }

                awardDtos.Add(new AwardDto
                {
                    Id = award.Id,
                    Code = award.Code,
                    Name = award.Name,
                    Description = award.Description,
                    Icon = award.Icon,
                    Category = award.Category,
                    Threshold = award.Threshold,
                    Color = award.Color,
                    IsEarned = isEarned,
                    EarnedAt = earnedAward?.EarnedAt,
                    ProgressValue = earnedAward?.ProgressValue,
                    ProgressPercentage = progressPercentage
                });
            }

            // Calculate category counts
            var categoryCounts = awardDtos
                .GroupBy(a => a.Category)
                .ToDictionary(g => g.Key, g => g.Count(a => a.IsEarned));

            var summary = new UserAwardsSummaryDto
            {
                TotalAwards = allAwards.Count,
                EarnedAwards = earnedAwardIds.Count,
                Awards = awardDtos,
                CategoryCounts = categoryCounts
            };

            return Ok(summary);
        }

        // POST /api/awards/check - Check and grant new awards
        [HttpPost("check")]
        public async Task<ActionResult<List<AwardDto>>> CheckAwards()
        {
            var userId = GetUserId();

            var newlyEarned = await _awardService.CheckAndGrantAwards(userId);

            if (newlyEarned.Any())
            {
                var awardDtos = newlyEarned.Select(a => new AwardDto
                {
                    Id = a.Id,
                    Code = a.Code,
                    Name = a.Name,
                    Description = a.Description,
                    Icon = a.Icon,
                    Category = a.Category,
                    Color = a.Color,
                    IsEarned = true,
                    EarnedAt = DateTime.UtcNow
                }).ToList();

                return Ok(new { message = $"Congratulations! You earned {newlyEarned.Count} new award(s)!", awards = awardDtos });
            }

            return Ok(new { message = "No new awards earned", awards = new List<AwardDto>() });
        }

        // GET /api/awards/progress - Get progress toward all awards
        [HttpGet("progress")]
        public async Task<ActionResult<List<AwardProgressDto>>> GetAwardProgress()
        {
            var userId = GetUserId();
            var stats = await GetUserStats(userId);

            var allAwards = await _db.Awards
                .Where(a => a.IsActive && a.Threshold.HasValue)
                .ToListAsync();

            var earnedAwardIds = await _db.UserAwards
                .Where(ua => ua.UserId == userId)
                .Select(ua => ua.AwardId)
                .ToListAsync();

            var progressList = new List<AwardProgressDto>();

            foreach (var award in allAwards)
            {
                int currentValue = 0;
                switch (award.Category.ToLower())
                {
                    case "hours":
                        currentValue = (int)stats.TotalHours;
                        break;
                    case "courses":
                        currentValue = stats.CompletedCourses;
                        break;
                    case "streak":
                    case "consistency":
                        currentValue = stats.CurrentStreak;
                        break;
                    case "longeststreak":
                        currentValue = stats.LongestStreak;
                        break;
                }

                var isEarned = earnedAwardIds.Contains(award.Id);
                var progress = award.Threshold.Value > 0
                    ? Math.Min(100, (double)currentValue / award.Threshold.Value * 100)
                    : 0;

                progressList.Add(new AwardProgressDto
                {
                    AwardCode = award.Code,
                    CurrentValue = currentValue,
                    TargetValue = award.Threshold,
                    ProgressPercentage = progress,
                    IsEarned = isEarned
                });
            }

            return Ok(progressList);
        }

        private async Task<UserStats> GetUserStats(int userId)
        {
            var totalHours = await _db.StudySessions
                .Join(_db.Courses.Where(c => c.UserId == userId), s => s.CourseId, c => c.Id, (s, _) => s)
                .Where(s => s.IsCompleted)
                .SumAsync(s => (decimal?)s.DurationHours) ?? 0;

            var completedCourses = await _db.Courses
                .Where(c => c.UserId == userId)
                .Include(c => c.Modules)
                .ToListAsync();

            var completedCount = completedCourses.Count(c =>
                c.Modules.Any() && c.Modules.All(m => m.IsCompleted));

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
            else if (completedDates.Count == 0)
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

