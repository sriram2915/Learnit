using Learnit.Server.Data;
using Learnit.Server.Models;
using Learnit.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;

namespace Learnit.Server.Controllers
{
    [ApiController]
    [Route("api/ai")]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly IAiProvider _provider;
        private readonly AiContextBuilder _contextBuilder;
        private readonly AppDbContext _db;
        private readonly FriendService _friends;
        private readonly UrlMetadataService _urlMetadata;

        public AiController(
            IAiProvider provider,
            AiContextBuilder contextBuilder,
            AppDbContext db,
            FriendService friends,
            UrlMetadataService urlMetadata)
        {
            _provider = provider;
            _contextBuilder = contextBuilder;
            _db = db;
            _friends = friends;
            _urlMetadata = urlMetadata;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedAccessException("Invalid user token");

            return userId;
        }

        [HttpPost("chat")]
        public async Task<ActionResult<AiChatResponse>> Chat([FromBody] AiChatRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var context = await _contextBuilder.BuildContextAsync(userId, cancellationToken);

            // Enhanced system prompt with better structure and guidance
            var systemPrompt = @"You are Learnit AI, a helpful learning assistant for a self-study course management platform.

Your role:
- Provide actionable, specific advice about courses, scheduling, and progress
- Use course/module names from the user's context when relevant
- Suggest concrete next steps with time estimates
- Be concise but helpful (3-6 bullet points or short paragraphs)
- Format responses clearly with markdown when appropriate

Guidelines:
- If suggesting scheduling, include specific duration and timing
- Reference actual course/module names from the user's context
- Prioritize actionable advice over general statements
- Use friendly, encouraging tone
- If the user asks about progress, reference their actual stats (streaks, hours, completion rates)";

            var history = request.History?.Select(h => new AiMessage(h.Role, h.Content)) ?? Enumerable.Empty<AiMessage>();

            // Enhanced user prompt with better structure
            var prompt = $"User question: {request.Message}\n\nUser's learning context:\n{context}\n\nProvide helpful, actionable advice based on the user's question and their current learning situation.";
            var reply = await _provider.GenerateAsync(systemPrompt, prompt, history, cancellationToken);

            return Ok(new AiChatResponse { Reply = reply });
        }

        [HttpPost("create-course")]
        public async Task<ActionResult<AiCourseGenerateResponse>> CreateCourse([FromBody] AiCourseGenerateRequest request, CancellationToken cancellationToken)
        {
            // Check if this is URL-based course creation
            var isUrlBased = !string.IsNullOrWhiteSpace(request.Url);
            
            var systemPrompt = isUrlBased
                ? @"You are Learnit AI, an expert course planner. Generate a GENERIC course structure based on the provided source URL content.

CRITICAL INSTRUCTIONS FOR URL-BASED COURSES:
1. Use the source title and description EXACTLY as provided - do NOT generate new titles or descriptions
2. Create a GENERIC course structure with simple module names (e.g., ""Module 1"", ""Module 2"", etc.)
3. If sections/chapters are provided, create ONE module per section/chapter with NO submodules
4. If no sections are provided, create 3-5 generic modules
5. Use the source title EXACTLY as provided (do not modify or enhance it)
6. Use the source description EXACTLY as provided (summarize only if >500 chars)
7. Estimate hours based on duration/reading time if provided, otherwise use default values
8. Keep learning objectives GENERIC (e.g., ""Complete the course content"", ""Understand the material"")
9. Respond with ONLY VALID MINIFIED JSON (no prose, no markdown, no code fences, no explanations)

REQUIRED JSON SCHEMA:
{
  ""title"": string (use source title EXACTLY - do NOT modify),
  ""description"": string (use source description EXACTLY - summarize only if >500 chars),
  ""subjectArea"": string (Programming, Data Science, Web Development, Design, Business, Science, Mathematics, Language, or Other),
  ""learningObjectives"": [""Complete the course content"", ""Understand the material"", ""Apply the concepts""] (GENERIC objectives only),
  ""difficulty"": ""Beginner"" | ""Intermediate"" | ""Advanced"",
  ""priority"": ""High"" | ""Medium"" | ""Low"",
  ""totalEstimatedHours"": integer (positive, based on duration if provided, otherwise default to 10),
  ""targetCompletionDate"": ""yyyy-MM-dd"" (default to 4 weeks from now),
  ""notes"": string (optional, can be empty),
  ""modules"": [
    {
      ""title"": string (GENERIC title like ""Module 1"", ""Module 2"" - do NOT use source section titles),
      ""description"": string (GENERIC description like ""Course content""),
      ""estimatedHours"": integer (positive, default to 2-3 hours per module),
      ""subModules"": [] (EMPTY ARRAY - URL-based courses have NO submodules)
    }
  ]
}

HARD RULES FOR URL-BASED COURSES:
1. Use source title and description EXACTLY - do NOT generate new content
2. Create GENERIC module titles (""Module 1"", ""Module 2"", etc.) - do NOT use source section titles
3. Match the number of modules to sections if provided, otherwise create 3-5 generic modules
4. Each module MUST have EMPTY subModules array (no submodules for URL-based courses)
5. estimatedHours are positive integers (default 2-3 per module)
6. If duration provided, totalEstimatedHours = ceil(durationMinutes / 60) with 25% buffer
7. difficulty must be one of: Beginner, Intermediate, Advanced
8. priority must be one of: High, Medium, Low
9. NO markdown, NO commentary, NO URLs in fields — PURE JSON ONLY
10. Keep everything GENERIC - use source metadata as-is without enhancement"
                : @"You are Learnit AI, an expert course planner. Generate a COMPREHENSIVE, DETAILED, and PROFESSIONAL course plan based on the user's learning request.

CRITICAL INSTRUCTIONS:
1. Analyze the user's learning request CAREFULLY - understand their goals, duration preferences, difficulty level, and subject area
2. If the user provides learning goals, use them directly. Otherwise, infer 4-6 specific, actionable learning objectives
3. Create a compelling, descriptive course title that clearly indicates what students will learn
4. Write a detailed course description (2-3 sentences) explaining the course value and what students will achieve
5. Infer subject area from the request (Programming, Data Science, Web Development, Design, Business, Science, Mathematics, Language, or Other)
6. Create 4-8 well-structured modules with SPECIFIC, DESCRIPTIVE titles (e.g., ""Building RESTful APIs with ASP.NET Core"" NOT ""Module 1"")
7. Each module MUST have 2-4 submodules with specific, actionable titles (e.g., ""Implementing CRUD Operations"" NOT ""Basics"")
8. Estimate hours realistically: Beginner courses need more time, Advanced courses can be more intensive
9. If user mentions duration (weeks), calculate totalEstimatedHours = weeks * 10 (assuming 10 hours per week)
10. Make modules progressive - each should build on previous knowledge
11. Include practical, hands-on submodules (projects, exercises, labs) in addition to theory
12. Tailor EVERYTHING to their specific request - NO generic content
13. Respond with ONLY VALID MINIFIED JSON (no prose, no markdown, no code fences, no explanations)

REQUIRED JSON SCHEMA:
{
  ""title"": string (compelling, specific course title),
  ""description"": string (2-3 sentences explaining course value and outcomes),
  ""subjectArea"": string (Programming, Data Science, Web Development, Design, Business, Science, Mathematics, Language, or Other),
  ""learningObjectives"": [4-6 specific, outcome-focused strings like ""Build RESTful APIs"", ""Implement authentication"", ""Deploy to production""],
  ""difficulty"": ""Beginner"" | ""Intermediate"" | ""Advanced"",
  ""priority"": ""High"" | ""Medium"" | ""Low"",
  ""totalEstimatedHours"": integer (positive, realistic based on scope),
  ""targetCompletionDate"": ""yyyy-MM-dd"" (default to 4 weeks from now),
  ""notes"": string (optional, can be empty),
  ""modules"": [
    {
      ""title"": string (SPECIFIC like ""Introduction to React Hooks and State Management"" NOT ""Module 1""),
      ""description"": string (brief explanation of what this module covers),
      ""estimatedHours"": integer (positive, typically 3-8 hours per module),
      ""subModules"": [
        {
          ""title"": string (SPECIFIC like ""Understanding useState and useEffect Hooks"" NOT ""Basics""),
          ""description"": string (optional, what this submodule covers),
          ""estimatedHours"": integer (positive, typically 1-3 hours per submodule)
        }
      ]
    }
  ]
}

HARD RULES:
1. Minimum 4 modules, ideally 5-8 modules for comprehensive courses
2. Each module MUST have 2-4 subModules (no empty arrays)
3. estimatedHours are positive integers (modules: 3-8h, submodules: 1-3h)
4. Module titles MUST be SPECIFIC and DESCRIPTIVE - avoid generic names
5. Submodule titles MUST be SPECIFIC and ACTIONABLE - avoid generic names
6. Include a mix of theory and practice submodules (e.g., ""Understanding X"", ""Building Y"", ""Project: Z"")
7. difficulty must be one of: Beginner, Intermediate, Advanced
8. priority must be one of: High, Medium, Low
9. learningObjectives should be 4-6 specific, measurable outcomes
10. NO markdown, NO commentary, NO URLs in fields — PURE JSON ONLY
11. If user provides subject area, difficulty, or priority, use those values exactly";

            string userPrompt;

            UrlMetadata? extractedMeta = null;
            if (!string.IsNullOrWhiteSpace(request.Url))
            {
                Console.WriteLine($"[URL Analysis] Starting analysis for: {request.Url}");
                extractedMeta = await _urlMetadata.TryGetMetadataAsync(request.Url, cancellationToken);
                
                if (extractedMeta != null)
                {
                    Console.WriteLine($"[URL Analysis] Success! Title: {extractedMeta.Title}, Platform: {extractedMeta.Platform}, Duration: {extractedMeta.DurationMinutes}min, Sections: {extractedMeta.Sections.Count}, Headings: {extractedMeta.Headings.Count}");
                    if (extractedMeta.Sections.Any())
                    {
                        Console.WriteLine($"[URL Analysis] Section titles: {string.Join(" | ", extractedMeta.Sections.Take(5).Select(s => s.Title))}");
                    }
                }
                else
                {
                    Console.WriteLine("[URL Analysis] Failed to extract metadata, falling back to basic parsing");
                }
                
                var hint = request.Hint ?? request.Prompt ?? string.Empty;
                var level = string.IsNullOrWhiteSpace(request.Level) ? "Intermediate" : request.Level;

                var sb = new StringBuilder();
                
                // PRIORITY: User-provided title and description come FIRST for URL-based courses
                if (!string.IsNullOrWhiteSpace(request.Title))
                {
                    sb.AppendLine($"REQUIRED COURSE TITLE: {request.Title}");
                }
                if (!string.IsNullOrWhiteSpace(request.Description))
                {
                    sb.AppendLine($"REQUIRED COURSE DESCRIPTION: {request.Description}");
                }
                
                sb.AppendLine($"Target level: {level}");
                
                if (!string.IsNullOrWhiteSpace(hint))
                {
                    sb.AppendLine($"User's learning goal: {hint}");
                }
                
                if (extractedMeta != null)
                {
                    sb.AppendLine($"--- Source Content Information (use as context) ---");
                    if (!string.IsNullOrWhiteSpace(extractedMeta.Title))
                        sb.AppendLine($"Source title: {extractedMeta.Title}");
                    if (!string.IsNullOrWhiteSpace(extractedMeta.Description))
                        sb.AppendLine($"Source description: {Truncate(extractedMeta.Description, 500)}");
                    if (!string.IsNullOrWhiteSpace(extractedMeta.Author))
                        sb.AppendLine($"Source author: {extractedMeta.Author}");
                    if (extractedMeta.DurationMinutes.HasValue)
                        sb.AppendLine($"Video duration: {extractedMeta.DurationMinutes} minutes");
                    if (extractedMeta.EstimatedReadingMinutes.HasValue)
                        sb.AppendLine($"Estimated reading time: {extractedMeta.EstimatedReadingMinutes} minutes");
                    
                    // Include specific sections/chapters to help AI create detailed modules
                    if (extractedMeta.Sections.Any())
                    {
                        var sectionTitles = extractedMeta.Sections.Select(s => s.Title).Where(t => !string.IsNullOrWhiteSpace(t));
                        if (sectionTitles.Any())
                        {
                            sb.AppendLine($"Content structure (create modules based on these - ONE module per section, NO submodules):");
                            foreach (var sectionTitle in sectionTitles.Take(15))
                            {
                                sb.AppendLine($"  - {sectionTitle}");
                            }
                        }
                    }
                    else if (extractedMeta.Headings.Any())
                    {
                        sb.AppendLine($"Content sections (use as reference): {string.Join(" | ", extractedMeta.Headings.Take(10))}");
                    }
                }
                else
                {
                    // If metadata extraction failed, at least mention the URL
                    sb.AppendLine($"Source URL: {request.Url}");
                }

                userPrompt = sb.ToString();
                Console.WriteLine($"[AI Prompt] User prompt length: {userPrompt.Length} chars");
                Console.WriteLine($"[AI Prompt] URL-based course: {isUrlBased}");
                Console.WriteLine($"[AI Prompt] User title provided: {!string.IsNullOrWhiteSpace(request.Title)}");
                Console.WriteLine($"[AI Prompt] User description provided: {!string.IsNullOrWhiteSpace(request.Description)}");
            }
            else
            {
                // For prompt-only course creation (no URL), use Hint or Prompt
                // Frontend sends 'hint' when user provides a topic/prompt with structured inputs
                var prompt = request.Hint ?? request.Prompt ?? string.Empty;
                
                var sb = new StringBuilder();
                
                // PRIORITY: User-provided title and description come FIRST
                if (!string.IsNullOrWhiteSpace(request.Title))
                {
                    sb.AppendLine($"REQUIRED COURSE TITLE: {request.Title}");
                }
                if (!string.IsNullOrWhiteSpace(request.Description))
                {
                    sb.AppendLine($"REQUIRED COURSE DESCRIPTION: {request.Description}");
                }
                
                if (!string.IsNullOrWhiteSpace(prompt))
                {
                    // User's learning request - this is what they want to learn
                    // The prompt may already include structured information (learning goals, subject, duration, etc.)
                    sb.AppendLine($"What the user wants to learn:");
                    sb.AppendLine($"{prompt}");
                }
                else
                {
                    // If no prompt provided, use a default
                    sb.AppendLine("Create a comprehensive course plan");
                }
                
                // Extract structured information from the prompt if it contains it
                // (The frontend now sends enhanced prompts with learning goals, subject area, duration, etc.)
                // The AI will parse this from the prompt text itself
                
                userPrompt = sb.ToString();
                Console.WriteLine($"[AI Prompt] Prompt-only course creation");
                Console.WriteLine($"[AI Prompt] User input length: {prompt.Length} chars");
                Console.WriteLine($"[AI Prompt] User title provided: {!string.IsNullOrWhiteSpace(request.Title)}");
                Console.WriteLine($"[AI Prompt] User description provided: {!string.IsNullOrWhiteSpace(request.Description)}");
            }

            var reply = await _provider.GenerateAsync(systemPrompt, userPrompt, null, cancellationToken);

            // Temporary diagnostics for client debugging
            Console.WriteLine("[AI raw create-course reply]");
            Console.WriteLine(reply);

            var parsed = TryParseCourseJson(reply) ?? BuildHeuristicCourse(userPrompt);
            
            // Check if this is YouTube - if so, return error (YouTube should use separate endpoint)
            var isYouTube = extractedMeta != null && 
                (extractedMeta.Platform == "YouTube" || extractedMeta.Platform == "YouTube Playlist");
            
            if (isYouTube)
            {
                return BadRequest(new { message = "YouTube courses should be created using /api/youtube/create-course endpoint. This endpoint is for AI-generated courses only." });
            }
            
            // Normalize AI-generated course (no YouTube logic)
            var normalized = NormalizeCourse(parsed, extractedMeta, request.Title, request.Description, isUrlBased);

            Console.WriteLine("[AI parsed course]");
            
            // Create a clean copy of the response to prevent any potential circular references
            var cleanResponse = new AiCourseGenerateResponse
            {
                Title = normalized.Title ?? "",
                Description = normalized.Description ?? "",
                SubjectArea = normalized.SubjectArea ?? "",
                LearningObjectives = normalized.LearningObjectives ?? "",
                Difficulty = normalized.Difficulty ?? "Balanced",
                Priority = normalized.Priority ?? "Medium",
                TotalEstimatedHours = normalized.TotalEstimatedHours,
                TargetCompletionDate = normalized.TargetCompletionDate ?? "",
                Notes = normalized.Notes ?? "",
                Modules = (normalized.Modules ?? new List<AiModuleDraft>()).Select(m => new AiModuleDraft
                {
                    Title = m.Title ?? "",
                    Description = m.Description ?? "",
                    EstimatedHours = m.EstimatedHours,
                    Notes = m.Notes ?? "",
                    // For URL-based courses: NO submodules. For AI-only courses: preserve submodules
                    SubModules = isUrlBased 
                        ? new List<AiSubModuleDraft>() // URL-based courses have NO submodules
                        : (m.SubModules ?? new List<AiSubModuleDraft>()).Select(sm => new AiSubModuleDraft
                        {
                            Title = sm.Title ?? "",
                            Description = sm.Description ?? "",
                            EstimatedHours = sm.EstimatedHours
                        }).ToList()
                }).ToList()
            };
            
            // Safe serialization for logging - prevent circular references
            try
            {
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                    MaxDepth = 32,
                    WriteIndented = false
                };
                Console.WriteLine(JsonSerializer.Serialize(cleanResponse, jsonOptions));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI parsed course] Serialization error: {ex.Message}");
            }
            
            return Ok(cleanResponse);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        private static string CleanPromptText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            
            var cleaned = text;
            
            // Remove prompt-like patterns
            cleaned = Regex.Replace(cleaned, @"Target level:\s*[^\n]+\n?", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            cleaned = Regex.Replace(cleaned, @"Source title:\s*", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"Source description:\s*", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"Source author:\s*[^\n]+\n?", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            cleaned = Regex.Replace(cleaned, @"Video duration:\s*[^\n]+\n?", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            cleaned = Regex.Replace(cleaned, @"Source sections:\s*", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"Content chapters:\s*", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"Estimated reading time:\s*[^\n]+\n?", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            
            // Remove simple labels
            cleaned = cleaned.Replace("Topic:", "", StringComparison.OrdinalIgnoreCase)
                .Replace("SourceUrl:", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Source:", "", StringComparison.OrdinalIgnoreCase)
                .Replace("URL:", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Hint:", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Task:", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Instruction:", "", StringComparison.OrdinalIgnoreCase);
            
            // Remove pipe-separated section lists that look like prompt data
            cleaned = Regex.Replace(cleaned, @"\|\s*Section \d+[^|]*\|", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"Section \d+[^|]*\|", "", RegexOptions.IgnoreCase);
            
            // Remove URLs
            cleaned = Regex.Replace(cleaned, @"https?://\S+", "");
            
            // Clean up whitespace
            cleaned = Regex.Replace(cleaned, @"\s+", " ");
            
            return cleaned.Trim();
        }

        private static string ExtractJsonBlock(string reply)
        {
            // Attempt to pull a JSON object even if the model added prose around it
            try
            {
                var fenceMatch = Regex.Match(reply, "```json\\s*(?<json>{[\\s\\S]*?})\\s*```", RegexOptions.IgnoreCase);
                if (fenceMatch.Success)
                    return fenceMatch.Groups["json"].Value;

                var firstBrace = reply.IndexOf('{');
                var lastBrace = reply.LastIndexOf('}');
                if (firstBrace >= 0 && lastBrace > firstBrace)
                    return reply.Substring(firstBrace, lastBrace - firstBrace + 1);
            }
            catch
            {
                // ignore and fall through
            }

            return reply;
        }

        private static AiCourseGenerateResponse? TryParseCourseJson(string reply)
        {
            try
            {
                var json = RepairJson(ExtractJsonBlock(reply));
                using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                });

                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    root = root[0];
                }

                var resp = new AiCourseGenerateResponse
                {
                    Title = GetString(root, "title"),
                    Description = GetString(root, "description"),
                    SubjectArea = GetString(root, "subjectArea", "subject_area"),
                    LearningObjectives = ParseLearningObjectives(root),
                    Difficulty = GetString(root, "difficulty"),
                    Priority = GetString(root, "priority"),
                    TotalEstimatedHours = ParseHours(root, 0, "totalEstimatedHours", "total_estimated_hours", "hours"),
                    TargetCompletionDate = GetString(root, "targetCompletionDate", "target_completion_date"),
                    Notes = GetString(root, "notes"),
                    Modules = new List<AiModuleDraft>()
                };

                var modules = GetProperty(root, "modules");
                if (modules?.ValueKind == JsonValueKind.Array)
                {
                    foreach (var m in modules.Value.EnumerateArray())
                    {
                        var moduleDraft = new AiModuleDraft
                        {
                            Title = GetString(m, "title", "Module"),
                            Description = GetString(m, "description"),
                            EstimatedHours = ParseHours(m, 0, "estimatedHours", "estimated_hours", "hours"),
                            SubModules = new List<AiSubModuleDraft>()
                        };

                        var subs = GetProperty(m, "subModules") ?? GetProperty(m, "submodules") ?? GetProperty(m, "sub_modules");
                        if (subs?.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var s in subs.Value.EnumerateArray())
                            {
                                moduleDraft.SubModules.Add(new AiSubModuleDraft
                                {
                                    Title = GetString(s, "title", "Submodule"),
                                    EstimatedHours = ParseHours(s, 0, "estimatedHours", "estimated_hours", "hours"),
                                    Description = GetString(s, "description"),
                                });
                            }
                        }

                        resp.Modules.Add(moduleDraft);
                    }
                }

                if (resp.Modules.Count == 0)
                {
                    resp.Modules.Add(new AiModuleDraft { Title = "Module 1", EstimatedHours = 2 });
                }

                return resp;
            }
            catch
            {
                return null;
            }
        }

        private static JsonElement? GetProperty(JsonElement element, string name)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return prop.Value;
                }
            }

            return null;
        }

        private static string GetString(JsonElement element, string name, string fallback = "")
        {
            // Call the overload that accepts multiple property names
            return GetString(element, fallback, new[] { name });
        }

        private static string GetString(JsonElement element, string fallback, params string[] names)
        {
            foreach (var name in names)
            {
                var prop = GetProperty(element, name);
                if (prop is null) continue;

                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    var val = prop.Value.GetString();
                    if (!string.IsNullOrEmpty(val)) return val;
                }
            }

            return fallback;
        }

        private static string RepairJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return json;

            // Fill missing estimatedHours with 1 when value is absent
            var filledHours = Regex.Replace(
                json,
                @"""estimatedHours""\s*:\s*(?=[}\]]|$)",
                @"""estimatedHours"":1",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            // Trim trailing commas before a closing brace/bracket
            var cleaned = Regex.Replace(filledHours, @",\s*(?=[}\]])", string.Empty);

            int openObj = cleaned.Count(c => c == '{');
            int closeObj = cleaned.Count(c => c == '}');
            int openArr = cleaned.Count(c => c == '[');
            int closeArr = cleaned.Count(c => c == ']');

            var sb = new System.Text.StringBuilder(cleaned);
            for (int i = 0; i < openObj - closeObj; i++) sb.Append('}');
            for (int i = 0; i < openArr - closeArr; i++) sb.Append(']');

            return sb.ToString();
        }

        private static string ParseLearningObjectives(JsonElement root)
        {
            var goalsProp = GetProperty(root, "learningObjectives");
            if (goalsProp is null) return string.Empty;

            var goals = goalsProp.Value;
            if (goals.ValueKind == JsonValueKind.Array)
            {
                var items = goals
                    .EnumerateArray()
                    .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : null)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim())
                    .ToArray();

                return string.Join("; ", items);
            }

            return goals.GetString() ?? string.Empty;
        }

        private static int ParseHours(JsonElement element, int fallback, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                var propNullable = GetProperty(element, name);
                if (propNullable is null) continue;
                var prop = propNullable.Value;

                try
                {
                    var value = prop.ValueKind switch
                    {
                        JsonValueKind.Number when prop.TryGetInt32(out var i) => i,
                        JsonValueKind.Number when prop.TryGetDouble(out var d) => (int)Math.Round(d),
                        JsonValueKind.String when int.TryParse(prop.GetString(), out var s) => s,
                        _ => (int?)null
                    };

                    if (value.HasValue) return value.Value;
                }
                catch
                {
                    // continue trying other names
                }
            }

            return fallback;
        }

        private static AiCourseGenerateResponse NormalizeCourse(AiCourseGenerateResponse resp, UrlMetadata? metadata = null, string? userTitle = null, string? userDescription = null, bool isUrlBased = false)
        {
            // PRIORITY 1: Use user-provided title/description if available (highest priority)
            if (!string.IsNullOrWhiteSpace(userTitle))
            {
                resp.Title = userTitle.Trim();
            }
            else if (metadata != null)
            {
                // Use actual title if AI didn't provide a good one
                if (string.IsNullOrWhiteSpace(resp.Title) || resp.Title == "Course Plan" || resp.Title.Length < 5)
                {
                    if (!string.IsNullOrWhiteSpace(metadata.Title))
                        resp.Title = metadata.Title;
                }
            }

            // PRIORITY 1: Use user-provided description if available (highest priority)
            if (!string.IsNullOrWhiteSpace(userDescription))
            {
                resp.Description = userDescription.Trim();
            }
            else if (metadata != null)
            {
                // Use actual description if available
                if (string.IsNullOrWhiteSpace(resp.Description) && !string.IsNullOrWhiteSpace(metadata.Description))
                {
                    resp.Description = metadata.Description.Length > 500 
                        ? metadata.Description.Substring(0, 500) + "..." 
                        : metadata.Description;
                }
            }

            // Use metadata to enhance the course if available (non-YouTube only)
            if (metadata != null)
            {
                // Use duration to set total hours if provided
                // For non-video courses: Use 25% buffer for practice/exercises
                if (metadata.DurationMinutes.HasValue && resp.TotalEstimatedHours <= 0)
                {
                    var studyMinutes = metadata.DurationMinutes.Value * 1.25; // 25% buffer
                    resp.TotalEstimatedHours = Math.Max(1, (int)Math.Round(studyMinutes / 60.0));
                }
                else if (metadata.EstimatedReadingMinutes.HasValue && resp.TotalEstimatedHours <= 0)
                {
                    // Reading-based courses: 25% buffer for practice/exercises
                    var studyMinutes = metadata.EstimatedReadingMinutes.Value * 1.25;
                    resp.TotalEstimatedHours = Math.Max(1, (int)Math.Round(studyMinutes / 60.0));
                }

                // For URL-based courses: Don't create modules from metadata - use AI-generated generic modules only
                // For non-URL courses: Only create from metadata if AI didn't generate modules
                if (!isUrlBased && (resp.Modules == null || !resp.Modules.Any() || resp.Modules.Count < 3))
                {
                    // Use sections if available (for non-URL courses that need fallback)
                    if (metadata.Sections.Any())
                    {
                            Console.WriteLine($"[NormalizeCourse] Using {metadata.Sections.Count} sections from {metadata.Platform} to create modules");
                            var sections = metadata.Sections;
                            var totalMinutes = metadata.DurationMinutes ?? (sections.Count * 30);
                            
                            // Non-video courses: Use 25% buffer and per-section calculation
                            var totalStudyMinutes = totalMinutes * 1.25;
                            var totalStudyHours = totalStudyMinutes / 60.0;
                            
                            // Calculate section durations first
                            var sectionDurations = sections.Select(s => s.EstimatedMinutes ?? (totalMinutes / sections.Count)).ToList();
                            var totalSectionMinutes = sectionDurations.Sum();
                            
                            var sectionModules = sections.Select((section, idx) => 
                            {
                                var sectionMinutes = sectionDurations[idx];
                                // Non-video courses: Per-section calculation with 25% buffer
                                var sectionStudyMinutes = sectionMinutes * 1.25;
                                var sectionStudyHours = Math.Max(0.1, sectionStudyMinutes / 60.0);
                                
                                return new AiModuleDraft
                                {
                                    Title = CleanPromptText(section.Title),
                                    Description = $"Module {idx + 1}: {CleanPromptText(section.Title)}",
                                    EstimatedHours = Math.Max(1, (int)Math.Ceiling(sectionStudyHours)),
                                    SubModules = new List<AiSubModuleDraft>
                                    {
                                        new() { 
                                            Title = "Learn", 
                                            EstimatedHours = Math.Max(1, (int)Math.Ceiling(sectionStudyHours * 0.6)), 
                                            Description = "Content and explanations" 
                                        },
                                        new() { 
                                            Title = "Practice", 
                                            EstimatedHours = Math.Max(1, (int)Math.Ceiling(sectionStudyHours * 0.4)), 
                                            Description = "Exercises and review" 
                                        }
                                    }
                                };
                            }).ToList();
                            
                            resp.Modules = sectionModules;
                            // Non-video courses: Sum module hours (includes practice/exercises)
                            resp.TotalEstimatedHours = Math.Max(1, resp.Modules.Sum(m => m.EstimatedHours));
                            Console.WriteLine($"[NormalizeCourse] Created {resp.Modules.Count} modules from sections with total {resp.TotalEstimatedHours} hours");
                    }
                    else if (metadata.Headings.Any())
                    {
                        // Use headings as modules (fallback when sections aren't available)
                        Console.WriteLine($"[NormalizeCourse] No sections found, using {metadata.Headings.Count} headings as modules");
                        var headingCount = Math.Min(metadata.Headings.Count, 20); // Limit to 20 modules
                        var totalMinutes = metadata.DurationMinutes ?? (headingCount * 30); // Default 30 min per heading
                        
                        // For NON-VIDEO: 25% buffer
                        var buffer = 1.25;
                        var totalStudyMinutes = totalMinutes * buffer;
                        var totalStudyHours = totalStudyMinutes / 60.0;
                        var hoursPerModule = Math.Max(1, (int)Math.Round(totalStudyHours / headingCount));
                        
                        var headingModules = metadata.Headings.Take(headingCount).Select((heading, idx) => new AiModuleDraft
                        {
                            Title = CleanPromptText(heading),
                            Description = $"Module {idx + 1}: {CleanPromptText(heading)}",
                            EstimatedHours = Math.Max(1, hoursPerModule),
                            SubModules = new List<AiSubModuleDraft>
                            {
                                new() { Title = "Overview", EstimatedHours = Math.Max(1, hoursPerModule / 2), Description = "Introduction and overview" },
                                new() { Title = "Deep Dive", EstimatedHours = Math.Max(1, hoursPerModule / 2), Description = "Detailed content and practice" }
                            }
                        }).ToList();
                        resp.Modules = headingModules;
                        // Non-video courses: Sum module hours (includes practice/exercises)
                        resp.TotalEstimatedHours = Math.Max(1, resp.Modules.Sum(m => m.EstimatedHours));
                        Console.WriteLine($"[NormalizeCourse] Created {resp.Modules.Count} modules from headings with total {resp.TotalEstimatedHours} hours");
                    }
                }
            }

            // Clean prompt-like text from AI responses (but preserve user-provided values)
            if (string.IsNullOrWhiteSpace(userTitle))
            {
                resp.Title = CleanPromptText(string.IsNullOrWhiteSpace(resp.Title) ? "Course Plan" : resp.Title.Trim());
            }
            if (string.IsNullOrWhiteSpace(userDescription))
            {
                resp.Description = CleanPromptText(resp.Description?.Trim() ?? string.Empty);
            }
            resp.SubjectArea = CleanPromptText(resp.SubjectArea?.Trim() ?? string.Empty);
            resp.Difficulty = string.IsNullOrWhiteSpace(resp.Difficulty) ? "Balanced" : resp.Difficulty.Trim();
            resp.Priority = string.IsNullOrWhiteSpace(resp.Priority) ? "Medium" : resp.Priority.Trim();
            resp.Notes = resp.Notes?.Trim() ?? string.Empty;

            if (resp.Modules == null)
            {
                resp.Modules = new List<AiModuleDraft>();
            }

            if (!resp.Modules.Any())
            {
                // For URL-based courses: Add default module with NO submodules
                // For AI-only courses: Add default module with submodules
                resp.Modules.Add(new AiModuleDraft
                {
                    Title = "Module 1",
                    Description = "Getting started",
                    EstimatedHours = resp.TotalEstimatedHours > 0 ? resp.TotalEstimatedHours : 4,
                    SubModules = isUrlBased 
                        ? new List<AiSubModuleDraft>() // URL-based: NO submodules
                        : new List<AiSubModuleDraft>
                        {
                            new() { Title = "Lesson 1", EstimatedHours = 2, Description = "Overview" },
                            new() { Title = "Lesson 2", EstimatedHours = 2, Description = "Practice" }
                        }
                });
            }

            // Process modules
            foreach (var module in resp.Modules)
            {
                // Only clean title if it's empty, otherwise preserve it
                if (string.IsNullOrWhiteSpace(module.Title))
                {
                    module.Title = CleanPromptText("Module");
                }
                else
                {
                    // Clean but preserve the title if it has content
                    var cleaned = CleanPromptText(module.Title.Trim());
                    if (!string.IsNullOrWhiteSpace(cleaned))
                    {
                        module.Title = cleaned;
                    }
                }
                
                module.Description = CleanPromptText(module.Description?.Trim() ?? string.Empty);
                module.EstimatedHours = module.EstimatedHours <= 0 ? 1 : module.EstimatedHours;

                if (module.SubModules == null)
                {
                    module.SubModules = new List<AiSubModuleDraft>();
                }

                // For URL-based courses: ALWAYS ensure empty submodules
                // For AI-only courses: Add default submodules if none exist
                if (isUrlBased)
                {
                    module.SubModules = new List<AiSubModuleDraft>(); // Force empty for URL-based
                }
                else if (!module.SubModules.Any())
                {
                    module.SubModules.Add(new AiSubModuleDraft
                    {
                        Title = "Submodule",
                        EstimatedHours = Math.Max(1, module.EstimatedHours / 2),
                        Description = ""
                    });
                }

                foreach (var sub in module.SubModules)
                {
                    sub.Title = CleanPromptText(string.IsNullOrWhiteSpace(sub.Title) ? "Submodule" : sub.Title.Trim());
                    sub.Description = CleanPromptText(sub.Description?.Trim() ?? string.Empty);
                    sub.EstimatedHours = sub.EstimatedHours <= 0 ? 1 : sub.EstimatedHours;
                }
            }

            if (resp.TotalEstimatedHours <= 0)
            {
                resp.TotalEstimatedHours = resp.Modules.Sum(m => Math.Max(1, m.EstimatedHours));
            }

            if (string.IsNullOrWhiteSpace(resp.LearningObjectives))
            {
                resp.LearningObjectives = string.Join("; ", resp.Modules.Take(3).Select(m => $"Complete {m.Title}"));
            }
            else
            {
                resp.LearningObjectives = CleanPromptText(resp.LearningObjectives);
            }

            if (string.IsNullOrWhiteSpace(resp.TargetCompletionDate))
            {
                resp.TargetCompletionDate = DateTime.UtcNow.AddDays(28).ToString("yyyy-MM-dd");
            }

            return resp;
        }

        private static AiCourseGenerateResponse BuildHeuristicCourse(string prompt)
        {
            // Stronger heuristic when LLM output is unusable or stubbed
            var trimmed = (prompt ?? string.Empty).Trim();
            var subject = DeriveSubject(trimmed);
            var weeks = ExtractWeeks(trimmed);
            var now = DateTime.UtcNow;
            var targetDate = now.AddDays(Math.Max(21, weeks * 7)).ToString("yyyy-MM-dd");

            var totalHours = weeks > 0 ? Math.Max(12, weeks * 6) : 24;
            var modules = weeks > 0
                ? BuildWeekModules(subject, weeks, totalHours)
                : BuildDefaultModules(subject, totalHours);

            var goals = new[]
            {
                $"Understand the fundamentals of {subject}",
                $"Apply {subject} in guided exercises",
                $"Build and ship a small {subject} project"
            };

            return new AiCourseGenerateResponse
            {
                Title = DeriveTitle(trimmed),
                Description = string.IsNullOrWhiteSpace(trimmed)
                    ? "Auto-generated plan based on your request."
                    : trimmed,
                SubjectArea = subject,
                LearningObjectives = string.Join("; ", goals),
                Difficulty = "Balanced",
                Priority = "Medium",
                TotalEstimatedHours = totalHours,
                TargetCompletionDate = targetDate,
                Notes = "Heuristic plan generated because AI response was not structured JSON.",
                Modules = modules
            };
        }

        private static string DeriveTitle(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return "Tailored Course Plan";
            var words = prompt.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(12);
            var title = string.Join(' ', words);
            return title.Length > 4 ? title : "Tailored Course Plan";
        }

        private static string DeriveSubject(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return "Custom Course";

            var lower = prompt.ToLowerInvariant();
            if (lower.Contains("node.js") || lower.Contains("nodejs") || lower.Contains("node js") || lower.Contains("node"))
                return "Node.js";

            var cleaned = Regex.Replace(prompt, "\\b\\d+\\s*-?\\s*week(s)?\\b", string.Empty, RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, "\\b(course|learn|learning|about|basics|foundation|foundations)\\b", "", RegexOptions.IgnoreCase);
            var tokens = cleaned
                .Split(new[] { ' ', ',', ';', '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(6)
                .ToArray();

            var subject = tokens.Length == 0 ? "Custom Course" : string.Join(" ", tokens);
            return subject.Trim();
        }

        private static int ExtractWeeks(string prompt)
        {
            var match = Regex.Match(prompt ?? string.Empty, "(?<num>\\d+)\\s*-?\\s*week", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups["num"].Value, out var weeks) && weeks > 0 && weeks <= 52)
            {
                return weeks;
            }
            return 0;
        }

        private static List<AiModuleDraft> BuildWeekModules(string subject, int weeks, int totalHours)
        {
            var modules = new List<AiModuleDraft>();
            var hoursPerWeek = DistributeHours(totalHours, weeks);

            for (int i = 0; i < weeks; i++)
            {
                var weekNumber = i + 1;
                var hours = hoursPerWeek[i];
                modules.Add(new AiModuleDraft
                {
                    Title = $"Week {weekNumber}: {subject}",
                    Description = weekNumber == weeks
                        ? "Capstone and consolidation"
                        : "Concepts and practice",
                    EstimatedHours = hours,
                    SubModules = new List<AiSubModuleDraft>
                    {
                        new() { Title = "Concepts", EstimatedHours = Math.Max(1, hours / 3), Description = "Key topics" },
                        new() { Title = "Hands-on", EstimatedHours = Math.Max(1, hours / 3), Description = "Guided exercises" },
                        new() { Title = "Review / Project", EstimatedHours = Math.Max(1, hours - 2 * Math.Max(1, hours / 3)), Description = "Apply and reflect" }
                    }
                });
            }

            return modules;
        }

        private static List<AiModuleDraft> BuildDefaultModules(string subject, int totalHours)
        {
            var modules = new List<AiModuleDraft>();
            var hoursPerModule = DistributeHours(totalHours, 3);

            modules.Add(new AiModuleDraft
            {
                Title = $"Foundations: {subject}",
                Description = "Key concepts, terminology, and setup.",
                EstimatedHours = hoursPerModule[0],
                SubModules = new List<AiSubModuleDraft>
                {
                    new() { Title = "Basics", EstimatedHours = Math.Max(1, hoursPerModule[0] / 3), Description = "Core principles" },
                    new() { Title = "Setup", EstimatedHours = Math.Max(1, hoursPerModule[0] / 3), Description = "Environment and tooling" },
                    new() { Title = "First steps", EstimatedHours = Math.Max(1, hoursPerModule[0] - 2 * Math.Max(1, hoursPerModule[0] / 3)), Description = "Hello world" }
                }
            });

            modules.Add(new AiModuleDraft
            {
                Title = $"Practice: {subject}",
                Description = "Apply skills with guided exercises.",
                EstimatedHours = hoursPerModule[1],
                SubModules = new List<AiSubModuleDraft>
                {
                    new() { Title = "Core exercises", EstimatedHours = Math.Max(1, hoursPerModule[1] / 3), Description = "Hands-on drills" },
                    new() { Title = "Patterns", EstimatedHours = Math.Max(1, hoursPerModule[1] / 3), Description = "Common approaches" },
                    new() { Title = "Review", EstimatedHours = Math.Max(1, hoursPerModule[1] - 2 * Math.Max(1, hoursPerModule[1] / 3)), Description = "Checkpoint" }
                }
            });

            modules.Add(new AiModuleDraft
            {
                Title = $"Project: {subject}",
                Description = "Build a small project to consolidate learning.",
                EstimatedHours = hoursPerModule[2],
                SubModules = new List<AiSubModuleDraft>
                {
                    new() { Title = "Plan", EstimatedHours = Math.Max(1, hoursPerModule[2] / 4), Description = "Define scope" },
                    new() { Title = "Build", EstimatedHours = Math.Max(1, hoursPerModule[2] / 2), Description = "Implement" },
                    new() { Title = "Polish", EstimatedHours = Math.Max(1, hoursPerModule[2] - Math.Max(1, hoursPerModule[2] / 4) - Math.Max(1, hoursPerModule[2] / 2)), Description = "Test and refine" }
                }
            });

            return modules;
        }

        private static int[] DistributeHours(int total, int buckets)
        {
            var safeTotal = Math.Max(total, buckets);
            var baseValue = safeTotal / buckets;
            var remainder = safeTotal % buckets;
            var arr = new int[buckets];
            for (int i = 0; i < buckets; i++)
            {
                arr[i] = baseValue + (i < remainder ? 1 : 0);
                if (arr[i] <= 0) arr[i] = 1;
            }
            return arr;
        }

        [HttpPost("schedule-insights")]
        public async Task<ActionResult<AiInsightResponse>> ScheduleInsights([FromBody] AiInsightRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var context = await _contextBuilder.BuildContextAsync(userId, cancellationToken);
            var systemPrompt = "Provide 3 concise scheduling suggestions. Each should include duration and target module. Return bullet text.";
            var reply = await _provider.GenerateAsync(systemPrompt, request.Prompt + "\nContext:\n" + context, null, cancellationToken);
            return Ok(new AiInsightResponse
            {
                Insights = new List<AiInsight>
                {
                    new() { Title = string.Empty, Detail = reply }
                }
            });
        }

        [HttpPost("progress-insights")]
        public async Task<ActionResult<AiInsightResponse>> ProgressInsights([FromBody] AiInsightRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var context = await _contextBuilder.BuildContextAsync(userId, cancellationToken);
            var systemPrompt = "Provide 3 concise progress insights and next best actions. Keep each under 140 chars.";
            var reply = await _provider.GenerateAsync(systemPrompt, request.Prompt + "\nContext:\n" + context, null, cancellationToken);
            return Ok(new AiInsightResponse
            {
                Insights = new List<AiInsight>
                {
                    new() { Title = string.Empty, Detail = reply }
                }
            });
        }

        // Helper methods for YouTube URL extraction
        private static string? ExtractYouTubeVideoId(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(url, @"(?:v=|\/embed\/|youtu\.be\/)([\w-]{11})");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string? ExtractYouTubePlaylistId(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(url, @"[?&]list=([a-zA-Z0-9_-]+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        [HttpPost("compare")]
        public async Task<ActionResult<FriendCompareResponse>> Compare([FromBody] FriendCompareRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var context = await _contextBuilder.BuildContextAsync(userId, cancellationToken);

            var selected = await _friends.GetFriendsByIdsAsync(userId, request.FriendIds.Take(2), cancellationToken);
            if (selected.Count == 0)
            {
                return Ok(new FriendCompareResponse
                {
                    Friends = new List<FriendDto>(),
                    Insights = new List<AiInsight>
                    {
                        new() { Title = "", Detail = "Pick one friend to compare." }
                    }
                });
            }

            var friend = selected.First();
            var friendsSummary = $"Friend {friend.DisplayName}: {friend.CompletionRate}% done, {friend.WeeklyHours}h/wk";
            var systemPrompt = "You are Learnit AI. Compare the current user versus one friend as a benchmark. Prioritize the user. Provide: (1) a short comparison of completion % and weekly hours (user vs friend), (2) 3 actionable next steps for the user. Do NOT use tables or markdown tables; use bullets or short paragraphs only. Keep it friendly and concise.";
            var reply = await _provider.GenerateAsync(systemPrompt, "Friend: " + friendsSummary + "\nContext (user):\n" + context, null, cancellationToken);

            return Ok(new FriendCompareResponse
            {
                Friends = selected,
                Insights = new List<AiInsight>
                {
                    new() { Title = string.Empty, Detail = reply }
                }
            });
        }

        private static List<AiInsight> SplitBullets(string text)
        {
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(l => l.Length > 0)
                .Take(5)
                .ToList();

            return lines.Select((l, i) => new AiInsight
            {
                Title = $"Idea {i + 1}",
                Detail = l
            }).ToList();
        }
    }
}
