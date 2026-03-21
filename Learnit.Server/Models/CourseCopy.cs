namespace Learnit.Server.Models
{
    public class CourseCopy
    {
        public int Id { get; set; }
        public int OriginalCourseId { get; set; } // Original shared course
        public int CopiedCourseId { get; set; } // User's copy
        public int UserId { get; set; } // User who copied
        public int? ClassroomId { get; set; } // Which classroom it came from
        public DateTime CopiedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Course? OriginalCourse { get; set; }
        public Course? CopiedCourse { get; set; }
        public User? User { get; set; }
        public Classroom? Classroom { get; set; }
    }
}

