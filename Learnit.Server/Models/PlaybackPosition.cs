namespace Learnit.Server.Models
{
    /// <summary>
    /// Stores the last playback position for a user's course module (for YouTube videos)
    /// This allows resuming playback after logout/login
    /// </summary>
    public class PlaybackPosition
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CourseId { get; set; }
        public int? ModuleId { get; set; } // Optional: for module-specific playback
        public string VideoId { get; set; } = ""; // YouTube video ID or playlist ID
        public string PlaylistId { get; set; } = ""; // Optional: for playlist videos
        public double CurrentTimeSeconds { get; set; } // Last watched position in seconds
        public double DurationSeconds { get; set; } // Total video duration
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public User? User { get; set; }
        public Course? Course { get; set; }
        public CourseModule? Module { get; set; }
    }
}

