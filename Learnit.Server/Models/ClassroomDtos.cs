namespace Learnit.Server.Models
{
    public class CreateClassroomDto
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsPublic { get; set; } = false;
    }

    public class UpdateClassroomDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsPublic { get; set; }
    }

    public class ClassroomDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int CreatorId { get; set; }
        public string CreatorName { get; set; } = "";
        public string InviteCode { get; set; } = "";
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
        public int CourseCount { get; set; }
        public string UserRole { get; set; } = "Member"; // Current user's role in this classroom
        public bool IsCreator { get; set; }
    }

    public class ClassroomMemberDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public string Role { get; set; } = "Member";
        public DateTime JoinedAt { get; set; }
    }

    public class ShareCourseDto
    {
        public List<int> CourseIds { get; set; } = new();
    }

    public class ClassroomCourseDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = "";
        public string CourseDescription { get; set; } = "";
        public string CourseSubjectArea { get; set; } = "";
        public string CourseDifficulty { get; set; } = "";
        public int CourseTotalEstimatedHours { get; set; }
        public int SharedByUserId { get; set; }
        public string SharedByUserName { get; set; } = "";
        public DateTime SharedAt { get; set; }
        public int ModuleCount { get; set; }
        public bool IsCopied { get; set; } // Whether current user has copied this course
    }

    public class JoinClassroomDto
    {
        public string InviteCode { get; set; } = "";
    }

    public class CopyCourseResponseDto
    {
        public int OriginalCourseId { get; set; }
        public int CopiedCourseId { get; set; }
        public string CourseTitle { get; set; } = "";
        public string Message { get; set; } = "";
    }

    public class MemberProgressDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public int TotalCourses { get; set; }
        public int CompletedCourses { get; set; }
        public int TotalModules { get; set; }
        public int CompletedModules { get; set; }
        public decimal TotalHours { get; set; }
        public decimal CompletedHours { get; set; }
        public double ProgressPercentage { get; set; }
        public List<ClassroomCourseProgressDto> CourseProgress { get; set; } = new();
    }

    public class ClassroomCourseProgressDto
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = "";
        public int TotalModules { get; set; }
        public int CompletedModules { get; set; }
        public int TotalHours { get; set; }
        public int CompletedHours { get; set; }
        public double ProgressPercentage { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? LastStudiedAt { get; set; }
    }
}

