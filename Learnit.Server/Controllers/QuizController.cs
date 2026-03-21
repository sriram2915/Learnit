using Learnit.Server.Data;
using Learnit.Server.Models;
using Learnit.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace Learnit.Server.Controllers
{
    [ApiController]
    [Route("api/quizzes")]
    [Authorize]
    public class QuizController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IAiProvider _aiProvider;

        public QuizController(AppDbContext db, IAiProvider aiProvider)
        {
            _db = db;
            _aiProvider = aiProvider;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedAccessException("Invalid user token");

            return userId;
        }

        /// <summary>
        /// Generate or get quiz for a module
        /// </summary>
        [HttpGet("module/{moduleId}")]
        public async Task<IActionResult> GetQuizForModule(int moduleId)
        {
            var userId = GetUserId();
            
            // Verify module belongs to user's course and load related data
            var module = await _db.CourseModules
                .Include(m => m.Course)
                .Include(m => m.SubModules)
                .FirstOrDefaultAsync(m => m.Id == moduleId && m.Course!.UserId == userId);

            if (module == null)
                return NotFound();

            if (!module.Course!.IsQuizEnabled)
            {
                return BadRequest(new
                {
                    message = "Quizzes are disabled for this course.",
                    quizzesDisabled = true
                });
            }

            // Check if quiz exists
            var quiz = await _db.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(qq => qq.Options)
                .FirstOrDefaultAsync(q => q.CourseModuleId == moduleId);

            // Check if quiz is a dummy/fallback quiz (only 1 question with "Did you complete studying" text)
            bool isDummyQuiz = quiz != null && 
                quiz.Questions.Count == 1 && 
                quiz.Questions[0].QuestionText.Contains("Did you complete studying", StringComparison.OrdinalIgnoreCase);

            if (quiz == null || isDummyQuiz)
            {
                // If dummy quiz exists, delete it first (with all related records)
                if (isDummyQuiz)
                {
                    Console.WriteLine($"[Quiz Generation] Detected dummy quiz for module {moduleId}, regenerating...");
                    
                    var quizId = quiz!.Id;
                    
                    // Delete in correct order to handle foreign key constraints:
                    // 1. Delete QuizAnswers first (they reference QuizQuestions with RESTRICT)
                    // 2. Delete QuizAttempts (cascades to QuizAnswers, but we need to delete answers first)
                    // 3. Delete Quiz (cascades to QuizQuestions and QuizOptions)
                    
                    // Get all quiz attempts for this quiz
                    var attempts = await _db.QuizAttempts
                        .Where(qa => qa.QuizId == quizId)
                        .ToListAsync();
                    
                    // Get all quiz answers for these attempts
                    var attemptIds = attempts.Select(a => a.Id).ToList();
                    var answers = await _db.QuizAnswers
                        .Where(qa => attemptIds.Contains(qa.QuizAttemptId))
                        .ToListAsync();
                    
                    // Delete answers first (they have RESTRICT constraint on QuizQuestion)
                    if (answers.Any())
                    {
                        _db.QuizAnswers.RemoveRange(answers);
                        await _db.SaveChangesAsync();
                    }
                    
                    // Now delete attempts
                    if (attempts.Any())
                    {
                        _db.QuizAttempts.RemoveRange(attempts);
                        await _db.SaveChangesAsync();
                    }
                    
                    // Finally, delete the quiz (will cascade to questions and options)
                    _db.Quizzes.Remove(quiz);
                    await _db.SaveChangesAsync();
                    
                    Console.WriteLine($"[Quiz Generation] Successfully deleted dummy quiz {quizId} and all related records.");
                }
                
                try
                {
                    // Generate quiz using AI with full module context
                    quiz = await GenerateQuizForModule(module);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Quiz Generation] ERROR: Failed to generate quiz for module {moduleId}: {ex.Message}");
                    Console.WriteLine($"[Quiz Generation] Stack trace: {ex.StackTrace}");
                    return StatusCode(500, new { 
                        message = "Failed to generate quiz. Please check server logs for details.",
                        error = ex.Message 
                    });
                }
            }

            // Return quiz without correct answers
            var quizDto = new QuizDto
            {
                Id = quiz.Id,
                CourseModuleId = quiz.CourseModuleId,
                Title = quiz.Title,
                Description = quiz.Description,
                PassingScore = quiz.PassingScore,
                Questions = quiz.Questions
                    .OrderBy(q => q.Order)
                    .Select(q => new QuizQuestionDto
                    {
                        Id = q.Id,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        Order = q.Order,
                        Points = q.Points,
                        Options = q.Options
                            .OrderBy(o => o.Order)
                            .Select(o => new QuizOptionDto
                            {
                                Id = o.Id,
                                OptionText = o.OptionText,
                                Order = o.Order
                            }).ToList()
                    }).ToList()
            };

            return Ok(quizDto);
        }

        /// <summary>
        /// Submit quiz attempt
        /// </summary>
        [HttpPost("{quizId}/attempt")]
        public async Task<IActionResult> SubmitQuizAttempt(int quizId, [FromBody] SubmitQuizAttemptDto dto)
        {
            var userId = GetUserId();

            var quiz = await _db.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(qq => qq.Options)
                .Include(q => q.CourseModule)
                    .ThenInclude(m => m.Course)
                .Include(q => q.CourseModule)
                    .ThenInclude(m => m.SubModules)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
                return NotFound();

            // Verify quiz belongs to user's course
            if (quiz.CourseModule!.Course!.UserId != userId)
                return Forbid();

            if (!quiz.CourseModule.Course.IsQuizEnabled)
            {
                return BadRequest(new
                {
                    message = "Quizzes are disabled for this course.",
                    quizzesDisabled = true
                });
            }

            // Calculate score
            var totalPoints = quiz.Questions.Sum(q => q.Points);
            var earnedPoints = 0;
            var answerResults = new List<QuizAnswerResultDto>();

            var attempt = new QuizAttempt
            {
                QuizId = quizId,
                UserId = userId,
                TimeSpentSeconds = dto.TimeSpentSeconds,
                AttemptedAt = DateTime.UtcNow,
                Answers = new List<QuizAnswer>()
            };

            foreach (var answerDto in dto.Answers)
            {
                var question = quiz.Questions.FirstOrDefault(q => q.Id == answerDto.QuestionId);
                if (question == null) continue;

                var selectedOption = answerDto.SelectedOptionId.HasValue
                    ? question.Options.FirstOrDefault(o => o.Id == answerDto.SelectedOptionId.Value)
                    : null;

                var isCorrect = selectedOption?.IsCorrect ?? false;
                var pointsEarned = isCorrect ? question.Points : 0;
                earnedPoints += pointsEarned;

                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);

                answerResults.Add(new QuizAnswerResultDto
                {
                    QuestionId = question.Id,
                    IsCorrect = isCorrect,
                    CorrectOptionId = correctOption?.Id,
                    PointsEarned = pointsEarned
                });

                attempt.Answers.Add(new QuizAnswer
                {
                    QuizQuestionId = question.Id,
                    SelectedOptionId = answerDto.SelectedOptionId,
                    IsCorrect = isCorrect,
                    PointsEarned = pointsEarned
                });
            }

            var score = totalPoints > 0 ? (int)Math.Round((double)earnedPoints / totalPoints * 100) : 0;
            var passed = score >= quiz.PassingScore;

            attempt.Score = score;
            attempt.Passed = passed;

            _db.QuizAttempts.Add(attempt);
            await _db.SaveChangesAsync();

            // If passed, mark module as completed
            if (passed && !quiz.CourseModule.IsCompleted)
            {
                quiz.CourseModule.IsCompleted = true;

                // Completing a top-level module should also complete all its submodules
                if (quiz.CourseModule.SubModules != null && quiz.CourseModule.SubModules.Count > 0)
                {
                    foreach (var sub in quiz.CourseModule.SubModules)
                    {
                        sub.IsCompleted = true;
                    }
                }

                quiz.CourseModule.Course!.UpdatedAt = DateTime.UtcNow;

                // Recalculate hours remaining
                var course = await _db.Courses
                    .AsNoTracking()
                    .Include(c => c.Modules)
                    .FirstOrDefaultAsync(c => c.Id == quiz.CourseModule.CourseId);

                if (course != null)
                {
                    var completedModuleHours = course.Modules.Where(m => m.IsCompleted).Sum(m => m.EstimatedHours);
                    var courseForUpdate = await _db.Courses.FindAsync(course.Id);
                    if (courseForUpdate != null)
                    {
                        courseForUpdate.HoursRemaining = Math.Max(0, courseForUpdate.TotalEstimatedHours - completedModuleHours);
                    }
                }

                await _db.SaveChangesAsync();
            }

            var result = new QuizAttemptResultDto
            {
                AttemptId = attempt.Id,
                Score = score,
                Passed = passed,
                TotalQuestions = quiz.Questions.Count,
                CorrectAnswers = answerResults.Count(r => r.IsCorrect),
                AnswerResults = answerResults
            };

            return Ok(result);
        }

        /// <summary>
        /// Get user's quiz attempt history
        /// </summary>
        [HttpGet("{quizId}/attempts")]
        public async Task<IActionResult> GetQuizAttempts(int quizId)
        {
            var userId = GetUserId();

            var attempts = await _db.QuizAttempts
                .Where(qa => qa.QuizId == quizId && qa.UserId == userId)
                .OrderByDescending(qa => qa.AttemptedAt)
                .Select(qa => new QuizAttemptHistoryDto
                {
                    Id = qa.Id,
                    Score = qa.Score,
                    Passed = qa.Passed,
                    AttemptedAt = qa.AttemptedAt,
                    TimeSpentSeconds = qa.TimeSpentSeconds
                })
                .ToListAsync();

            return Ok(attempts);
        }

        private async Task<Quiz> GenerateQuizForModule(CourseModule module)
        {
            var systemPrompt = @"You are an expert educator and technical interviewer creating a quiz for an online learning platform.

CRITICAL REQUIREMENTS:
- Generate 8-10 questions that are SPECIFIC to this exact module's content
- Questions MUST be based ONLY on the module title, description, notes, and submodules provided
- DO NOT generate generic questions - they must test understanding of THIS specific module's topics

QUESTION QUALITY REQUIREMENTS:
- Focus on DEEP UNDERSTANDING, not definitions or surface-level knowledge
- Prefer questions about: WHY, HOW, edge cases, trade-offs, and real-world usage
- Avoid obvious or trivial questions that anyone could guess
- Questions should reflect what someone must know to ACTUALLY APPLY the topic
- Test practical knowledge and problem-solving ability
- Include scenario-based questions when applicable

QUESTION TYPES:
- Mostly multiple choice (4 options each, labeled A, B, C, D)
- 1-2 scenario-based questions if applicable to the topic
- Each question must test conceptual understanding or practical application

CRITICAL: You MUST respond with ONLY valid JSON. Do NOT include markdown code fences, explanations, or any text outside the JSON object.

REQUIRED JSON FORMAT (respond with exactly this structure, no other text):
{
  ""questions"": [
    {
      ""questionText"": ""Your question text here"",
      ""questionType"": ""multiple_choice"",
      ""options"": [
        { ""optionText"": ""Option A text"", ""isCorrect"": false },
        { ""optionText"": ""Option B text"", ""isCorrect"": true },
        { ""optionText"": ""Option C text"", ""isCorrect"": false },
        { ""optionText"": ""Option D text"", ""isCorrect"": false }
      ]
    }
  ]
}

IMPORTANT: 
- Each question MUST have exactly 4 options
- Exactly ONE option per question must have ""isCorrect"": true
- All other options must have ""isCorrect"": false
- Do NOT use markdown code blocks (```json or ```)
- Return ONLY the JSON object, nothing else";

            // Build comprehensive module context
            var moduleContext = new System.Text.StringBuilder();
            moduleContext.AppendLine($"MODULE TITLE: {module.Title ?? "N/A"}");
            moduleContext.AppendLine($"MODULE DESCRIPTION: {module.Description ?? "N/A"}");
            
            if (!string.IsNullOrWhiteSpace(module.Notes))
            {
                moduleContext.AppendLine($"MODULE NOTES/CONTENT: {module.Notes}");
            }
            
            if (module.SubModules != null && module.SubModules.Any())
            {
                moduleContext.AppendLine($"SUBMODULES ({module.SubModules.Count}):");
                foreach (var subModule in module.SubModules.OrderBy(sm => sm.Order))
                {
                    moduleContext.AppendLine($"  - {subModule.Title ?? "Untitled"}: {subModule.Description ?? "No description"}");
                }
            }
            
            // Include course context for better understanding
            if (module.Course != null)
            {
                moduleContext.AppendLine($"COURSE CONTEXT:");
                moduleContext.AppendLine($"  Course Title: {module.Course.Title ?? "N/A"}");
                moduleContext.AppendLine($"  Subject Area: {module.Course.SubjectArea ?? "N/A"}");
                if (!string.IsNullOrWhiteSpace(module.Course.LearningObjectives))
                {
                    moduleContext.AppendLine($"  Course Learning Objectives: {module.Course.LearningObjectives}");
                }
            }
            
            moduleContext.AppendLine($"Estimated Study Time: {module.EstimatedHours} hours");

            // Determine difficulty level from course context
            var difficulty = module.Course?.Difficulty ?? "Intermediate";
            var questionCount = 8; // Default, can be adjusted based on module complexity

            var userPrompt = $@"Generate a quiz for the following course and module.

Course title: {module.Course?.Title ?? "Course"}

Module title: {module.Title}

Difficulty: {difficulty} (Beginner / Intermediate / Advanced)

Instructions:

Questions must be based strictly on the module topic provided below.

Focus on deep understanding, not definitions.

Prefer why, how, edge cases, trade-offs, and real-world usage.

Avoid surface-level or obvious questions.

Questions should reflect what someone must know to actually apply the topic.

MODULE CONTENT:
{moduleContext}

Quiz requirements:

Total questions: {questionCount}

Question types:
- Mostly multiple choice (4 options each, labeled A, B, C, D)
- 1-2 scenario-based questions if applicable

Each question must:
- Test deep understanding of the module content
- Require knowledge of concepts, not just memorization
- Include 4 options (A, B, C, D) with one correct answer
- Be specific to the module topic provided above

IMPORTANT: 
- Create questions that are SPECIFIC to the module title, description, and content provided
- Reference specific concepts, topics, or details mentioned in the module information
- Do NOT create generic questions - they must be tailored to THIS specific module
- If submodules are listed, you may reference them in questions
- Ensure questions test actual understanding and application of the module's subject matter";

            string aiResponse;
            try
            {
                aiResponse = await _aiProvider.GenerateAsync(systemPrompt, userPrompt, null, CancellationToken.None);
                
                // Log the response for debugging
                Console.WriteLine($"[Quiz Generation] AI Response received (length: {aiResponse?.Length ?? 0})");
                if (aiResponse != null && aiResponse.Length > 0)
                {
                    var previewLength = Math.Min(1000, aiResponse.Length);
                    Console.WriteLine($"[Quiz Generation] AI Response preview (first {previewLength} chars):");
                    Console.WriteLine(aiResponse.Substring(0, previewLength));
                    if (aiResponse.Length > 1000)
                    {
                        Console.WriteLine($"[Quiz Generation] ... (truncated, total length: {aiResponse.Length})");
                    }
                }
                
                // Check if it's a stub response
                if (aiResponse.Contains("Stubbed AI response") || aiResponse.Contains("Key not set"))
                {
                    Console.WriteLine("[Quiz Generation] WARNING: AI provider returned stub response. API key may not be configured.");
                    throw new InvalidOperationException("AI provider is not properly configured. Please set Groq:ApiKey or OpenAi:ApiKey in configuration.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Quiz Generation] ERROR calling AI provider: {ex.Message}");
                Console.WriteLine($"[Quiz Generation] Stack trace: {ex.StackTrace}");
                throw new InvalidOperationException($"Failed to generate quiz: {ex.Message}. Please ensure AI provider is configured correctly.", ex);
            }

            // Parse AI response
            var quiz = ParseQuizFromAiResponse(aiResponse, module);
            
            // Validate quiz has questions
            if (quiz.Questions == null || !quiz.Questions.Any())
            {
                throw new InvalidOperationException("Generated quiz has no questions. AI response may be invalid.");
            }

            _db.Quizzes.Add(quiz);
            await _db.SaveChangesAsync();
            
            Console.WriteLine($"[Quiz Generation] Successfully created quiz with {quiz.Questions.Count} questions for module {module.Id}");

            return quiz;
        }

        private Quiz ParseQuizFromAiResponse(string aiResponse, CourseModule module)
        {
            var quiz = new Quiz
            {
                CourseModuleId = module.Id,
                Title = $"Quiz: {module.Title}",
                Description = "Complete this quiz to verify your understanding of the module content.",
                PassingScore = 70,
                Questions = new List<QuizQuestion>()
            };

            if (string.IsNullOrWhiteSpace(aiResponse))
            {
                throw new InvalidOperationException("AI response is empty. Cannot generate quiz.");
            }

            try
            {
                var json = ExtractJsonBlock(aiResponse);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    throw new InvalidOperationException("Could not extract JSON from AI response.");
                }
                
                Console.WriteLine($"[Quiz Generation] Extracted JSON (length: {json.Length})");
                
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("questions", out var questionsElement))
                {
                    throw new InvalidOperationException("AI response does not contain 'questions' property.");
                }

                if (questionsElement.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException("'questions' property is not an array.");
                }

                int order = 0;
                foreach (var q in questionsElement.EnumerateArray())
                {
                    if (!q.TryGetProperty("questionText", out var questionTextElement))
                    {
                        Console.WriteLine("[Quiz Generation] WARNING: Question missing 'questionText', skipping.");
                        continue;
                    }
                    
                    var questionText = questionTextElement.GetString() ?? "";
                    if (string.IsNullOrWhiteSpace(questionText))
                    {
                        Console.WriteLine("[Quiz Generation] WARNING: Question text is empty, skipping.");
                        continue;
                    }
                    
                    var questionType = q.TryGetProperty("questionType", out var typeElement) 
                        ? typeElement.GetString() ?? "multiple_choice" 
                        : "multiple_choice";
                    
                    if (!q.TryGetProperty("options", out var optionsElement))
                    {
                        Console.WriteLine("[Quiz Generation] WARNING: Question missing 'options', skipping.");
                        continue;
                    }
                    
                    if (optionsElement.ValueKind != JsonValueKind.Array)
                    {
                        Console.WriteLine("[Quiz Generation] WARNING: 'options' is not an array, skipping.");
                        continue;
                    }

                    var question = new QuizQuestion
                    {
                        QuizId = 0, // Will be set after save
                        QuestionText = questionText,
                        QuestionType = questionType,
                        Order = order++,
                        Points = 1,
                        Options = new List<QuizOption>()
                    };

                    int optionOrder = 0;
                    bool hasCorrectAnswer = false;
                    
                    foreach (var opt in optionsElement.EnumerateArray())
                    {
                        if (!opt.TryGetProperty("optionText", out var optionTextElement))
                        {
                            Console.WriteLine("[Quiz Generation] WARNING: Option missing 'optionText', skipping.");
                            continue;
                        }
                        
                        var optionText = optionTextElement.GetString() ?? "";
                        if (string.IsNullOrWhiteSpace(optionText))
                        {
                            Console.WriteLine("[Quiz Generation] WARNING: Option text is empty, skipping.");
                            continue;
                        }
                        
                        var isCorrect = opt.TryGetProperty("isCorrect", out var isCorrectElement) 
                            && isCorrectElement.GetBoolean();
                        
                        if (isCorrect) hasCorrectAnswer = true;

                        question.Options.Add(new QuizOption
                        {
                            QuizQuestionId = 0, // Will be set after save
                            OptionText = optionText,
                            IsCorrect = isCorrect,
                            Order = optionOrder++
                        });
                    }
                    
                    // Validate question has at least 2 options and one correct answer
                    if (question.Options.Count < 2)
                    {
                        Console.WriteLine($"[Quiz Generation] WARNING: Question has less than 2 options, skipping: {questionText}");
                        continue;
                    }
                    
                    if (!hasCorrectAnswer)
                    {
                        Console.WriteLine($"[Quiz Generation] WARNING: Question has no correct answer, marking first option as correct: {questionText}");
                        question.Options[0].IsCorrect = true;
                    }

                    quiz.Questions.Add(question);
                }
                
                if (quiz.Questions.Count == 0)
                {
                    throw new InvalidOperationException("No valid questions could be parsed from AI response.");
                }
                
                Console.WriteLine($"[Quiz Generation] Successfully parsed {quiz.Questions.Count} questions from AI response.");
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"[Quiz Generation] JSON parsing error: {jsonEx.Message}");
                Console.WriteLine($"[Quiz Generation] AI Response that failed: {aiResponse?.Substring(0, Math.Min(1000, aiResponse?.Length ?? 0))}");
                throw new InvalidOperationException($"Failed to parse AI response as JSON: {jsonEx.Message}. Please check AI provider configuration.", jsonEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Quiz Generation] Error parsing quiz: {ex.Message}");
                Console.WriteLine($"[Quiz Generation] Stack trace: {ex.StackTrace}");
                throw new InvalidOperationException($"Failed to parse quiz from AI response: {ex.Message}", ex);
            }

            return quiz;
        }

        private string ExtractJsonBlock(string reply)
        {
            if (string.IsNullOrWhiteSpace(reply))
                return reply;

            try
            {
                // First, try to extract JSON from markdown code fences
                var fenceMatch = System.Text.RegularExpressions.Regex.Match(reply, @"```(?:json)?\s*(?<json>[\s\S]*?)\s*```", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (fenceMatch.Success)
                {
                    var extracted = fenceMatch.Groups["json"].Value.Trim();
                    Console.WriteLine($"[Quiz Generation] Extracted JSON from code fence (length: {extracted.Length})");
                    return extracted;
                }

                // Try to find JSON object boundaries
                var firstBrace = reply.IndexOf('{');
                var lastBrace = reply.LastIndexOf('}');
                if (firstBrace >= 0 && lastBrace > firstBrace)
                {
                    var extracted = reply.Substring(firstBrace, lastBrace - firstBrace + 1).Trim();
                    Console.WriteLine($"[Quiz Generation] Extracted JSON from text boundaries (length: {extracted.Length})");
                    return extracted;
                }
                
                // If reply looks like it might be JSON already, return it
                if (reply.Trim().StartsWith("{") && reply.Trim().EndsWith("}"))
                {
                    Console.WriteLine($"[Quiz Generation] Using reply as-is (appears to be JSON, length: {reply.Length})");
                    return reply.Trim();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Quiz Generation] Error extracting JSON: {ex.Message}");
            }

            Console.WriteLine($"[Quiz Generation] WARNING: Could not extract JSON, returning reply as-is (length: {reply.Length})");
            return reply;
        }
    }
}

