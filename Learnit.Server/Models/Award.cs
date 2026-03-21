namespace Learnit.Server.Models
{
    public class Award
    {
        public int Id { get; set; }
        public string Code { get; set; } = ""; // Unique code like "HOURS_10", "STREAK_7", "COURSES_5"
        public string Name { get; set; } = ""; // Display name
        public string Description { get; set; } = ""; // What user needs to do
        public string Icon { get; set; } = ""; // Icon name or emoji
        public string Category { get; set; } = ""; // "hours", "courses", "streak", "consistency"
        public int? Threshold { get; set; } // For numeric awards (hours, days, courses)
        public string Color { get; set; } = "#2563eb"; // Badge color
        public int Order { get; set; } = 0; // Display order
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public List<UserAward> UserAwards { get; set; } = new();
    }
}

