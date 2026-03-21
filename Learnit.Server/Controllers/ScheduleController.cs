using Learnit.Server.Data;
using Learnit.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace Learnit.Server.Controllers
{
    [ApiController]
    [Route("api/schedule")]
    [Authorize]
    public class ScheduleController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ScheduleController(AppDbContext db)
        {
            _db = db;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid user token");
            }

            return userId;
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        private static DateTime? EnsureUtc(DateTime? value)
        {
            if (!value.HasValue) return null;
            return EnsureUtc(value.Value);
        }

        // Helper method to check for event conflicts
        private async Task<bool> HasEventConflict(int userId, DateTime startUtc, DateTime? endUtc, int? excludeEventId = null)
        {
            var eventEnd = endUtc ?? startUtc.AddHours(1);
            var conflicts = await _db.ScheduleEvents
                .Where(e => e.UserId == userId)
                .Where(e => excludeEventId == null || e.Id != excludeEventId.Value)
                .Where(e => e.StartUtc < eventEnd && (e.EndUtc ?? e.StartUtc.AddHours(1)) > startUtc)
                .AnyAsync();
            return conflicts;
        }

        // Helper method to validate event data
        private async Task<IActionResult?> ValidateEventDataAsync(CreateScheduleEventDto dto, int userId, int? existingEventId = null)
        {
            // Validate title
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { message = "Event title is required" });

            // Validate dates
            var startUtc = EnsureUtc(dto.StartUtc);
            DateTime? endUtc = dto.EndUtc.HasValue ? EnsureUtc(dto.EndUtc.Value) : null;

            // If end time is provided, it must be after start time
            if (endUtc.HasValue && endUtc.Value <= startUtc)
                return BadRequest(new { message = "End time must be after start time" });

            // Validate minimum duration (at least 15 minutes)
            if (endUtc.HasValue && !dto.AllDay)
            {
                var durationMinutes = (endUtc.Value - startUtc).TotalMinutes;
                if (durationMinutes < 15)
                    return BadRequest(new { message = "Event duration must be at least 15 minutes" });
            }

            // For all-day events, validate dates are reasonable
            if (dto.AllDay && endUtc.HasValue)
            {
                var duration = (endUtc.Value.Date - startUtc.Date).TotalDays;
                if (duration > 365)
                    return BadRequest(new { message = "Event duration cannot exceed 365 days" });
            }

            // Validate CourseModuleId ownership if provided
            if (dto.CourseModuleId.HasValue)
            {
                var moduleExists = await _db.CourseModules
                    .Include(cm => cm.Course)
                    .AnyAsync(cm => cm.Id == dto.CourseModuleId.Value && cm.Course!.UserId == userId);
                
                if (!moduleExists)
                    return BadRequest(new { message = "Invalid course module. The module does not belong to your courses." });
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> GetEvents(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var userId = GetUserId();

            var query = _db.ScheduleEvents
                .Where(e => e.UserId == userId)
                .Include(e => e.CourseModule)
                    .ThenInclude(cm => cm!.Course)
                .AsQueryable();

            if (from.HasValue)
            {
                var fromUtc = EnsureUtc(from.Value);
                query = query.Where(e => e.EndUtc == null ? e.StartUtc >= fromUtc : e.EndUtc >= fromUtc);
            }

            if (to.HasValue)
            {
                var toUtc = EnsureUtc(to.Value);
                query = query.Where(e => e.StartUtc <= toUtc);
            }

            var events = await query
                .OrderBy(e => e.StartUtc)
                .ToListAsync();

            var result = events.Select(e => new ScheduleEventDto
            {
                Id = e.Id,
                Title = e.Title,
                StartUtc = e.StartUtc,
                EndUtc = e.EndUtc,
                AllDay = e.AllDay,
                CourseModuleId = e.CourseModuleId,
                CourseModule = e.CourseModule == null
                    ? null
                    : new CourseModuleInfo
                    {
                        Id = e.CourseModule.Id,
                        Title = e.CourseModule.Title,
                        CourseId = e.CourseModule.CourseId,
                        CourseTitle = e.CourseModule.Course?.Title ?? string.Empty,
                        IsCompleted = e.CourseModule.IsCompleted
                    }
            }).ToList();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent(CreateScheduleEventDto dto)
        {
            var userId = GetUserId();

            // Validate event data
            var validationError = await ValidateEventDataAsync(dto, userId);
            if (validationError != null)
                return validationError;

            var startUtc = EnsureUtc(dto.StartUtc);
            DateTime? endUtc = dto.EndUtc.HasValue ? (DateTime?)EnsureUtc(dto.EndUtc.Value) : null;
            var eventEnd = endUtc ?? startUtc.AddHours(1);

            // Check for conflicts (unless all-day event)
            if (!dto.AllDay)
            {
                var hasConflict = await HasEventConflict(userId, startUtc, endUtc);
                if (hasConflict)
                    return Conflict(new { message = "This event conflicts with an existing scheduled event. Please choose a different time." });
            }

            var entity = new ScheduleEvent
            {
                UserId = userId,
                Title = dto.Title.Trim(),
                StartUtc = startUtc,
                EndUtc = endUtc,
                AllDay = dto.AllDay,
                CourseModuleId = dto.CourseModuleId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.ScheduleEvents.Add(entity);
            await _db.SaveChangesAsync();

            var result = new ScheduleEventDto
            {
                Id = entity.Id,
                Title = entity.Title,
                StartUtc = entity.StartUtc,
                EndUtc = entity.EndUtc,
                AllDay = entity.AllDay,
                CourseModuleId = entity.CourseModuleId
            };

            return CreatedAtAction(nameof(GetEvents), new { id = entity.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, CreateScheduleEventDto dto)
        {
            var userId = GetUserId();

            var entity = await _db.ScheduleEvents
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (entity == null)
                return NotFound(new { message = "Event not found or you don't have permission to edit it" });

            // Validate event data
            var validationError = await ValidateEventDataAsync(dto, userId, existingEventId: id);
            if (validationError != null)
                return validationError;

            var startUtc = EnsureUtc(dto.StartUtc);
            DateTime? endUtc = dto.EndUtc.HasValue ? (DateTime?)EnsureUtc(dto.EndUtc.Value) : null;
            var eventEnd = endUtc ?? startUtc.AddHours(1);

            // Check for conflicts with other events (unless all-day event)
            if (!dto.AllDay)
            {
                var hasConflict = await HasEventConflict(userId, startUtc, endUtc, excludeEventId: id);
                if (hasConflict)
                    return Conflict(new { message = "This event conflicts with an existing scheduled event. Please choose a different time." });
            }

            entity.Title = dto.Title.Trim();
            entity.StartUtc = startUtc;
            entity.EndUtc = endUtc;
            entity.AllDay = dto.AllDay;
            entity.CourseModuleId = dto.CourseModuleId;
            entity.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                // Entity was deleted or modified, return not found
                return NotFound("Event not found or was deleted");
            }

            return Ok(new { message = "Event updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userId = GetUserId();

            var entity = await _db.ScheduleEvents
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (entity == null)
                return NotFound();

            _db.ScheduleEvents.Remove(entity);
            
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                // Entity was already deleted, return success
                return Ok(new { message = "Event deleted" });
            }

            return Ok(new { message = "Event deleted" });
        }

        [HttpDelete("reset")]
        public async Task<IActionResult> ResetAll()
        {
            var userId = GetUserId();

            var events = await _db.ScheduleEvents
                .Where(e => e.UserId == userId)
                .ToListAsync();

            if (events.Count == 0)
                return Ok(new { message = "No events to remove", removed = 0 });

            _db.ScheduleEvents.RemoveRange(events);
            await _db.SaveChangesAsync();

            return Ok(new { message = "All schedule events cleared", removed = events.Count });
        }

        [HttpGet("available-modules")]
        public async Task<IActionResult> GetAvailableModules()
        {
            var userId = GetUserId();

            // Get course modules that haven't been scheduled yet
            var scheduledModuleIds = await _db.ScheduleEvents
                .Where(e => e.UserId == userId && e.CourseModuleId.HasValue)
                .Select(e => e.CourseModuleId!.Value)
                .ToListAsync();

            var availableModules = await _db.CourseModules
                .Include(cm => cm.Course)
                .Where(cm => cm.Course!.UserId == userId && !scheduledModuleIds.Contains(cm.Id))
                .Select(cm => new
                {
                    id = cm.Id,
                    title = cm.Title,
                    courseTitle = cm.Course!.Title,
                    estimatedHours = cm.EstimatedHours,
                    courseId = cm.CourseId
                })
                .ToListAsync();

            return Ok(availableModules);
        }

        [HttpPost("auto-schedule")]
        public async Task<IActionResult> AutoScheduleModules([FromBody] AutoScheduleRequest request)
        {
            var userId = GetUserId();
            var includeWeekends = request.IncludeWeekends ?? false;
            // Validate preferred hours are reasonable (0-23, and end > start)
            var preferredStartHour = request.PreferredStartHour ?? 9;
            var preferredEndHour = request.PreferredEndHour ?? 18;
            
            // Clamp to valid hour range
            preferredStartHour = Math.Clamp(preferredStartHour, 0, 23);
            preferredEndHour = Math.Clamp(preferredEndHour, 0, 23);
            
            // Ensure end hour is after start hour
            if (preferredEndHour <= preferredStartHour)
            {
                preferredEndHour = Math.Min(23, preferredStartHour + 8); // Default to 8-hour window
            }

            // Treat all window math in the user's local time based on the provided offset
            // Validate timezone offset is reasonable (-12 to +14 hours, or -720 to +840 minutes)
            var offsetMinutes = request.TimezoneOffsetMinutes ?? 0;
            if (offsetMinutes < -720 || offsetMinutes > 840)
            {
                return BadRequest(new { message = "Invalid timezone offset. Must be between -12 and +14 hours." });
            }
            var offsetSpan = TimeSpan.FromMinutes(offsetMinutes);

            DateTime ToLocal(DateTime utc) => utc - offsetSpan;
            DateTime ToUtc(DateTime local) => local + offsetSpan;

            var maxSessionMinutes = Math.Clamp(request.MaxSessionMinutes ?? 90, 30, 180);
            var maxBlockHours = maxSessionMinutes / 60d;
            var bufferMinutes = Math.Clamp(request.BufferMinutes ?? 15, 5, 45);

            var windowHours = Math.Max(2, preferredEndHour - preferredStartHour);
            var maxDailyHours = Math.Clamp(request.MaxDailyHours ?? Math.Min(windowHours, 6), 2, windowHours);
            var weeklyLimitHours = request.WeeklyLimitHours ?? 20;

            var courseOrder = request.CourseOrderIds?
                .Where(id => id > 0)
                .Distinct()
                .ToList() ?? new List<int>();

            // If the client explicitly sent an empty course selection AND did not provide explicit module IDs,
            // schedule nothing. This prevents accidentally scheduling unselected courses.
            if (request.CourseOrderIds != null && request.CourseOrderIds.Count == 0 &&
                (request.ModuleIds == null || request.ModuleIds.Count == 0))
            {
                return Ok(new { scheduledEvents = 0, message = "No courses selected" });
            }

            var hasCourseFilter = courseOrder.Any();
            var courseOrderRank = courseOrder
                .Select((id, idx) => new { id, idx })
                .ToDictionary(x => x.id, x => x.idx);

            // Get available modules ordered by urgency and priority
            var scheduledModuleIds = await _db.ScheduleEvents
                .Where(e => e.UserId == userId && e.CourseModuleId.HasValue)
                .Select(e => e.CourseModuleId!.Value)
                .ToListAsync();

            var modulesQuery = _db.CourseModules
                .Include(cm => cm.Course)
                .Where(cm => cm.Course!.UserId == userId && !scheduledModuleIds.Contains(cm.Id) && cm.Course!.IsActive && !cm.IsCompleted);

            if (hasCourseFilter)
            {
                modulesQuery = modulesQuery.Where(cm => courseOrder.Contains(cm.CourseId));
            }
            
            // Filter by specific module IDs if provided (for rescheduling specific missed modules)
            var moduleIdsFilter = request.ModuleIds?
                .Where(id => id > 0)
                .Distinct()
                .ToList() ?? new List<int>();
            var hasModuleFilter = moduleIdsFilter.Any();
            if (hasModuleFilter)
            {
                modulesQuery = modulesQuery.Where(cm => moduleIdsFilter.Contains(cm.Id));
            }

            var modulesRaw = await modulesQuery.ToListAsync();

            int GetRank(Learnit.Server.Models.CourseModule cm)
            {
                return hasCourseFilter && courseOrderRank.TryGetValue(cm.CourseId, out var rank)
                    ? rank
                    : int.MaxValue;
            }

            var modulesToSchedule = modulesRaw
                .OrderBy(cm => GetRank(cm))
                .ThenBy(cm => cm.Course!.TargetCompletionDate ?? DateTime.MaxValue)
                .ThenBy(cm => cm.Course!.Priority == "High" ? 1 : cm.Course!.Priority == "Medium" ? 2 : 3)
                .ThenBy(cm => cm.Order)
                .ToList();

            var occupiedIntervals = (await _db.ScheduleEvents
                .Where(e => e.UserId == userId)
                .Select(e => new
                {
                    Start = e.StartUtc,
                    End = e.EndUtc ?? e.StartUtc.AddHours(1)
                })
                .ToListAsync())
                .Select(e => (Start: ToLocal(EnsureUtc(e.Start)), End: ToLocal(EnsureUtc(e.End))))
                .OrderBy(e => e.Start)
                .ToList();

            var events = new List<ScheduleEvent>();
            var anchor = EnsureUtc(request.StartDateTime ?? DateTime.UtcNow);
            var currentTimeLocal = ToLocal(anchor).Date.AddHours(preferredStartHour);

            const int lunchStartHour = 12;
            const int lunchEndHour = 13;

            DateTime GetWeekStart(DateTime dt)
            {
                var normalized = dt.Date;
                var offset = normalized.DayOfWeek == DayOfWeek.Sunday ? 6 : ((int)normalized.DayOfWeek - 1);
                return normalized.AddDays(-offset);
            }

            DateTime AlignToWorkWindow(DateTime dt)
            {
                var aligned = dt;

                // Loop because we may roll into a weekend when adjusting to next day.
                while (true)
                {
                    if (!includeWeekends && (aligned.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday))
                    {
                        aligned = aligned.AddDays(1).Date.AddHours(preferredStartHour);
                        continue;
                    }

                    if (aligned.Hour < preferredStartHour)
                    {
                        aligned = aligned.Date.AddHours(preferredStartHour);
                    }
                    else if (aligned.Hour >= preferredEndHour)
                    {
                        aligned = aligned.AddDays(1).Date.AddHours(preferredStartHour);
                        continue;
                    }

                    // Avoid lunch window when it sits inside the work window
                    if (preferredStartHour < lunchStartHour && preferredEndHour > lunchEndHour)
                    {
                        if (aligned.Hour == lunchStartHour || (aligned.Hour == lunchStartHour - 1 && aligned.Minute > 0))
                        {
                            aligned = aligned.Date.AddHours(lunchEndHour);
                        }
                        else if (aligned.Hour >= lunchStartHour && aligned.Hour < lunchEndHour)
                        {
                            aligned = aligned.Date.AddHours(lunchEndHour);
                        }
                    }

                    // After lunch adjustment, re-check weekend in case we crossed a date boundary (rare but safe).
                    if (!includeWeekends && (aligned.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday))
                    {
                        aligned = aligned.AddDays(1).Date.AddHours(preferredStartHour);
                        continue;
                    }

                    return aligned;
                }
            }

            currentTimeLocal = AlignToWorkWindow(currentTimeLocal);
            
            var currentDay = currentTimeLocal.Date;
            double currentDayHours = 0;
            var weeklyHours = new Dictionary<DateTime, double>();

            // Optimized overlap detection using sorted intervals (O(log n) with binary search potential)
            bool HasOverlap(DateTime start, DateTime end, out DateTime nextStart)
            {
                // Since occupiedIntervals is already sorted, we can optimize
                // For now, using early exit optimization
                foreach (var interval in occupiedIntervals)
                {
                    // Early exit if we've passed all possible overlaps
                    if (interval.Start >= end)
                        break;

                    if (start < interval.End && end > interval.Start)
                    {
                        nextStart = interval.End.AddMinutes(bufferMinutes);
                        return true;
                    }
                }

                nextStart = DateTime.MinValue;
                return false;
            }
            
            // CRITICAL: Ensure we never schedule in the past, and handle overlaps
            // If the startDateTime is in the past, move it to the next available slot
            var nowLocal = ToLocal(DateTime.UtcNow);
            if (currentTimeLocal <= nowLocal)
            {
                // Move to the next available slot (could be later today or tomorrow)
                currentTimeLocal = AlignToWorkWindow(nowLocal.AddMinutes(bufferMinutes));
                
                // If we're still in the past (shouldn't happen, but safety check), move to tomorrow
                if (currentTimeLocal <= nowLocal)
                {
                    currentTimeLocal = AlignToWorkWindow(nowLocal.Date.AddDays(1).AddHours(preferredStartHour));
                }
                
                // Check for overlaps with existing events and move forward if needed
                // We use a dummy end time (1 hour) to check overlaps, the actual duration will be calculated in the loop
                var dummyEndTime = currentTimeLocal.AddHours(1);
                if (HasOverlap(currentTimeLocal, dummyEndTime, out var nextAvailable))
                {
                    currentTimeLocal = AlignToWorkWindow(nextAvailable);
                }
            }

            foreach (var module in modulesToSchedule)
            {
                var originalEstimatedHours = Math.Max(0.25, module.EstimatedHours);
                double remainingHours = originalEstimatedHours;

                // Slightly smaller blocks for advanced material
                var difficultyCap = module.Course?.Difficulty == "Advanced" ? Math.Min(maxBlockHours, 1.0) : maxBlockHours;

                var baseTitle = $"{module.Course!.Title} - {module.Title}";
                var willSplit = originalEstimatedHours > difficultyCap + 0.001;
                var sessionIndex = 1;

                while (remainingHours > 0)
                {
                    if (currentTimeLocal.Date != currentDay)
                    {
                        currentDay = currentTimeLocal.Date;
                        currentDayHours = 0;
                        currentTimeLocal = AlignToWorkWindow(currentTimeLocal);
                    }

                    var weekKey = GetWeekStart(currentTimeLocal);
                    weeklyHours.TryGetValue(weekKey, out var usedThisWeek);

                    if (weeklyLimitHours > 0 && usedThisWeek >= weeklyLimitHours)
                    {
                        currentTimeLocal = AlignToWorkWindow(weekKey.AddDays(7).AddHours(preferredStartHour));
                        continue;
                    }

                    if (currentDayHours >= maxDailyHours)
                    {
                        currentTimeLocal = AlignToWorkWindow(currentTimeLocal.AddDays(1));
                        continue;
                    }

                    currentTimeLocal = AlignToWorkWindow(currentTimeLocal);

                    DateTime dayBoundary;
                    if (preferredStartHour < lunchStartHour && preferredEndHour > lunchEndHour && currentTimeLocal.Hour < lunchStartHour)
                    {
                        dayBoundary = currentTimeLocal.Date.AddHours(lunchStartHour);
                    }
                    else
                    {
                        dayBoundary = currentTimeLocal.Date.AddHours(preferredEndHour);
                    }

                    var availableHoursToday = Math.Max(0, (dayBoundary - currentTimeLocal).TotalHours);
                    var remainingDailyHours = maxDailyHours - currentDayHours;
                    var remainingWeeklyHours = weeklyLimitHours > 0 ? weeklyLimitHours - usedThisWeek : double.MaxValue;

                    var blockHours = new[]
                    {
                        difficultyCap,
                        remainingHours,
                        availableHoursToday,
                        remainingDailyHours,
                        remainingWeeklyHours
                    }.Min();

                    if (blockHours <= 0)
                    {
                        currentTimeLocal = AlignToWorkWindow(currentTimeLocal.AddDays(1));
                        continue;
                    }

                    var endTimeLocal = currentTimeLocal.AddHours(blockHours);

                    // Ensure event doesn't exceed preferred end hour
                    var maxEndTime = currentTimeLocal.Date.AddHours(preferredEndHour);
                    if (endTimeLocal > maxEndTime)
                    {
                        endTimeLocal = maxEndTime;
                        blockHours = Math.Max(0.25, (endTimeLocal - currentTimeLocal).TotalHours); // Minimum 15 minutes
                        
                        // If adjusted block is too small, skip to next day
                        if (blockHours < 0.25)
                        {
                            currentTimeLocal = AlignToWorkWindow(currentTimeLocal.AddDays(1));
                            continue;
                        }
                    }

                    if (HasOverlap(currentTimeLocal, endTimeLocal, out var nextStart))
                    {
                        currentTimeLocal = AlignToWorkWindow(nextStart);
                        continue;
                    }

                    // Do not cross the lunch gap
                    if (preferredStartHour < lunchStartHour && preferredEndHour > lunchEndHour && currentTimeLocal < currentTimeLocal.Date.AddHours(lunchStartHour) && endTimeLocal > currentTimeLocal.Date.AddHours(lunchStartHour))
                    {
                        endTimeLocal = currentTimeLocal.Date.AddHours(lunchStartHour);
                        blockHours = (endTimeLocal - currentTimeLocal).TotalHours;
                        
                        // If adjusted block is too small, skip to after lunch
                        if (blockHours < 0.25)
                        {
                            currentTimeLocal = AlignToWorkWindow(currentTimeLocal.Date.AddHours(lunchEndHour));
                            continue;
                        }
                    }

                    var scheduleEvent = new ScheduleEvent
                    {
                        UserId = userId,
                        Title = willSplit ? $"{baseTitle} (Session {sessionIndex})" : baseTitle,
                        StartUtc = ToUtc(currentTimeLocal),
                        EndUtc = ToUtc(endTimeLocal),
                        AllDay = false,
                        CourseModuleId = module.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    events.Add(scheduleEvent);
                    occupiedIntervals.Add((currentTimeLocal, endTimeLocal));

                    remainingHours -= blockHours;
                    currentDayHours += blockHours;
                    weeklyHours[weekKey] = usedThisWeek + blockHours;

                    sessionIndex++;

                    currentTimeLocal = AlignToWorkWindow(endTimeLocal.AddMinutes(bufferMinutes));
                }
            }

            if (events.Any())
            {
                _db.ScheduleEvents.AddRange(events);
                await _db.SaveChangesAsync();
            }

            return Ok(new
            {
                scheduledEvents = events.Count,
                weeklyLimitHours,
                maxDailyHours,
                maxSessionMinutes,
                events = events.Select(e => new ScheduleEventDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    StartUtc = e.StartUtc,
                    EndUtc = e.EndUtc,
                    AllDay = e.AllDay,
                    CourseModuleId = e.CourseModuleId,
                    CourseModule = e.CourseModuleId.HasValue
                        ? new CourseModuleInfo
                        {
                            Id = e.CourseModuleId.Value,
                            Title = e.Title,
                            CourseId = modulesToSchedule.First(m => m.Id == e.CourseModuleId.Value).CourseId,
                            CourseTitle = modulesToSchedule.First(m => m.Id == e.CourseModuleId.Value).Course!.Title,
                            IsCompleted = modulesToSchedule.First(m => m.Id == e.CourseModuleId.Value).IsCompleted
                        }
                        : null
                })
            });
        }

        [HttpPost("{eventId}/link-module/{moduleId}")]
        public async Task<IActionResult> LinkEventToModule(int eventId, int moduleId)
        {
            var userId = GetUserId();

            var scheduleEvent = await _db.ScheduleEvents
                .FirstOrDefaultAsync(e => e.Id == eventId && e.UserId == userId);

            if (scheduleEvent == null)
                return NotFound("Event not found");

            // Verify the module belongs to user's course
            var courseModule = await _db.CourseModules
                .Include(cm => cm.Course)
                .FirstOrDefaultAsync(cm => cm.Id == moduleId && cm.Course!.UserId == userId);

            if (courseModule == null)
                return BadRequest("Invalid module");

            scheduleEvent.CourseModuleId = moduleId;
            scheduleEvent.Title = $"{courseModule.Course!.Title} - {courseModule.Title}";
            scheduleEvent.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Event linked to module" });
        }

        [HttpDelete("{eventId}/unlink-module")]
        public async Task<IActionResult> UnlinkEventFromModule(int eventId)
        {
            var userId = GetUserId();

            var scheduleEvent = await _db.ScheduleEvents
                .FirstOrDefaultAsync(e => e.Id == eventId && e.UserId == userId);

            if (scheduleEvent == null)
                return NotFound("Event not found");

            scheduleEvent.CourseModuleId = null;
            // Reset title to generic
            scheduleEvent.Title = "Study Session";
            scheduleEvent.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Event unlinked from module" });
        }

        [HttpPost("auto-adjust")]
        public async Task<IActionResult> AutoAdjustSchedule([FromBody] AutoAdjustRequest request)
        {
            var userId = GetUserId();
            var courseId = request.CourseId;
            var actualWatchHours = request.ActualWatchHours ?? 0;
            var timezoneOffsetMinutes = request.TimezoneOffsetMinutes ?? 0;
            var offsetSpan = TimeSpan.FromMinutes(timezoneOffsetMinutes);

            DateTime ToLocal(DateTime utc) => utc - offsetSpan;
            DateTime ToUtc(DateTime local) => local + offsetSpan;

            // Get course and its modules
            var course = await _db.Courses
                .Include(c => c.Modules)
                .FirstOrDefaultAsync(c => c.Id == courseId && c.UserId == userId);

            if (course == null)
                return NotFound("Course not found");

            // Get scheduled hours for this course
            var scheduledEvents = await _db.ScheduleEvents
                .Include(e => e.CourseModule)
                .Where(e => e.UserId == userId && 
                           e.CourseModule != null && 
                           e.CourseModule.CourseId == courseId &&
                           e.StartUtc >= DateTime.UtcNow)
                .OrderBy(e => e.StartUtc)
                .ToListAsync();

            var scheduledHours = scheduledEvents
                .Where(e => e.EndUtc.HasValue)
                .Sum(e => (e.EndUtc.Value - e.StartUtc).TotalHours);

            var remainingHours = course.HoursRemaining;
            var completedModules = course.Modules.Count(m => m.IsCompleted);
            var totalModules = course.Modules.Count;
            var progressPercent = totalModules > 0 ? (completedModules * 100.0 / totalModules) : 0;

            // Calculate adjustment needed
            var hoursDifference = actualWatchHours - scheduledHours;
            var isBehind = hoursDifference < -1; // More than 1 hour behind
            var isAhead = hoursDifference > 1 && progressPercent < 50; // Ahead but not too far along

            if (!isBehind && !isAhead)
            {
                return Ok(new { 
                    message = "No adjustment needed", 
                    scheduledHours, 
                    actualWatchHours, 
                    remainingHours,
                    progressPercent 
                });
            }

            // Remove future events for incomplete modules
            var incompleteModuleIds = course.Modules
                .Where(m => !m.IsCompleted)
                .Select(m => m.Id)
                .ToHashSet();

            var eventsToRemove = scheduledEvents
                .Where(e => e.CourseModuleId.HasValue && incompleteModuleIds.Contains(e.CourseModuleId.Value))
                .ToList();

            if (eventsToRemove.Any())
            {
                _db.ScheduleEvents.RemoveRange(eventsToRemove);
                await _db.SaveChangesAsync();
            }

            // Re-schedule remaining modules with adjusted parameters
            var preferredStartHour = request.PreferredStartHour ?? 9;
            var preferredEndHour = request.PreferredEndHour ?? 18;
            var includeWeekends = request.IncludeWeekends ?? false;
            var maxDailyHours = request.MaxDailyHours ?? 6;
            var weeklyLimitHours = request.WeeklyLimitHours ?? 20;

            // Adjust weekly limit based on progress
            if (isBehind)
            {
                // Increase weekly hours if behind
                weeklyLimitHours = Math.Min(30, weeklyLimitHours + 5);
            }
            else if (isAhead)
            {
                // Slightly reduce if ahead (but not too much)
                weeklyLimitHours = Math.Max(15, weeklyLimitHours - 2);
            }

            // Get incomplete modules that aren't already scheduled
            var scheduledModuleIds = scheduledEvents
                .Where(e => e.CourseModuleId.HasValue)
                .Select(e => e.CourseModuleId!.Value)
                .ToHashSet();

            var incompleteModules = await _db.CourseModules
                .Include(cm => cm.Course)
                .Where(cm => cm.CourseId == courseId && 
                            !cm.IsCompleted && 
                            !scheduledModuleIds.Contains(cm.Id))
                .OrderBy(cm => cm.Order)
                .ToListAsync();

            if (!incompleteModules.Any())
            {
                return Ok(new { 
                    message = "No modules to reschedule", 
                    removedEvents = eventsToRemove.Count 
                });
            }

            // Use existing auto-schedule logic
            var occupiedIntervals = (await _db.ScheduleEvents
                .Where(e => e.UserId == userId)
                .Select(e => new { Start = e.StartUtc, End = e.EndUtc ?? e.StartUtc.AddHours(1) })
                .ToListAsync())
                .Select(e => (Start: ToLocal(EnsureUtc(e.Start)), End: ToLocal(EnsureUtc(e.End))))
                .OrderBy(e => e.Start)
                .ToList();

            var newEvents = new List<ScheduleEvent>();
            var currentTimeLocal = ToLocal(DateTime.UtcNow).Date.AddHours(preferredStartHour);
            var currentDay = currentTimeLocal.Date;
            double currentDayHours = 0;
            var weeklyHours = new Dictionary<DateTime, double>();

            DateTime GetWeekStart(DateTime dt)
            {
                var normalized = dt.Date;
                var offset = normalized.DayOfWeek == DayOfWeek.Sunday ? 6 : ((int)normalized.DayOfWeek - 1);
                return normalized.AddDays(-offset);
            }

            DateTime AlignToWorkWindow(DateTime dt)
            {
                var aligned = dt;

                while (true)
                {
                    if (!includeWeekends && (aligned.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday))
                    {
                        aligned = aligned.AddDays(1).Date.AddHours(preferredStartHour);
                        continue;
                    }

                    if (aligned.Hour < preferredStartHour)
                    {
                        aligned = aligned.Date.AddHours(preferredStartHour);
                    }
                    else if (aligned.Hour >= preferredEndHour)
                    {
                        aligned = aligned.AddDays(1).Date.AddHours(preferredStartHour);
                        continue;
                    }

                    if (!includeWeekends && (aligned.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday))
                    {
                        aligned = aligned.AddDays(1).Date.AddHours(preferredStartHour);
                        continue;
                    }

                    return aligned;
                }
            }

            // Optimized overlap detection using sorted intervals
            bool HasOverlap(DateTime start, DateTime end, out DateTime nextStart)
            {
                // Since occupiedIntervals is already sorted, we can optimize
                foreach (var interval in occupiedIntervals)
                {
                    // Early exit if we've passed all possible overlaps
                    if (interval.Start >= end)
                        break;

                    if (start < interval.End && end > interval.Start)
                    {
                        nextStart = interval.End.AddMinutes(15);
                        return true;
                    }
                }
                nextStart = DateTime.MinValue;
                return false;
            }

            currentTimeLocal = AlignToWorkWindow(currentTimeLocal);
            const int maxSessionMinutes = 90;
            var maxBlockHours = maxSessionMinutes / 60d;

            foreach (var module in incompleteModules)
            {
                double moduleRemainingHours = Math.Max(0.25, module.EstimatedHours);

                while (moduleRemainingHours > 0)
                {
                    if (currentTimeLocal.Date != currentDay)
                    {
                        currentDay = currentTimeLocal.Date;
                        currentDayHours = 0;
                        currentTimeLocal = AlignToWorkWindow(currentTimeLocal);
                    }

                    var weekKey = GetWeekStart(currentTimeLocal);
                    weeklyHours.TryGetValue(weekKey, out var usedThisWeek);

                    if (weeklyLimitHours > 0 && usedThisWeek >= weeklyLimitHours)
                    {
                        currentTimeLocal = AlignToWorkWindow(weekKey.AddDays(7).AddHours(preferredStartHour));
                        continue;
                    }

                    if (currentDayHours >= maxDailyHours)
                    {
                        currentTimeLocal = AlignToWorkWindow(currentTimeLocal.AddDays(1));
                        continue;
                    }

                    currentTimeLocal = AlignToWorkWindow(currentTimeLocal);
                    var dayBoundary = currentTimeLocal.Date.AddHours(preferredEndHour);
                    var availableHoursToday = Math.Max(0, (dayBoundary - currentTimeLocal).TotalHours);
                    var remainingDailyHours = maxDailyHours - currentDayHours;
                    var remainingWeeklyHours = weeklyLimitHours > 0 ? weeklyLimitHours - usedThisWeek : double.MaxValue;

                    var blockHours = new[] { maxBlockHours, moduleRemainingHours, availableHoursToday, remainingDailyHours, remainingWeeklyHours }.Min();

                    if (blockHours <= 0)
                    {
                        currentTimeLocal = AlignToWorkWindow(currentTimeLocal.AddDays(1));
                        continue;
                    }

                    var endTimeLocal = currentTimeLocal.AddHours(blockHours);

                    // Ensure event doesn't exceed preferred end hour
                    var maxEndTime = currentTimeLocal.Date.AddHours(preferredEndHour);
                    if (endTimeLocal > maxEndTime)
                    {
                        endTimeLocal = maxEndTime;
                        blockHours = Math.Max(0.25, (endTimeLocal - currentTimeLocal).TotalHours); // Minimum 15 minutes
                        
                        // If adjusted block is too small, skip to next day
                        if (blockHours < 0.25)
                        {
                            currentTimeLocal = AlignToWorkWindow(currentTimeLocal.AddDays(1));
                            continue;
                        }
                    }

                    if (HasOverlap(currentTimeLocal, endTimeLocal, out var nextStart))
                    {
                        currentTimeLocal = AlignToWorkWindow(nextStart);
                        continue;
                    }

                    var scheduleEvent = new ScheduleEvent
                    {
                        UserId = userId,
                        Title = $"{course.Title} - {module.Title}",
                        StartUtc = ToUtc(currentTimeLocal),
                        EndUtc = ToUtc(endTimeLocal),
                        AllDay = false,
                        CourseModuleId = module.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    newEvents.Add(scheduleEvent);
                    occupiedIntervals.Add((currentTimeLocal, endTimeLocal));

                    moduleRemainingHours -= blockHours;
                    currentDayHours += blockHours;
                    weeklyHours[weekKey] = usedThisWeek + blockHours;

                    currentTimeLocal = AlignToWorkWindow(endTimeLocal.AddMinutes(15));
                }
            }

            if (newEvents.Any())
            {
                _db.ScheduleEvents.AddRange(newEvents);
                await _db.SaveChangesAsync();
            }

            return Ok(new
            {
                message = "Schedule adjusted",
                removedEvents = eventsToRemove.Count,
                addedEvents = newEvents.Count,
                scheduledHours = newEvents.Sum(e => (e.EndUtc!.Value - e.StartUtc).TotalHours),
                weeklyLimitHours
            });
        }
    }

    public class AutoAdjustRequest
    {
        public int CourseId { get; set; }
        public double? ActualWatchHours { get; set; }
        public int? PreferredStartHour { get; set; }
        public int? PreferredEndHour { get; set; }
        public bool? IncludeWeekends { get; set; }
        public int? MaxDailyHours { get; set; }
        public int? WeeklyLimitHours { get; set; }
        public int? TimezoneOffsetMinutes { get; set; }
    }

    public class AutoScheduleRequest
    {
        public DateTime? StartDateTime { get; set; }
        public int? PreferredStartHour { get; set; }
        public int? PreferredEndHour { get; set; }
        public bool? IncludeWeekends { get; set; }
        public int? MaxDailyHours { get; set; }
        public int? MaxSessionMinutes { get; set; }
        public int? BufferMinutes { get; set; }
        public int? WeeklyLimitHours { get; set; }
        public int? TimezoneOffsetMinutes { get; set; }
        public List<int>? CourseOrderIds { get; set; }
        public List<int>? ModuleIds { get; set; } // For rescheduling specific modules only
    }
    
}


