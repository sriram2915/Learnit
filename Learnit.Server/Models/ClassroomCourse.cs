namespace Learnit.Server.Models
{
    public class ClassroomCourse
    {
        public int Id { get; set; }
        public int ClassroomId { get; set; }
        public int CourseId { get; set; } // Original course ID
        public int SharedByUserId { get; set; } // User who shared the course
        public DateTime SharedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true; // Can be unshared

        // Navigation properties
        public Classroom? Classroom { get; set; }
        public Course? Course { get; set; }
    }
}

