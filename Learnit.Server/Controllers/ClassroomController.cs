using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Learnit.Server.Data;
using Learnit.Server.Models;
using System.Security.Claims;
using System.Text;
using System.Linq;

namespace Learnit.Server.Controllers
{
    [ApiController]
    [Route("api/classrooms")]
    [Authorize]
    public class ClassroomController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ClassroomController(AppDbContext db)
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

        private string GenerateInviteCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var code = new StringBuilder(8);
            for (int i = 0; i < 8; i++)
            {
                code.Append(chars[random.Next(chars.Length)]);
            }
            return code.ToString();
        }

        // GET /api/classrooms - List user's classrooms
        [HttpGet]
        public async Task<ActionResult<List<ClassroomDto>>> GetClassrooms()
        {
            var userId = GetUserId();

            var classrooms = await _db.Classrooms
                .Where(c => c.Members.Any(m => m.UserId == userId))
                .Include(c => c.Members)
                .Include(c => c.SharedCourses.Where(cc => cc.IsActive))
                .Select(c => new ClassroomDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    CreatorId = c.CreatorId,
                    CreatorName = _db.Users.Where(u => u.Id == c.CreatorId).Select(u => u.FullName).FirstOrDefault() ?? "",
                    InviteCode = c.InviteCode,
                    IsPublic = c.IsPublic,
                    CreatedAt = c.CreatedAt,
                    MemberCount = c.Members.Count,
                    CourseCount = c.SharedCourses.Count(cc => cc.IsActive),
                    UserRole = c.Members.Where(m => m.UserId == userId).Select(m => m.Role).FirstOrDefault() ?? "Member",
                    IsCreator = c.CreatorId == userId
                })
                .ToListAsync();

            return Ok(classrooms);
        }

        // GET /api/classrooms/{id} - Get classroom details
        [HttpGet("{id}")]
        public async Task<ActionResult<ClassroomDto>> GetClassroom(int id)
        {
            var userId = GetUserId();

            var classroom = await _db.Classrooms
                .Where(c => c.Id == id && c.Members.Any(m => m.UserId == userId))
                .Include(c => c.Members)
                .Include(c => c.SharedCourses.Where(cc => cc.IsActive))
                .Select(c => new ClassroomDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    CreatorId = c.CreatorId,
                    CreatorName = _db.Users.Where(u => u.Id == c.CreatorId).Select(u => u.FullName).FirstOrDefault() ?? "",
                    InviteCode = c.InviteCode,
                    IsPublic = c.IsPublic,
                    CreatedAt = c.CreatedAt,
                    MemberCount = c.Members.Count,
                    CourseCount = c.SharedCourses.Count(cc => cc.IsActive),
                    UserRole = c.Members.Where(m => m.UserId == userId).Select(m => m.Role).FirstOrDefault() ?? "Member",
                    IsCreator = c.CreatorId == userId
                })
                .FirstOrDefaultAsync();

            if (classroom == null)
            {
                return NotFound("Classroom not found or you are not a member");
            }

            return Ok(classroom);
        }

        // POST /api/classrooms - Create new classroom
        [HttpPost]
        public async Task<ActionResult<ClassroomDto>> CreateClassroom([FromBody] CreateClassroomDto dto)
        {
            var userId = GetUserId();

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest("Classroom name is required");
            }

            // Generate unique invite code
            string inviteCode;
            bool isUnique = false;
            int attempts = 0;
            do
            {
                inviteCode = GenerateInviteCode();
                isUnique = !await _db.Classrooms.AnyAsync(c => c.InviteCode == inviteCode);
                attempts++;
            } while (!isUnique && attempts < 10);

            if (!isUnique)
            {
                return StatusCode(500, "Failed to generate unique invite code");
            }

            var classroom = new Classroom
            {
                Name = dto.Name,
                Description = dto.Description ?? "",
                CreatorId = userId,
                InviteCode = inviteCode,
                IsPublic = dto.IsPublic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Classrooms.Add(classroom);
            await _db.SaveChangesAsync();

            // Add creator as member with Creator role
            var creatorMember = new ClassroomMember
            {
                ClassroomId = classroom.Id,
                UserId = userId,
                Role = "Creator",
                JoinedAt = DateTime.UtcNow
            };
            _db.ClassroomMembers.Add(creatorMember);
            await _db.SaveChangesAsync();

            var creatorName = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync() ?? "";

            var response = new ClassroomDto
            {
                Id = classroom.Id,
                Name = classroom.Name,
                Description = classroom.Description,
                CreatorId = classroom.CreatorId,
                CreatorName = creatorName,
                InviteCode = classroom.InviteCode,
                IsPublic = classroom.IsPublic,
                CreatedAt = classroom.CreatedAt,
                MemberCount = 1,
                CourseCount = 0,
                UserRole = "Creator",
                IsCreator = true
            };

            return CreatedAtAction(nameof(GetClassroom), new { id = classroom.Id }, response);
        }

        // PUT /api/classrooms/{id} - Update classroom (creator only)
        [HttpPut("{id}")]
        public async Task<ActionResult<ClassroomDto>> UpdateClassroom(int id, [FromBody] UpdateClassroomDto dto)
        {
            var userId = GetUserId();

            var classroom = await _db.Classrooms
                .FirstOrDefaultAsync(c => c.Id == id && c.CreatorId == userId);

            if (classroom == null)
            {
                return NotFound("Classroom not found or you are not the creator");
            }

            if (dto.Name != null) classroom.Name = dto.Name;
            if (dto.Description != null) classroom.Description = dto.Description;
            if (dto.IsPublic.HasValue) classroom.IsPublic = dto.IsPublic.Value;
            classroom.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return await GetClassroom(id);
        }

        // DELETE /api/classrooms/{id} - Delete classroom (creator only)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClassroom(int id)
        {
            var userId = GetUserId();

            var classroom = await _db.Classrooms
                .FirstOrDefaultAsync(c => c.Id == id && c.CreatorId == userId);

            if (classroom == null)
            {
                return NotFound("Classroom not found or you are not the creator");
            }

            // Remove all related entities to prevent FK errors
            var members = await _db.ClassroomMembers.Where(m => m.ClassroomId == id).ToListAsync();
            _db.ClassroomMembers.RemoveRange(members);

            var sharedCourses = await _db.ClassroomCourses.Where(cc => cc.ClassroomId == id).ToListAsync();
            _db.ClassroomCourses.RemoveRange(sharedCourses);

            var courseCopies = await _db.CourseCopies.Where(cc => cc.ClassroomId == id).ToListAsync();
            _db.CourseCopies.RemoveRange(courseCopies);

            _db.Classrooms.Remove(classroom);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // POST /api/classrooms/{id}/join - Join classroom by invite code
        [HttpPost("{id}/join")]
        public async Task<ActionResult<ClassroomDto>> JoinClassroom(int id, [FromBody] JoinClassroomDto dto)
        {
            var userId = GetUserId();

            var classroom = await _db.Classrooms
                .FirstOrDefaultAsync(c => c.Id == id && c.InviteCode == dto.InviteCode);

            if (classroom == null)
            {
                return NotFound("Classroom not found or invalid invite code");
            }

            // Check if already a member
            var existingMember = await _db.ClassroomMembers
                .FirstOrDefaultAsync(m => m.ClassroomId == id && m.UserId == userId);

            if (existingMember != null)
            {
                return BadRequest("You are already a member of this classroom");
            }

            var member = new ClassroomMember
            {
                ClassroomId = id,
                UserId = userId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            };

            _db.ClassroomMembers.Add(member);
            await _db.SaveChangesAsync();

            return await GetClassroom(id);
        }

        // POST /api/classrooms/join - Join by invite code only
        [HttpPost("join")]
        public async Task<ActionResult<ClassroomDto>> JoinClassroomByCode([FromBody] JoinClassroomDto dto)
        {
            var userId = GetUserId();

            if (string.IsNullOrWhiteSpace(dto.InviteCode))
            {
                return BadRequest("Invite code is required");
            }

            var classroom = await _db.Classrooms
                .FirstOrDefaultAsync(c => c.InviteCode == dto.InviteCode);

            if (classroom == null)
            {
                return NotFound("Invalid invite code");
            }

            // Check if already a member
            var existingMember = await _db.ClassroomMembers
                .FirstOrDefaultAsync(m => m.ClassroomId == classroom.Id && m.UserId == userId);

            if (existingMember != null)
            {
                return await GetClassroom(classroom.Id);
            }

            var member = new ClassroomMember
            {
                ClassroomId = classroom.Id,
                UserId = userId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            };

            _db.ClassroomMembers.Add(member);
            await _db.SaveChangesAsync();

            return await GetClassroom(classroom.Id);
        }

        // POST /api/classrooms/{id}/leave - Leave classroom
        [HttpPost("{id}/leave")]
        public async Task<IActionResult> LeaveClassroom(int id)
        {
            var userId = GetUserId();

            var member = await _db.ClassroomMembers
                .FirstOrDefaultAsync(m => m.ClassroomId == id && m.UserId == userId);

            if (member == null)
            {
                return NotFound("You are not a member of this classroom");
            }

            // Don't allow creator to leave (they must delete the classroom)
            if (member.Role == "Creator")
            {
                return BadRequest("Classroom creator cannot leave. Delete the classroom instead.");
            }

            _db.ClassroomMembers.Remove(member);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // GET /api/classrooms/{id}/members - List classroom members
        [HttpGet("{id}/members")]
        public async Task<ActionResult<List<ClassroomMemberDto>>> GetMembers(int id)
        {
            var userId = GetUserId();

            // Check if user is a member
            var isMember = await _db.ClassroomMembers
                .AnyAsync(m => m.ClassroomId == id && m.UserId == userId);

            if (!isMember)
            {
                return NotFound("Classroom not found or you are not a member");
            }

            var members = await _db.ClassroomMembers
                .Where(m => m.ClassroomId == id)
                .Include(m => m.User)
                .Select(m => new ClassroomMemberDto
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    UserName = m.User!.FullName,
                    UserEmail = m.User.Email,
                    Role = m.Role,
                    JoinedAt = m.JoinedAt
                })
                .ToListAsync();

            return Ok(members);
        }

        // DELETE /api/classrooms/{id}/members/{memberId} - Remove member (creator only)
        [HttpDelete("{id}/members/{memberId}")]
        public async Task<IActionResult> RemoveMember(int id, int memberId)
        {
            var userId = GetUserId();

            var classroom = await _db.Classrooms
                .FirstOrDefaultAsync(c => c.Id == id && c.CreatorId == userId);

            if (classroom == null)
            {
                return NotFound("Classroom not found or you are not the creator");
            }

            var member = await _db.ClassroomMembers
                .FirstOrDefaultAsync(m => m.ClassroomId == id && m.Id == memberId);

            if (member == null)
            {
                return NotFound("Member not found");
            }

            if (member.Role == "Creator")
            {
                return BadRequest("Cannot remove the classroom creator");
            }

            _db.ClassroomMembers.Remove(member);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // POST /api/classrooms/{id}/courses/share - Share course(s) to classroom
        [HttpPost("{id}/courses/share")]
        public async Task<ActionResult> ShareCourses(int id, [FromBody] ShareCourseDto dto)
        {
            var userId = GetUserId();

            // Check if user is creator or member
            var classroom = await _db.Classrooms
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (classroom == null)
            {
                return NotFound("Classroom not found");
            }

            var isMember = classroom.Members.Any(m => m.UserId == userId);
            if (!isMember)
            {
                return NotFound("You are not a member of this classroom");
            }

            if (dto.CourseIds == null || !dto.CourseIds.Any())
            {
                return BadRequest("At least one course ID is required");
            }

            // Verify user owns all courses
            var courses = await _db.Courses
                .Where(c => dto.CourseIds.Contains(c.Id) && c.UserId == userId)
                .ToListAsync();

            if (courses.Count != dto.CourseIds.Count)
            {
                return BadRequest("You can only share courses that you own");
            }

            var sharedCourses = new List<ClassroomCourse>();
            foreach (var courseId in dto.CourseIds)
            {
                // Check if already shared
                var existing = await _db.ClassroomCourses
                    .FirstOrDefaultAsync(cc => cc.ClassroomId == id && cc.CourseId == courseId);

                if (existing != null)
                {
                    // Reactivate if previously unshared
                    if (!existing.IsActive)
                    {
                        existing.IsActive = true;
                        existing.SharedAt = DateTime.UtcNow;
                        existing.SharedByUserId = userId;
                    }
                }
                else
                {
                    sharedCourses.Add(new ClassroomCourse
                    {
                        ClassroomId = id,
                        CourseId = courseId,
                        SharedByUserId = userId,
                        SharedAt = DateTime.UtcNow,
                        IsActive = true
                    });
                }
            }

            if (sharedCourses.Any())
            {
                _db.ClassroomCourses.AddRange(sharedCourses);
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = $"Successfully shared {dto.CourseIds.Count} course(s) to classroom", courseIds = dto.CourseIds });
        }

        // DELETE /api/classrooms/{id}/courses/{courseId} - Unshare course
        [HttpDelete("{id}/courses/{courseId}")]
        public async Task<IActionResult> UnshareCourse(int id, int courseId)
        {
            var userId = GetUserId();

            var classroomCourse = await _db.ClassroomCourses
                .FirstOrDefaultAsync(cc => cc.ClassroomId == id && cc.CourseId == courseId && cc.SharedByUserId == userId);

            if (classroomCourse == null)
            {
                return NotFound("Shared course not found or you are not the one who shared it");
            }

            classroomCourse.IsActive = false;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // GET /api/classrooms/{id}/courses - Get all shared courses in classroom
        [HttpGet("{id}/courses")]
        public async Task<ActionResult<List<ClassroomCourseDto>>> GetSharedCourses(int id)
        {
            var userId = GetUserId();

            // Check if user is a member
            var isMember = await _db.ClassroomMembers
                .AnyAsync(m => m.ClassroomId == id && m.UserId == userId);

            if (!isMember)
            {
                return NotFound("Classroom not found or you are not a member");
            }

            // Get courses user has copied
            var copiedCourseIds = await _db.CourseCopies
                .Where(cc => cc.UserId == userId && cc.ClassroomId == id)
                .Select(cc => cc.OriginalCourseId)
                .ToListAsync();

            var sharedCourses = await _db.ClassroomCourses
                .Where(cc => cc.ClassroomId == id && cc.IsActive)
                .Include(cc => cc.Course)
                    .ThenInclude(c => c!.Modules)
                .Include(cc => cc.Course)
                    .ThenInclude(c => c!.ExternalLinks)
                .Select(cc => new ClassroomCourseDto
                {
                    Id = cc.Id,
                    CourseId = cc.CourseId,
                    CourseTitle = cc.Course!.Title,
                    CourseDescription = cc.Course.Description,
                    CourseSubjectArea = cc.Course.SubjectArea,
                    CourseDifficulty = cc.Course.Difficulty,
                    CourseTotalEstimatedHours = cc.Course.TotalEstimatedHours,
                    SharedByUserId = cc.SharedByUserId,
                    SharedByUserName = _db.Users.Where(u => u.Id == cc.SharedByUserId).Select(u => u.FullName).FirstOrDefault() ?? "",
                    SharedAt = cc.SharedAt,
                    ModuleCount = cc.Course.Modules.Count,
                    IsCopied = copiedCourseIds.Contains(cc.CourseId)
                })
                .ToListAsync();

            return Ok(sharedCourses);
        }

        // POST /api/classrooms/courses/{classroomCourseId}/copy - Copy shared course to personal library
        [HttpPost("courses/{classroomCourseId}/copy")]
        public async Task<ActionResult<CopyCourseResponseDto>> CopyCourse(int classroomCourseId)
        {
            var userId = GetUserId();

            var classroomCourse = await _db.ClassroomCourses
                .Include(cc => cc.Course)
                    .ThenInclude(c => c!.Modules)
                        .ThenInclude(m => m.SubModules)
                .Include(cc => cc.Course)
                    .ThenInclude(c => c!.ExternalLinks)
                .FirstOrDefaultAsync(cc => cc.Id == classroomCourseId && cc.IsActive);

            if (classroomCourse == null)
            {
                return NotFound("Shared course not found");
            }

            // Check if user is a member of the classroom
            var isMember = await _db.ClassroomMembers
                .AnyAsync(m => m.ClassroomId == classroomCourse.ClassroomId && m.UserId == userId);

            if (!isMember)
            {
                return NotFound("You are not a member of this classroom");
            }

            var originalCourse = classroomCourse.Course;
            if (originalCourse == null)
            {
                return NotFound("Original course not found");
            }

            // Check if already copied by this user (across any classroom) to avoid duplicates
            var existingCopy = await _db.CourseCopies
                .FirstOrDefaultAsync(cc => cc.UserId == userId && cc.OriginalCourseId == originalCourse.Id);

            if (existingCopy != null)
            {
                // Ensure we also have a copy record for this classroom (for IsCopied UI state)
                var existingForThisClassroom = await _db.CourseCopies
                    .AnyAsync(cc => cc.UserId == userId &&
                                   cc.OriginalCourseId == originalCourse.Id &&
                                   cc.ClassroomId == classroomCourse.ClassroomId);

                if (!existingForThisClassroom)
                {
                    _db.CourseCopies.Add(new CourseCopy
                    {
                        OriginalCourseId = originalCourse.Id,
                        CopiedCourseId = existingCopy.CopiedCourseId,
                        UserId = userId,
                        ClassroomId = classroomCourse.ClassroomId,
                        CopiedAt = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync();
                }

                var copiedCourse = await _db.Courses
                    .FirstOrDefaultAsync(c => c.Id == existingCopy.CopiedCourseId);

                return Ok(new CopyCourseResponseDto
                {
                    OriginalCourseId = originalCourse.Id,
                    CopiedCourseId = existingCopy.CopiedCourseId,
                    CourseTitle = copiedCourse?.Title ?? originalCourse.Title,
                    Message = "Course already copied to your library"
                });
            }

            // Create deep copy of course
            var newCourse = new Course
            {
                UserId = userId,
                Title = originalCourse.Title,
                Description = originalCourse.Description,
                SubjectArea = originalCourse.SubjectArea,
                LearningObjectives = originalCourse.LearningObjectives,
                Difficulty = originalCourse.Difficulty,
                Priority = originalCourse.Priority,
                TotalEstimatedHours = originalCourse.TotalEstimatedHours,
                HoursRemaining = originalCourse.HoursRemaining,
                TargetCompletionDate = originalCourse.TargetCompletionDate,
                Notes = originalCourse.Notes,
                IsActive = originalCourse.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Courses.Add(newCourse);
            await _db.SaveChangesAsync();

            // Copy modules
            foreach (var module in originalCourse.Modules)
            {
                var newModule = new CourseModule
                {
                    CourseId = newCourse.Id,
                    Title = module.Title,
                    Description = module.Description,
                    Notes = module.Notes,
                    EstimatedHours = module.EstimatedHours,
                    IsCompleted = false, // Reset completion status
                    Order = module.Order
                };

                _db.CourseModules.Add(newModule);
                await _db.SaveChangesAsync();

                // Copy submodules
                foreach (var subModule in module.SubModules)
                {
                    var newSubModule = new CourseSubModule
                    {
                        CourseModuleId = newModule.Id,
                        Title = subModule.Title,
                        Description = subModule.Description,
                        Notes = subModule.Notes,
                        EstimatedHours = subModule.EstimatedHours,
                        IsCompleted = false, // Reset completion status
                        Order = subModule.Order
                    };

                    _db.CourseSubModules.Add(newSubModule);
                }
            }

            // Copy external links
            foreach (var link in originalCourse.ExternalLinks)
            {
                var newLink = new ExternalLink
                {
                    CourseId = newCourse.Id,
                    Url = link.Url,
                    Title = link.Title,
                    Platform = link.Platform,
                    CreatedAt = DateTime.UtcNow
                };

                _db.ExternalLinks.Add(newLink);
            }

            await _db.SaveChangesAsync();

            // Record the copy
            var courseCopy = new CourseCopy
            {
                OriginalCourseId = originalCourse.Id,
                CopiedCourseId = newCourse.Id,
                UserId = userId,
                ClassroomId = classroomCourse.ClassroomId,
                CopiedAt = DateTime.UtcNow
            };

            _db.CourseCopies.Add(courseCopy);
            await _db.SaveChangesAsync();

            return Ok(new CopyCourseResponseDto
            {
                OriginalCourseId = originalCourse.Id,
                CopiedCourseId = newCourse.Id,
                CourseTitle = newCourse.Title,
                Message = "Course successfully copied to your library"
            });
        }

        // GET /api/classrooms/{id}/progress - Get member progress for all courses in classroom
        [HttpGet("{id}/progress")]
        public async Task<ActionResult<List<MemberProgressDto>>> GetMemberProgress(int id)
        {
            var userId = GetUserId();

            // Check if user is a member
            var isMember = await _db.ClassroomMembers
                .AnyAsync(m => m.ClassroomId == id && m.UserId == userId);

            if (!isMember)
            {
                return NotFound("Classroom not found or you are not a member");
            }

            // Get all shared courses in the classroom
            var sharedCourseIds = await _db.ClassroomCourses
                .Where(cc => cc.ClassroomId == id && cc.IsActive)
                .Select(cc => cc.CourseId)
                .ToListAsync();

            if (!sharedCourseIds.Any())
            {
                return Ok(new List<MemberProgressDto>());
            }

            // Get all members
            var classroomMembers = await _db.ClassroomMembers
                .Where(m => m.ClassroomId == id)
                .Include(m => m.User)
                .ToListAsync();

            var memberProgressList = new List<MemberProgressDto>();

            foreach (var member in classroomMembers)
            {
                // Get all courses this member has copied from this classroom
                var copiedCourses = await _db.CourseCopies
                    .Where(cc => cc.UserId == member.UserId && 
                                 cc.ClassroomId == id && 
                                 sharedCourseIds.Contains(cc.OriginalCourseId))
                    .Include(cc => cc.CopiedCourse)
                        .ThenInclude(c => c!.Modules)
                    .ToListAsync();

                if (!copiedCourses.Any())
                {
                    // Member hasn't copied any courses yet
                    memberProgressList.Add(new MemberProgressDto
                    {
                        UserId = member.UserId,
                        UserName = member.User!.FullName,
                        UserEmail = member.User.Email,
                        TotalCourses = sharedCourseIds.Count,
                        CompletedCourses = 0,
                        TotalModules = 0,
                        CompletedModules = 0,
                        TotalHours = 0,
                        CompletedHours = 0,
                        ProgressPercentage = 0
                    });
                    continue;
                }

                var courseProgressList = new List<ClassroomCourseProgressDto>();
                int totalModules = 0;
                int completedModules = 0;
                int totalHours = 0;
                int completedHours = 0;
                int completedCourses = 0;

                foreach (var copy in copiedCourses)
                {
                    var course = copy.CopiedCourse;
                    if (course == null) continue;

                    var modules = course.Modules ?? new List<CourseModule>();
                    var courseTotalModules = modules.Count;
                    var courseCompletedModules = modules.Count(m => m.IsCompleted);
                    var courseTotalHours = modules.Sum(m => m.EstimatedHours);
                    var courseCompletedHours = modules.Where(m => m.IsCompleted).Sum(m => m.EstimatedHours);
                    var courseProgress = courseTotalModules > 0 
                        ? (double)courseCompletedModules / courseTotalModules * 100 
                        : 0;
                    var isCourseCompleted = courseTotalModules > 0 && courseCompletedModules == courseTotalModules;

                    if (isCourseCompleted)
                    {
                        completedCourses++;
                    }

                    totalModules += courseTotalModules;
                    completedModules += courseCompletedModules;
                    totalHours += courseTotalHours;
                    completedHours += courseCompletedHours;

                    courseProgressList.Add(new ClassroomCourseProgressDto
                    {
                        CourseId = course.Id,
                        CourseTitle = course.Title,
                        TotalModules = courseTotalModules,
                        CompletedModules = courseCompletedModules,
                        TotalHours = courseTotalHours,
                        CompletedHours = courseCompletedHours,
                        ProgressPercentage = courseProgress,
                        IsCompleted = isCourseCompleted,
                        LastStudiedAt = course.LastStudiedAt
                    });
                }

                var overallProgress = totalModules > 0 
                    ? (double)completedModules / totalModules * 100 
                    : 0;

                memberProgressList.Add(new MemberProgressDto
                {
                    UserId = member.UserId,
                    UserName = member.User!.FullName,
                    UserEmail = member.User.Email,
                    TotalCourses = sharedCourseIds.Count,
                    CompletedCourses = completedCourses,
                    TotalModules = totalModules,
                    CompletedModules = completedModules,
                    TotalHours = totalHours,
                    CompletedHours = completedHours,
                    ProgressPercentage = overallProgress,
                    CourseProgress = courseProgressList
                });
            }

            // Sort by progress percentage (descending)
            memberProgressList = memberProgressList
                .OrderByDescending(m => m.ProgressPercentage)
                .ToList();

            return Ok(memberProgressList);
        }

        // GET /api/classrooms/public - Discover public classrooms
        [HttpGet("public")]
        public async Task<ActionResult<List<ClassroomDto>>> GetPublicClassrooms()
        {
            var userId = GetUserId();

            var classrooms = await _db.Classrooms
                .Where(c => c.IsPublic)
                .Include(c => c.Members)
                .Include(c => c.SharedCourses.Where(cc => cc.IsActive))
                .Select(c => new ClassroomDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    CreatorId = c.CreatorId,
                    CreatorName = _db.Users.Where(u => u.Id == c.CreatorId).Select(u => u.FullName).FirstOrDefault() ?? "",
                    InviteCode = c.InviteCode,
                    IsPublic = c.IsPublic,
                    CreatedAt = c.CreatedAt,
                    MemberCount = c.Members.Count,
                    CourseCount = c.SharedCourses.Count(cc => cc.IsActive),
                    UserRole = c.Members.Where(m => m.UserId == userId).Select(m => m.Role).FirstOrDefault() ?? "",
                    IsCreator = c.CreatorId == userId
                })
                .OrderByDescending(c => c.CreatedAt)
                .Take(50)
                .ToListAsync();

            return Ok(classrooms);
        }
    }
}

