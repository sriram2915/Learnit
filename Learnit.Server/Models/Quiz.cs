namespace Learnit.Server.Models
{
    /// <summary>
    /// Quiz associated with a course module
    /// Used to verify completion of external course modules
    /// </summary>
    public class Quiz
    {
        public int Id { get; set; }
        public int CourseModuleId { get; set; }
        public string Title { get; set; } = ""; // Usually matches module title
        public string Description { get; set; } = ""; // Instructions or context
        public int PassingScore { get; set; } = 70; // Percentage required to pass (default 70%)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public CourseModule? CourseModule { get; set; }
        public List<QuizQuestion> Questions { get; set; } = new();
        public List<QuizAttempt> Attempts { get; set; } = new();
    }

    /// <summary>
    /// Individual question in a quiz
    /// </summary>
    public class QuizQuestion
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public string QuestionText { get; set; } = "";
        public string QuestionType { get; set; } = "multiple_choice"; // multiple_choice, true_false
        public int Order { get; set; }
        public int Points { get; set; } = 1; // Points awarded for correct answer
        
        // Navigation properties
        public Quiz? Quiz { get; set; }
        public List<QuizOption> Options { get; set; } = new(); // For multiple choice/true-false
    }

    /// <summary>
    /// Answer options for multiple choice questions
    /// </summary>
    public class QuizOption
    {
        public int Id { get; set; }
        public int QuizQuestionId { get; set; }
        public string OptionText { get; set; } = "";
        public bool IsCorrect { get; set; } = false;
        public int Order { get; set; }
        
        // Navigation property
        public QuizQuestion? QuizQuestion { get; set; }
    }

    /// <summary>
    /// User's attempt to complete a quiz
    /// </summary>
    public class QuizAttempt
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public int UserId { get; set; }
        public int Score { get; set; } = 0; // Percentage score
        public bool Passed { get; set; } = false;
        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
        public int TimeSpentSeconds { get; set; } = 0; // Time taken to complete quiz
        
        // Navigation properties
        public Quiz? Quiz { get; set; }
        public User? User { get; set; }
        public List<QuizAnswer> Answers { get; set; } = new();
    }

    /// <summary>
    /// User's answer to a specific question in a quiz attempt
    /// </summary>
    public class QuizAnswer
    {
        public int Id { get; set; }
        public int QuizAttemptId { get; set; }
        public int QuizQuestionId { get; set; }
        public int? SelectedOptionId { get; set; } // Selected option ID for multiple choice
        public bool IsCorrect { get; set; } = false;
        public int PointsEarned { get; set; } = 0;
        
        // Navigation properties
        public QuizAttempt? QuizAttempt { get; set; }
        public QuizQuestion? QuizQuestion { get; set; }
    }
}

