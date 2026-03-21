namespace Learnit.Server.Models
{
    public class UserAchievement
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AchievementId { get; set; }
        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
        public int Progress { get; set; } = 0; // Current progress towards achievement (if applicable)

        // Navigation properties
        public User? User { get; set; }
        public Achievement? Achievement { get; set; }
    }
}

