namespace Learnit.Server.Models
{
    public class Classroom
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int CreatorId { get; set; } // User who created the classroom
        public string InviteCode { get; set; } = ""; // Unique invite code (6-8 characters)
        public bool IsPublic { get; set; } = false; // Can be discovered publicly
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public List<ClassroomMember> Members { get; set; } = new();
        public List<ClassroomCourse> SharedCourses { get; set; } = new();
    }
}

