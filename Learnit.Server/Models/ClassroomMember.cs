namespace Learnit.Server.Models
{
    public class ClassroomMember
    {
        public int Id { get; set; }
        public int ClassroomId { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; } = "Member"; // Creator, Admin, Member
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Classroom? Classroom { get; set; }
        public User? User { get; set; }
    }
}

