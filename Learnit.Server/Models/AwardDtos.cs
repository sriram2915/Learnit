namespace Learnit.Server.Models
{
    public class AwardDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Category { get; set; } = "";
        public int? Threshold { get; set; }
        public string Color { get; set; } = "";
        public bool IsEarned { get; set; }
        public DateTime? EarnedAt { get; set; }
        public int? ProgressValue { get; set; }
        public double? ProgressPercentage { get; set; } // Progress toward earning (0-100)
    }

    public class UserAwardsSummaryDto
    {
        public int TotalAwards { get; set; }
        public int EarnedAwards { get; set; }
        public List<AwardDto> Awards { get; set; } = new();
        public Dictionary<string, int> CategoryCounts { get; set; } = new(); // Category -> count
    }

    public class AwardProgressDto
    {
        public string AwardCode { get; set; } = "";
        public int CurrentValue { get; set; }
        public int? TargetValue { get; set; }
        public double ProgressPercentage { get; set; }
        public bool IsEarned { get; set; }
    }
}

