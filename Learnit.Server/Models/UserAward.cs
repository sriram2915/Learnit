namespace Learnit.Server.Models
{
    public class UserAward
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AwardId { get; set; }
        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
        public int? ProgressValue { get; set; } // The value when earned (e.g., 10 hours, 7 days streak)

        // Navigation properties
        public User? User { get; set; }
        public Award? Award { get; set; }
    }
}

