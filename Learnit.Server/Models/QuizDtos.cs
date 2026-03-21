namespace Learnit.Server.Models
{
    public class QuizDto
    {
        public int Id { get; set; }
        public int CourseModuleId { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int PassingScore { get; set; }
        public List<QuizQuestionDto> Questions { get; set; } = new();
    }

    public class QuizQuestionDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = "";
        public string QuestionType { get; set; } = "";
        public int Order { get; set; }
        public int Points { get; set; }
        public List<QuizOptionDto> Options { get; set; } = new();
    }

    public class QuizOptionDto
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = "";
        public int Order { get; set; }
        // Don't include IsCorrect in DTO to prevent cheating
    }

    public class SubmitQuizAttemptDto
    {
        public List<QuizAnswerSubmissionDto> Answers { get; set; } = new();
        public int TimeSpentSeconds { get; set; }
    }

    public class QuizAnswerSubmissionDto
    {
        public int QuestionId { get; set; }
        public int? SelectedOptionId { get; set; }
    }

    public class QuizAttemptResultDto
    {
        public int AttemptId { get; set; }
        public int Score { get; set; }
        public bool Passed { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public List<QuizAnswerResultDto> AnswerResults { get; set; } = new();
    }

    public class QuizAnswerResultDto
    {
        public int QuestionId { get; set; }
        public bool IsCorrect { get; set; }
        public int? CorrectOptionId { get; set; }
        public int PointsEarned { get; set; }
    }

    public class QuizAttemptHistoryDto
    {
        public int Id { get; set; }
        public int Score { get; set; }
        public bool Passed { get; set; }
        public DateTime AttemptedAt { get; set; }
        public int TimeSpentSeconds { get; set; }
    }
}

