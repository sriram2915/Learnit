namespace Learnit.Server.Models
{
    public class Achievement
    {
        public int Id { get; set; }
        public string Code { get; set; } = ""; // Unique code like "FIRST_COURSE", "100_HOURS"
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = ""; // Icon name or emoji
        public string Category { get; set; } = ""; // "courses", "hours", "streaks", "quizzes"
        public int? RequiredValue { get; set; } // e.g., 100 for "100_HOURS", 10 for "10_COURSES"
        public string BadgeColor { get; set; } = "#2563eb"; // Color for the badge
        public int Points { get; set; } = 10; // Points awarded
        public int Rarity { get; set; } = 1; // 1=Common, 2=Rare, 3=Epic, 4=Legendary
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public List<UserAchievement> UserAchievements { get; set; } = new();
    }
}

