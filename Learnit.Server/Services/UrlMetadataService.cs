using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Learnit.Server.Services
{
    public class UrlMetadata
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Headings { get; set; } = new();
        public int? DurationMinutes { get; set; }
        public string Platform { get; set; } = "Website";
        public string Author { get; set; } = string.Empty;
        public List<ContentSection> Sections { get; set; } = new();
        public int? EstimatedReadingMinutes { get; set; }
        public string? ThumbnailUrl { get; set; }
    }

    public class ContentSection
    {
        public string Title { get; set; } = string.Empty;
        public int? StartTimeSeconds { get; set; }
        public int? EstimatedMinutes { get; set; }
    }

    public class UrlMetadataService
    {
        private readonly HttpClient _http;

        public UrlMetadataService(HttpClient http)
        {
            _http = http;
            _http.Timeout = TimeSpan.FromSeconds(15); // Increased from 8 to 15 seconds for better reliability
            if (!_http.DefaultRequestHeaders.UserAgent.Any())
            {
                _http.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("LearnitBot", "1.0"));
            }
        }

        public async Task<UrlMetadata?> TryGetMetadataAsync(string url, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            try
            {
                if (LooksLikePdfUrl(url))
                {
                    var pdf = await TryGetPdfMetadataAsync(url, ct);
                    if (pdf != null) return pdf;
                }

                if (IsYouTube(url))
                {
                    // Check if it's a playlist first
                    if (IsYouTubePlaylist(url))
                    {
                        var playlist = await TryGetYouTubePlaylistMetadataAsync(url, ct);
                        if (playlist != null) return playlist;
                    }
                    
                    // Otherwise treat as single video
                    var yt = await TryGetYouTubeMetadataAsync(url, ct);
                    if (yt != null) return yt;
                }
                else if (IsMedium(url))
                {
                    var medium = await TryGetMediumMetadataAsync(url, ct);
                    if (medium != null) return medium;
                }
                else if (IsDevTo(url))
                {
                    var devto = await TryGetDevToMetadataAsync(url, ct);
                    if (devto != null) return devto;
                }
                else if (IsGitHub(url))
                {
                    var github = await TryGetGitHubMetadataAsync(url, ct);
                    if (github != null) return github;
                }
                else if (IsDocumentationSite(url))
                {
                    var docs = await TryGetDocumentationMetadataAsync(url, ct);
                    if (docs != null) return docs;
                }
                else if (IsUdemy(url))
                {
                    var udemy = await TryGetUdemyMetadataAsync(url, ct);
                    if (udemy != null) return udemy;
                }
                else if (IsCoursera(url))
                {
                    var coursera = await TryGetCourseraMetadataAsync(url, ct);
                    if (coursera != null) return coursera;
                }
                else if (IsKhanAcademy(url))
                {
                    var khan = await TryGetKhanAcademyMetadataAsync(url, ct);
                    if (khan != null) return khan;
                }
                else if (IsEdx(url))
                {
                    var edx = await TryGetEdxMetadataAsync(url, ct);
                    if (edx != null) return edx;
                }

                // Fallback to generic HTML parsing
                return await TryGetHtmlMetadataAsync(url, ct);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[UrlMetadata] HTTP Request Error in TryGetMetadataAsync: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Console.WriteLine($"[UrlMetadata] Request Timeout in TryGetMetadataAsync: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"[UrlMetadata] Request Canceled in TryGetMetadataAsync: {ex.Message}");
                return null;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[UrlMetadata] Network Error in TryGetMetadataAsync: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UrlMetadata] Unexpected Error in TryGetMetadataAsync ({ex.GetType().Name}): {ex.Message}");
                return null;
            }
        }

        private static bool IsYouTube(string url) =>
            url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("youtu.be", StringComparison.OrdinalIgnoreCase);

        private static bool IsMedium(string url) =>
            url.Contains("medium.com", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("towardsdatascience.com", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("freecodecamp.org", StringComparison.OrdinalIgnoreCase);

        private static bool IsDevTo(string url) =>
            url.Contains("dev.to", StringComparison.OrdinalIgnoreCase);

        private static bool IsGitHub(string url) =>
            url.Contains("github.com", StringComparison.OrdinalIgnoreCase);

        private static bool IsDocumentationSite(string url) =>
            url.Contains("docs.", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("documentation", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("readthedocs.io", StringComparison.OrdinalIgnoreCase);

        private static bool IsUdemy(string url) =>
            url.Contains("udemy.com", StringComparison.OrdinalIgnoreCase);

        private static bool IsCoursera(string url) =>
            url.Contains("coursera.org", StringComparison.OrdinalIgnoreCase);

        private static bool IsKhanAcademy(string url) =>
            url.Contains("khanacademy.org", StringComparison.OrdinalIgnoreCase);

        private static bool IsEdx(string url) =>
            url.Contains("edx.org", StringComparison.OrdinalIgnoreCase);

        private static bool LooksLikePdfUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (url.Contains(".pdf", StringComparison.OrdinalIgnoreCase)) return true;
            if (url.Contains("arxiv.org/pdf/", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private async Task<UrlMetadata?> TryGetYouTubeMetadataAsync(string url, CancellationToken ct)
        {
            try
            {
                // Extract video ID
                var videoId = ExtractYouTubeVideoId(url);
                if (string.IsNullOrEmpty(videoId)) return null;

                // Try oEmbed first (fast, reliable)
                var oembedUrl = $"https://www.youtube.com/oembed?url={Uri.EscapeDataString(url)}&format=json";
                using var oembedResp = await _http.GetAsync(oembedUrl, ct);
                if (oembedResp.IsSuccessStatusCode)
                {
                    var oembedJson = await oembedResp.Content.ReadAsStringAsync(ct);
                    using var oembedDoc = JsonDocument.Parse(oembedJson);
                    var root = oembedDoc.RootElement;
                    var title = root.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                    var author = root.TryGetProperty("author_name", out var a) ? a.GetString() ?? "" : "";
                    var thumbnail = root.TryGetProperty("thumbnail_url", out var thumb) ? thumb.GetString() : null;

                    var metadata = new UrlMetadata
                    {
                        Title = title,
                        Description = string.IsNullOrWhiteSpace(author) ? title : $"{title} by {author}",
                        Author = author,
                        Platform = "YouTube",
                        ThumbnailUrl = thumbnail,
                        Headings = new List<string> { title }
                    };

                    // Try to get video page for duration and chapters
                    // Use a shorter timeout (5 seconds) to avoid blocking course creation
                    try
                    {
                        var videoPageUrl = $"https://www.youtube.com/watch?v={videoId}";
                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5)); // 5 second timeout for video page
                        
                        using var pageResp = await _http.GetAsync(videoPageUrl, timeoutCts.Token);
                        if (pageResp.IsSuccessStatusCode)
                        {
                            // CRITICAL: Aggressively limit HTML size to prevent stack overflow
                            // Reduced from 5MB to 1MB for better safety
                            var htmlBytes = await pageResp.Content.ReadAsByteArrayAsync(ct);
                            if (htmlBytes.Length > 1_000_000)
                            {
                                Console.WriteLine($"[YouTube Metadata] HTML too large ({htmlBytes.Length} bytes), truncating to 1MB");
                                // Truncate to 1MB
                                var truncated = new byte[1_000_000];
                                Array.Copy(htmlBytes, truncated, 1_000_000);
                                htmlBytes = truncated;
                            }
                            
                            var html = System.Text.Encoding.UTF8.GetString(htmlBytes);
                            
                            // Extract duration from JSON-LD or player config
                            var duration = ExtractYouTubeDuration(html);
                            if (duration.HasValue)
                                metadata.DurationMinutes = duration.Value;

                            // Extract chapters from video description or structured data
                            var chapters = ExtractYouTubeChapters(html, videoId);
                            Console.WriteLine($"[YouTube Metadata] Extracted {chapters.Count} chapters for video {videoId}");
                            if (chapters.Any())
                            {
                                metadata.Sections = chapters;
                                metadata.Headings = chapters.Select(c => c.Title).ToList();
                                Console.WriteLine($"[YouTube Metadata] SUCCESS - Chapter titles: {string.Join(" | ", chapters.Take(8).Select(c => $"{c.Title} ({c.StartTimeSeconds}s)"))}");
                            }
                            else
                            {
                                Console.WriteLine("[YouTube Metadata] WARNING - No chapters found in description or JSON-LD");
                                Console.WriteLine("[YouTube Metadata] This might mean: 1) Video has no chapters, 2) Description format is different, 3) HTML structure changed");
                                // Even if no chapters, create a single section for the entire video
                                metadata.Sections = new List<ContentSection>
                                {
                                    new ContentSection
                                    {
                                        Title = title,
                                        StartTimeSeconds = 0,
                                        EstimatedMinutes = duration ?? 30
                                    }
                                };
                                Console.WriteLine($"[YouTube Metadata] Created default section for entire video ({duration ?? 30} minutes)");
                            }
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"[YouTube Metadata] HTTP Request Error in chapter extraction: {ex.Message}");
                        // Continue with oEmbed data only
                    }
                    catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                    {
                        Console.WriteLine($"[YouTube Metadata] Request Timeout in chapter extraction: {ex.Message}");
                        // Continue with oEmbed data only
                    }
                    catch (TaskCanceledException ex)
                    {
                        Console.WriteLine($"[YouTube Metadata] Request Canceled in chapter extraction: {ex.Message}");
                        // Continue with oEmbed data only
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($"[YouTube Metadata] Network Error in chapter extraction: {ex.Message}");
                        // Continue with oEmbed data only
                    }
                    catch (StackOverflowException ex)
                    {
                        Console.WriteLine($"[YouTube Metadata] CRITICAL: StackOverflowException in chapter extraction - {ex.Message}");
                        // Continue with oEmbed data only
                    }
                    catch (OutOfMemoryException ex)
                    {
                        Console.WriteLine($"[YouTube Metadata] CRITICAL: OutOfMemoryException in chapter extraction - {ex.Message}");
                        // Continue with oEmbed data only
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[YouTube Metadata] Error extracting chapters ({ex.GetType().Name}): {ex.Message}");
                        // Continue with oEmbed data only
                    }

                    return metadata;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[YouTube Metadata] HTTP Request Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[YouTube Metadata] Inner Exception: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
                }
                return null;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Console.WriteLine($"[YouTube Metadata] Request Timeout: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"[YouTube Metadata] Request Canceled: {ex.Message}");
                return null;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[YouTube Metadata] Network Error: {ex.Message} (Error Code: {ex.SocketErrorCode})");
                return null;
            }
            catch (StackOverflowException ex)
            {
                Console.WriteLine($"[YouTube Metadata] CRITICAL: StackOverflowException - {ex.Message}");
                return null;
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine($"[YouTube Metadata] CRITICAL: OutOfMemoryException - {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YouTube Metadata] Unexpected Error ({ex.GetType().Name}): {ex.Message}");
                // Fall through to HTML parsing
            }

            return null;
        }

        private static bool IsYouTubePlaylist(string url)
        {
            return url.Contains("list=", StringComparison.OrdinalIgnoreCase) ||
                   url.Contains("/playlist", StringComparison.OrdinalIgnoreCase);
        }

        private static string? ExtractYouTubePlaylistId(string url)
        {
            // Extract playlist ID from URL patterns:
            // https://www.youtube.com/playlist?list=PLxxxxx
            // https://www.youtube.com/watch?v=xxxxx&list=PLxxxxx
            var listMatch = Regex.Match(url, @"[?&]list=([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase);
            if (listMatch.Success)
            {
                return listMatch.Groups[1].Value;
            }
            return null;
        }

        private async Task<UrlMetadata?> TryGetYouTubePlaylistMetadataAsync(string url, CancellationToken ct)
        {
            try
            {
                var playlistId = ExtractYouTubePlaylistId(url);
                if (string.IsNullOrEmpty(playlistId))
                    return null;

                Console.WriteLine($"[YouTube Playlist] Extracting playlist: {playlistId}");

                // Fetch playlist page
                var playlistUrl = $"https://www.youtube.com/playlist?list={playlistId}";
                using var resp = await _http.GetAsync(playlistUrl, ct);
                if (!resp.IsSuccessStatusCode)
                    return null;

                var html = await resp.Content.ReadAsStringAsync(ct);
                // CRITICAL: Aggressively limit HTML size to prevent stack overflow
                // Reduce from 2MB to 1MB for better safety
                if (html.Length > 1_000_000)
                {
                    Console.WriteLine($"[YouTube Playlist] HTML too large ({html.Length} bytes), truncating to 1MB");
                    html = html.Substring(0, 1_000_000);
                }

                // Extract playlist title (use safe regex)
                var titleMatch = SafeRegexMatch(html, @"<meta[^>]+property=[""']og:title[""'][^>]*content=[""']([^""]{0,500})[""']", RegexOptions.IgnoreCase, timeoutSeconds: 2);
                var title = titleMatch.Success ? System.Net.WebUtility.HtmlDecode(titleMatch.Groups[1].Value) : "YouTube Playlist";

                // Extract video IDs from the page
                var videoIds = new List<string>();
                var videoTitles = new List<string>();

                // Method 1: Extract from ytInitialData (if available and not too large)
                try
                {
                    // CRITICAL: Limit match size to prevent stack overflow - use bounded quantifier
                    // Reduced from 500KB to 300KB for better safety
                    var ytDataMatch = SafeRegexMatch(html, @"var\s+ytInitialData\s*=\s*({[^;]{0,300000}});", RegexOptions.Singleline, timeoutSeconds: 3);
                    if (ytDataMatch.Success && ytDataMatch.Groups[1].Value.Length < 300_000)
                    {
                        var jsonStr = ytDataMatch.Groups[1].Value;
                        // Use JsonDocumentOptions with MaxDepth limit
                        var options = new JsonDocumentOptions { MaxDepth = 64 };
                        using var doc = JsonDocument.Parse(jsonStr, options);
                        ExtractVideosFromPlaylistJson(doc.RootElement, videoIds, videoTitles);
                    }
                }
                catch (StackOverflowException ex)
                {
                    Console.WriteLine($"[YouTube Playlist] CRITICAL: StackOverflowException in JSON parsing - {ex.Message}");
                    // Continue to fallback method
                }
                catch (OutOfMemoryException ex)
                {
                    Console.WriteLine($"[YouTube Playlist] CRITICAL: OutOfMemoryException in JSON parsing - {ex.Message}");
                    // Continue to fallback method
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[YouTube Playlist] JSON parsing failed: {ex.Message}");
                }

                // Method 2: Fallback - extract video IDs and titles from HTML patterns
                if (videoIds.Count == 0)
                {
                    var videoPatterns = new[]
                    {
                        @"/watch\?v=([a-zA-Z0-9_-]{11})",
                        @"""videoId"":""([a-zA-Z0-9_-]{11})"""
                    };

                    foreach (var pattern in videoPatterns)
                    {
                        // Use safe regex with timeout
                        var matches = SafeRegexMatches(html, pattern, RegexOptions.None, timeoutSeconds: 2);
                        foreach (Match match in matches)
                        {
                            if (match.Groups.Count > 1)
                            {
                                var vid = match.Groups[1].Value;
                                if (!videoIds.Contains(vid))
                                {
                                    videoIds.Add(vid);
                                    
                                    // Try to extract title near this video ID
                                    // Look for title patterns near the video ID in the HTML
                                    var titlePatterns = new[]
                                    {
                                        $@"videoId[""']?\s*:\s*[""']{Regex.Escape(vid)}[""'][^}}]*?title[""']?\s*:\s*{{[^}}]*?runs[""']?\s*:\s*\[[^\\]]*?text[""']?\s*:\s*[""']([^""']{{1,200}})[""']",
                                        $@"{Regex.Escape(vid)}[^""]*?[""']title[""']\s*:\s*{{[^}}]*?text[""']\s*:\s*[""']([^""']{{1,200}})[""']",
                                        $@"videoId[""']?\s*:\s*[""']{Regex.Escape(vid)}[""'][^}}]*?simpleText[""']?\s*:\s*[""']([^""']{{1,200}})[""']"
                                    };
                                    
                                    string? foundTitle = null;
                                    foreach (var titlePattern in titlePatterns)
                                    {
                                        try
                                        {
                                            var videoTitleMatch = SafeRegexMatch(html, titlePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline, timeoutSeconds: 1);
                                            if (videoTitleMatch.Success && videoTitleMatch.Groups.Count > 1)
                                            {
                                                foundTitle = System.Net.WebUtility.HtmlDecode(videoTitleMatch.Groups[1].Value.Trim());
                                                if (!string.IsNullOrWhiteSpace(foundTitle) && foundTitle.Length > 2)
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            // Continue to next pattern
                                        }
                                    }
                                    
                                    videoTitles.Add(foundTitle ?? $"Video {videoIds.Count}");
                                }
                            }
                        }
                        if (videoIds.Count > 0) break;
                    }
                }

                // Limit to first 200 videos to prevent too many modules (increased from 50)
                // This allows for larger playlists while still maintaining reasonable limits
                const int MAX_PLAYLIST_VIDEOS = 200;
                if (videoIds.Count > MAX_PLAYLIST_VIDEOS)
                {
                    videoIds = videoIds.Take(MAX_PLAYLIST_VIDEOS).ToList();
                    if (videoTitles.Count > MAX_PLAYLIST_VIDEOS)
                    {
                        videoTitles = videoTitles.Take(MAX_PLAYLIST_VIDEOS).ToList();
                    }
                    Console.WriteLine($"[YouTube Playlist] Limited to {MAX_PLAYLIST_VIDEOS} videos");
                }

                Console.WriteLine($"[YouTube Playlist] Found {videoIds.Count} videos");

                if (videoIds.Count == 0)
                    return null;

                // For playlists: Each video = Module (sections represent videos)
                // Chapters within each video will be extracted later as SubModules
                var sections = new List<ContentSection>();
                for (int i = 0; i < videoIds.Count; i++)
                {
                    var videoTitle = i < videoTitles.Count && !string.IsNullOrWhiteSpace(videoTitles[i])
                        ? videoTitles[i]
                        : $"Video {i + 1}";

                    sections.Add(new ContentSection
                    {
                        Title = videoTitle,
                        StartTimeSeconds = null,
                        EstimatedMinutes = 15 // Default 15 minutes per video (will be adjusted with 1.25x)
                    });
                }

                return new UrlMetadata
                {
                    Title = title,
                    Description = $"YouTube playlist with {videoIds.Count} videos",
                    Platform = "YouTube Playlist",
                    Sections = sections, // Each section = one video = one module
                    Headings = sections.Select(s => s.Title).ToList(),
                    DurationMinutes = videoIds.Count * 15 // Estimate 15 min per video
                };
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[YouTube Playlist] HTTP Request Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[YouTube Playlist] Inner Exception: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
                }
                return null;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Console.WriteLine($"[YouTube Playlist] Request Timeout: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"[YouTube Playlist] Request Canceled: {ex.Message}");
                return null;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[YouTube Playlist] Network Error: {ex.Message} (Error Code: {ex.SocketErrorCode})");
                return null;
            }
            catch (StackOverflowException ex)
            {
                Console.WriteLine($"[YouTube Playlist] CRITICAL: StackOverflowException caught - {ex.Message}");
                Console.WriteLine("[YouTube Playlist] Returning null to prevent server crash");
                return null;
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine($"[YouTube Playlist] CRITICAL: OutOfMemoryException caught - {ex.Message}");
                Console.WriteLine("[YouTube Playlist] Returning null to prevent server crash");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YouTube Playlist] Error ({ex.GetType().Name}): {ex.Message}");
                return null;
            }
        }

        private static void ExtractVideosFromPlaylistJson(JsonElement root, List<string> videoIds, List<string> videoTitles, int depth = 0)
        {
            // Stricter safety limits to prevent stack overflow
            const int MAX_VIDEOS = 200; // Increased from 50 to support larger playlists
            if (depth > 5) return; // Reduced from 10 to 5
            if (videoIds.Count >= MAX_VIDEOS) return; // Stop once we have enough videos

            try
            {
                if (root.ValueKind == JsonValueKind.Object)
                {
                    // Look for videoRenderer objects first (most common path)
                    if (root.TryGetProperty("videoRenderer", out var videoRenderer))
                    {
                        if (videoRenderer.TryGetProperty("videoId", out var videoId))
                        {
                            var vid = videoId.GetString();
                            if (!string.IsNullOrEmpty(vid) && !videoIds.Contains(vid))
                            {
                                videoIds.Add(vid);
                                
                                // Try to get title - check multiple possible title paths
                                string? videoTitle = null;
                                
                                // Path 1: title.runs[0].text (most common)
                                if (videoRenderer.TryGetProperty("title", out var titleObj))
                                {
                                    if (titleObj.TryGetProperty("runs", out var runs) &&
                                        runs.ValueKind == JsonValueKind.Array &&
                                        runs.GetArrayLength() > 0)
                                    {
                                        var firstRun = runs[0];
                                        if (firstRun.TryGetProperty("text", out var text))
                                        {
                                            videoTitle = text.GetString();
                                        }
                                    }
                                    // Path 2: title.simpleText (fallback)
                                    else if (titleObj.TryGetProperty("simpleText", out var simpleText))
                                    {
                                        videoTitle = simpleText.GetString();
                                    }
                                }
                                
                                // Path 3: Check for title in other locations
                                if (string.IsNullOrWhiteSpace(videoTitle))
                                {
                                    if (videoRenderer.TryGetProperty("headline", out var headline) &&
                                        headline.TryGetProperty("simpleText", out var headlineText))
                                    {
                                        videoTitle = headlineText.GetString();
                                    }
                                }
                                
                                videoTitles.Add(!string.IsNullOrWhiteSpace(videoTitle) ? videoTitle : $"Video {videoIds.Count}");
                                
                                // Early return if we found a video and have enough
                                if (videoIds.Count >= MAX_VIDEOS) return;
                            }
                        }
                    }

                    // Recursively search in nested objects (limited depth and property count)
                    int propCount = 0;
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (propCount++ > 50) break; // Limit properties per object
                        if (videoIds.Count >= MAX_VIDEOS) break; // Stop if we have enough
                        
                        if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            ExtractVideosFromPlaylistJson(prop.Value, videoIds, videoTitles, depth + 1);
                        }
                    }
                }
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    int itemCount = 0;
                    foreach (var item in root.EnumerateArray())
                    {
                        if (itemCount++ > 100) break; // Limit array items
                        if (videoIds.Count >= MAX_VIDEOS) break; // Stop if we have enough
                        ExtractVideosFromPlaylistJson(item, videoIds, videoTitles, depth + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but continue on errors to prevent crashes
                Console.WriteLine($"[YouTube Playlist] Error extracting videos at depth {depth}: {ex.Message}");
            }
        }

        private static string? ExtractYouTubeVideoId(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            
            // Handle youtu.be URLs with query parameters (e.g., ?si=...)
            // Pattern 1: youtu.be/VIDEO_ID?si=... or youtu.be/VIDEO_ID
            var youtuBeMatch = Regex.Match(url, @"youtu\.be/([a-zA-Z0-9_-]{11})", RegexOptions.IgnoreCase);
            if (youtuBeMatch.Success && youtuBeMatch.Groups.Count > 1)
            {
                var videoId = youtuBeMatch.Groups[1].Value;
                Console.WriteLine($"[YouTube Video ID] Extracted from youtu.be: {videoId}");
                return videoId;
            }
            
            // Pattern 2: youtube.com/watch?v=VIDEO_ID&... or youtube.com/watch?v=VIDEO_ID
            var watchMatch = Regex.Match(url, @"youtube\.com/watch\?v=([a-zA-Z0-9_-]{11})", RegexOptions.IgnoreCase);
            if (watchMatch.Success && watchMatch.Groups.Count > 1)
            {
                var videoId = watchMatch.Groups[1].Value;
                Console.WriteLine($"[YouTube Video ID] Extracted from youtube.com/watch: {videoId}");
                return videoId;
            }
            
            // Pattern 3: youtube.com/embed/VIDEO_ID
            var embedMatch = Regex.Match(url, @"youtube\.com/embed/([a-zA-Z0-9_-]{11})", RegexOptions.IgnoreCase);
            if (embedMatch.Success && embedMatch.Groups.Count > 1)
            {
                var videoId = embedMatch.Groups[1].Value;
                Console.WriteLine($"[YouTube Video ID] Extracted from youtube.com/embed: {videoId}");
                return videoId;
            }
            
            Console.WriteLine($"[YouTube Video ID] Failed to extract video ID from URL: {url}");
            return null;
        }

        private static int? ExtractYouTubeDuration(string html)
        {
            // CRITICAL: Limit HTML size before processing
            if (html.Length > 1_000_000) html = html.Substring(0, 1_000_000);
            
            // Try to find duration in JSON-LD structured data (use safe regex)
            var jsonLdMatch = SafeRegexMatch(html, @"<script[^>]*type=[""']application/ld\+json[""'][^>]*>([^<]{0,50000})</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline, timeoutSeconds: 2);
            if (jsonLdMatch.Success)
            {
                try
                {
                    var json = jsonLdMatch.Groups[1].Value;
                    if (json.Length < 50_000) // Only parse if reasonably sized
                    {
                        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32, AllowTrailingCommas = true });
                        var root = doc.RootElement;
                        
                        // Handle array or object
                        var element = root.ValueKind == JsonValueKind.Array ? root[0] : root;
                        
                        if (element.TryGetProperty("duration", out var durationProp))
                        {
                            var durationStr = durationProp.GetString();
                            if (!string.IsNullOrEmpty(durationStr) && TryParseIso8601Duration(durationStr, out var minutes))
                                return minutes;
                        }
                    }
                }
                catch { }
            }

            // Try player config (use safe regex)
            var playerMatch = SafeRegexMatch(html, @"""lengthSeconds""\s*:\s*""?(\d+)""?", RegexOptions.IgnoreCase, timeoutSeconds: 2);
            if (playerMatch.Success && int.TryParse(playerMatch.Groups[1].Value, out var seconds))
            {
                return (int)Math.Ceiling(seconds / 60.0);
            }

            return null;
        }

        private static bool TryParseIso8601Duration(string duration, out int minutes)
        {
            minutes = 0;
            // ISO 8601 format: PT1H2M10S (use safe regex - duration string is small, but still use timeout)
            var match = SafeRegexMatch(duration, @"PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?", RegexOptions.None, timeoutSeconds: 1);
            if (match.Success)
            {
                var hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
                var mins = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
                var secs = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
                minutes = hours * 60 + mins + (int)Math.Ceiling(secs / 60.0);
                return true;
            }
            return false;
        }

        // Helper method for safe regex matching with timeout and error handling
        private static Match SafeRegexMatch(string input, string pattern, RegexOptions options = RegexOptions.None, int timeoutSeconds = 2)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern))
                return Match.Empty;
            
            try
            {
                var regex = new Regex(pattern, options, TimeSpan.FromSeconds(timeoutSeconds));
                return regex.Match(input);
            }
            catch (RegexMatchTimeoutException)
            {
                Console.WriteLine($"[Safe Regex] Pattern timed out after {timeoutSeconds}s: {pattern.Substring(0, Math.Min(50, pattern.Length))}...");
                return Match.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Safe Regex] Error: {ex.Message}");
                return Match.Empty;
            }
        }
        
        // Helper method for safe regex matching collection with timeout and error handling
        private static MatchCollection SafeRegexMatches(string input, string pattern, RegexOptions options = RegexOptions.None, int timeoutSeconds = 2)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern))
            {
                var emptyRegex = new Regex("^$");
                return emptyRegex.Matches("");
            }
            
            try
            {
                var regex = new Regex(pattern, options, TimeSpan.FromSeconds(timeoutSeconds));
                return regex.Matches(input);
            }
            catch (RegexMatchTimeoutException)
            {
                Console.WriteLine($"[Safe Regex] Pattern collection timed out after {timeoutSeconds}s: {pattern.Substring(0, Math.Min(50, pattern.Length))}...");
                var emptyRegex = new Regex("^$");
                return emptyRegex.Matches("");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Safe Regex] Error: {ex.Message}");
                var emptyRegex = new Regex("^$");
                return emptyRegex.Matches("");
            }
        }

        private static List<ContentSection> ExtractYouTubeChapters(string html, string videoId)
        {
            var sections = new List<ContentSection>();
            
            // CRITICAL: Top-level try-catch to prevent any unhandled exceptions from crashing the server
            try
            {
                // CRITICAL: Limit HTML size aggressively to prevent stack overflow
                // YouTube pages can be 5-10MB, but we only need description/chapter data
                const int MAX_HTML_SIZE = 1_000_000; // 1MB max (reduced from 1.5MB for safety)
                
                if (html == null || html.Length == 0)
                {
                    Console.WriteLine("[YouTube Chapters] HTML is null or empty");
                    return sections;
                }
                
                if (html.Length > MAX_HTML_SIZE)
                {
                    Console.WriteLine($"[YouTube Chapters] HTML too large ({html.Length:N0} chars), truncating to {MAX_HTML_SIZE:N0} chars");
                    html = html.Substring(0, MAX_HTML_SIZE);
                }
                
                // Method 1: Try to extract from JSON-LD structured data (most reliable)
                // Use safe regex helper with timeout protection
                var jsonLdPattern = @"<script[^>]*type=[""']application/ld\+json[""'][^>]*>([^<]{0,50000})</script>";
                var jsonLdMatches = SafeRegexMatches(html, jsonLdPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline, timeoutSeconds: 2);
                
                // Limit JSON-LD matches to prevent processing too much
                int jsonMatchCount = 0;
                
                foreach (Match jsonMatch in jsonLdMatches)
                {
                    if (jsonMatchCount++ >= 10) break; // Limit to 10 JSON-LD blocks
                    try
                    {
                        var json = jsonMatch.Groups[1].Value;
                        if (json.Length > 100_000) 
                        {
                            Console.WriteLine($"[YouTube Chapters] Skipping JSON-LD block (too large: {json.Length:N0} chars)");
                            continue; // Skip very large JSON blocks
                        }
                        
                        // CRITICAL: Use JsonDocumentOptions to limit depth and prevent stack overflow
                        try
                        {
                            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
                            {
                                MaxDepth = 64, // Limit JSON depth to prevent deep recursion
                                AllowTrailingCommas = true
                            });
                            var root = doc.RootElement;
                            
                            // Handle array or object
                            var element = root.ValueKind == JsonValueKind.Array ? root[0] : root;
                            
                            // Look for chapters in various possible locations (with safety limit)
                            if (element.TryGetProperty("hasPart", out var hasPart) && hasPart.ValueKind == JsonValueKind.Array)
                            {
                                int count = 0;
                                const int MAX_PARTS = 200; // Limit parts to prevent excessive processing
                                foreach (var part in hasPart.EnumerateArray())
                                {
                                    if (count++ >= MAX_PARTS)
                                    {
                                        Console.WriteLine($"[YouTube Chapters] Limiting to {MAX_PARTS} parts in hasPart array");
                                        break;
                                    }
                                    
                                    if (part.TryGetProperty("name", out var name) && part.TryGetProperty("startOffset", out var offset))
                                    {
                                        var title = name.GetString();
                                        var offsetStr = offset.GetString();
                                        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(offsetStr))
                                        {
                                            if (TryParseTimeOffset(offsetStr, out var seconds))
                                            {
                                                sections.Add(new ContentSection
                                                {
                                                    Title = title,
                                                    StartTimeSeconds = seconds
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[YouTube Chapters] JSON-LD parsing error: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[YouTube Chapters] Error processing JSON-LD match: {ex.Message}");
                    }
                }

                // Method 2: Try to find chapters in player config (var ytInitialData)
                // Use a more limited regex to avoid matching huge JSON structures
                if (sections.Count == 0)
                {
                    try
                    {
                        // Try to find a smaller chunk - look for playerOverlays specifically
                        var playerOverlaysMatch = SafeRegexMatch(html, @"playerOverlays[""']?\s*:\s*({[^}]{0,50000}})", RegexOptions.IgnoreCase | RegexOptions.Singleline, timeoutSeconds: 2);
                        if (playerOverlaysMatch.Success)
                        {
                            var configJson = "{" + playerOverlaysMatch.Groups[1].Value + "}";
                            if (configJson.Length < 100_000) // Only parse if reasonably sized
                            {
                                using var doc = JsonDocument.Parse(configJson);
                                var root = doc.RootElement;
                                if (TryExtractChaptersFromJson(root, out var jsonChapters))
                                {
                                    sections.AddRange(jsonChapters);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[YouTube Chapters] Limited player config parsing failed: {ex.Message}");
                    }
                }

                // Method 3: Try ytInitialPlayerResponse.videoDetails.shortDescription (BEST for chapters)
                // Use a more restrictive pattern to avoid matching huge JSON structures
                if (sections.Count == 0)
                {
                    try
                    {
                        // Look for ytInitialPlayerResponse but limit the match size to prevent stack overflow
                        // Use a more specific pattern that stops at reasonable boundaries
                        var playerResponsePattern = @"ytInitialPlayerResponse\s*=\s*({[^}]*""videoDetails""[^}]*{[^}]*""shortDescription""[^}]*""[^""]{0,50000}""[^}]*}[^}]*})";
                        var playerResponseMatch = SafeRegexMatch(html, playerResponsePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline, timeoutSeconds: 2);
                        
                        if (!playerResponseMatch.Success)
                        {
                            // Try simpler pattern - just find shortDescription value directly (use safe regex)
                            // Increased limit to 100KB for longer descriptions with many chapters
                            var shortDescPattern = @"""shortDescription""\s*:\s*""([^""]{0,100000})""";
                            var shortDescMatch = SafeRegexMatch(html, shortDescPattern, RegexOptions.IgnoreCase, timeoutSeconds: 3);
                            if (shortDescMatch.Success)
                            {
                                var description = shortDescMatch.Groups[1].Value;
                                // Limit description size to prevent issues (increased for longer videos with many chapters)
                                if (description.Length > 100_000) description = description.Substring(0, 100_000);
                                
                                // Unescape JSON string - handle more escape sequences
                                description = description
                                    .Replace("\\n", "\n")
                                    .Replace("\\r", "\r")
                                    .Replace("\\\"", "\"")
                                    .Replace("\\t", "\t")
                                    .Replace("\\\\", "\\");
                                Console.WriteLine($"[YouTube Chapters] Found shortDescription via direct match: {description.Length} chars");
                                Console.WriteLine($"[YouTube Chapters] Description preview (first 1000 chars): {description.Substring(0, Math.Min(1000, description.Length))}");
                                
                                // Extract chapters from description using timestamp patterns (use safe regex with limits)
                                // Use the same comprehensive patterns as Method 4 for consistency
                                var chapterPatterns = new[]
                                {
                                    @"(\d{1,2}):(\d{2}):(\d{2})\s*[-–—•\*]?\s*([^\r\n]{0,200}?)(?=\d{1,2}:\d{2}:\d{2}|\d{1,2}:\d{2}|$|\r|\n)", // HH:MM:SS format with separators
                                    @"(\d{1,2}):(\d{2})\s*[-–—•\*]?\s*([^\r\n]{0,200}?)(?=\d{1,2}:\d{2}|$|\r|\n)", // MM:SS format with separators
                                    @"(\d{1,2}):(\d{2}):(\d{2})\s+([^\r\n]{0,200}?)(?=\d{1,2}:\d{2}:\d{2}|\d{1,2}:\d{2}|$|\r|\n)", // HH:MM:SS format
                                    @"(\d{1,2}):(\d{2})\s+([^\r\n]{0,200}?)(?=\d{1,2}:\d{2}|$|\r|\n)", // MM:SS format
                                    @"(\d+)\.\s*(\d{1,2}):(\d{2}):(\d{2})\s+([^\r\n]{0,200}?)(?=\d+\.\s*\d{1,2}:\d{2}:\d{2}|\d+\.\s*\d{1,2}:\d{2}|$|\r|\n)", // 1. HH:MM:SS Title
                                    @"(\d+)\.\s*(\d{1,2}):(\d{2})\s+([^\r\n]{0,200}?)(?=\d+\.\s*\d{1,2}:\d{2}|$|\r|\n)", // 1. MM:SS Title
                                };
                                
                                foreach (var pattern in chapterPatterns)
                                {
                                    var matches = SafeRegexMatches(description, pattern, RegexOptions.None, timeoutSeconds: 2);
                                    int matchCount = 0;
                                    const int MAX_CHAPTERS = 100;
                                    foreach (Match match in matches)
                                    {
                                        if (matchCount++ >= MAX_CHAPTERS)
                                        {
                                            Console.WriteLine($"[YouTube Chapters] Limiting to {MAX_CHAPTERS} chapters from shortDescription");
                                            break;
                                        }
                                        
                                        if (!match.Success) continue;
                                        
                                        try
                                        {
                                            int totalSeconds = 0;
                                            string title = "";
                                            
                                            // Handle different group counts based on pattern (same logic as Method 4)
                                            // Pattern: (\d+)\.\s*(\d{1,2}):(\d{2}):(\d{2})\s+([^\r\n]{0,200}?) - 6 groups (1. HH:MM:SS Title)
                                            if (match.Groups.Count == 6 && match.Groups[1].Value.Contains("."))
                                            {
                                                var hours = int.Parse(match.Groups[2].Value);
                                                var minutes = int.Parse(match.Groups[3].Value);
                                                var seconds = int.Parse(match.Groups[4].Value);
                                                totalSeconds = hours * 3600 + minutes * 60 + seconds;
                                                title = match.Groups[5].Value.Trim();
                                            }
                                            // Pattern: (\d+)\.\s*(\d{1,2}):(\d{2})\s+([^\r\n]{0,200}?) - 5 groups (1. MM:SS Title)
                                            else if (match.Groups.Count == 5 && match.Groups[1].Value.Contains("."))
                                            {
                                                var minutes = int.Parse(match.Groups[2].Value);
                                                var seconds = int.Parse(match.Groups[3].Value);
                                                totalSeconds = minutes * 60 + seconds;
                                                title = match.Groups[4].Value.Trim();
                                            }
                                            // Pattern: (\d{1,2}):(\d{2}):(\d{2})\s+([^\r\n]{0,200}?) - 5 groups (HH:MM:SS Title)
                                            else if (match.Groups.Count == 5)
                                            {
                                                var hours = int.Parse(match.Groups[1].Value);
                                                var minutes = int.Parse(match.Groups[2].Value);
                                                var seconds = int.Parse(match.Groups[3].Value);
                                                totalSeconds = hours * 3600 + minutes * 60 + seconds;
                                                title = match.Groups[4].Value.Trim();
                                            }
                                            // Pattern: (\d{1,2}):(\d{2})\s+([^\r\n]{0,200}?) - 4 groups (MM:SS Title)
                                            else if (match.Groups.Count == 4)
                                            {
                                                var minutes = int.Parse(match.Groups[1].Value);
                                                var seconds = int.Parse(match.Groups[2].Value);
                                                totalSeconds = minutes * 60 + seconds;
                                                title = match.Groups[3].Value.Trim();
                                            }
                                            else
                                            {
                                                Console.WriteLine($"[YouTube Chapters] Unexpected group count in shortDescription: {match.Groups.Count}, skipping match");
                                                continue;
                                            }
                                            
                                            // Clean up title
                                            title = title.Trim();
                                            if (!string.IsNullOrWhiteSpace(title) && title.Length < 200 && title.Length > 2 && !title.StartsWith("http"))
                                            {
                                                sections.Add(new ContentSection
                                                {
                                                    Title = title,
                                                    StartTimeSeconds = totalSeconds
                                                });
                                                Console.WriteLine($"[YouTube Chapters] Added chapter from shortDescription: {title} at {totalSeconds}s");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"[YouTube Chapters] Error parsing timestamp in shortDescription: {ex.Message}");
                                        }
                                    }
                                    if (sections.Count > 0)
                                    {
                                        Console.WriteLine($"[YouTube Chapters] Found {sections.Count} chapters from shortDescription using pattern: {pattern.Substring(0, Math.Min(80, pattern.Length))}...");
                                        Console.WriteLine($"[YouTube Chapters] First 3 chapters: {string.Join(" | ", sections.Take(3).Select(s => $"{s.Title} ({s.StartTimeSeconds}s)"))}");
                                        break;
                                    }
                                    else if (matches.Count > 0)
                                    {
                                        Console.WriteLine($"[YouTube Chapters] Pattern matched {matches.Count} times in shortDescription but no valid chapters extracted");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[YouTube Chapters] ytInitialPlayerResponse parsing failed: {ex.Message}");
                    }
                }

                // Method 4: Extract from description with timestamp patterns (fallback)
                // This is the most reliable method and doesn't cause stack overflow
                if (sections.Count == 0)
                {
                    // Try multiple timestamp patterns (more flexible to catch various formats)
                    // CRITICAL: Use non-greedy with limits to prevent catastrophic backtracking
                    // Added more patterns to catch different YouTube chapter formats
                    var patterns = new[]
                    {
                        // Standard formats with separators
                        @"(\d{1,2}):(\d{2}):(\d{2})\s*[-–—•\*]?\s*([^\r\n]{0,200}?)(?=\d{1,2}:\d{2}:\d{2}|\d{1,2}:\d{2}|$|\r|\n)", // HH:MM:SS format with separators
                        @"(\d{1,2}):(\d{2})\s*[-–—•\*]?\s*([^\r\n]{0,200}?)(?=\d{1,2}:\d{2}|$|\r|\n)", // MM:SS format with separators
                        // Standard formats without separators
                        @"(\d{1,2}):(\d{2}):(\d{2})\s+([^\r\n]{0,200}?)(?=\d{1,2}:\d{2}:\d{2}|\d{1,2}:\d{2}|$|\r|\n)", // HH:MM:SS format
                        @"(\d{1,2}):(\d{2})\s+([^\r\n]{0,200}?)(?=\d{1,2}:\d{2}|$|\r|\n)", // MM:SS format
                        // Formats with parentheses or brackets
                        @"(\d{1,2}):(\d{2}):(\d{2})\s*[\(\[].*?[\)\]]\s*([^\r\n]{0,200}?)(?=\d{1,2}:\d{2}:\d{2}|\d{1,2}:\d{2}|$|\r|\n)", // HH:MM:SS (Title) format
                        @"(\d{1,2}):(\d{2})\s*[\(\[].*?[\)\]]\s*([^\r\n]{0,200}?)(?=\d{1,2}:\d{2}|$|\r|\n)", // MM:SS (Title) format
                        // Formats with numbers before timestamp
                        @"(\d+)\.\s*(\d{1,2}):(\d{2}):(\d{2})\s+([^\r\n]{0,200}?)(?=\d+\.\s*\d{1,2}:\d{2}:\d{2}|\d+\.\s*\d{1,2}:\d{2}|$|\r|\n)", // 1. HH:MM:SS Title
                        @"(\d+)\.\s*(\d{1,2}):(\d{2})\s+([^\r\n]{0,200}?)(?=\d+\.\s*\d{1,2}:\d{2}|$|\r|\n)", // 1. MM:SS Title
                        // Flexible format
                        @"(\d+)\s*:\s*(\d+)\s+([^\r\n]{0,200}?)(?=\d+\s*:\s*\d+|$|\r|\n)", // Flexible format
                        // Section format
                        @"Section\s+\d+[:\s-]+([^\r\n]{0,200}?)\s+(\d{1,2}):(\d{2})", // "Section 1: Title 5:30" format
                        // Chapter format
                        @"Chapter\s+\d+[:\s-]+([^\r\n]{0,200}?)\s+(\d{1,2}):(\d{2})", // "Chapter 1: Title 5:30" format
                    };

                    // First try meta description (use safe regex)
                    var descMatch = SafeRegexMatch(html, @"<meta[^>]+name=[""']description[""'][^>]*content=[""']([^""]{0,10000})[""']", RegexOptions.IgnoreCase, timeoutSeconds: 2);
                    var description = descMatch.Success 
                        ? System.Net.WebUtility.HtmlDecode(descMatch.Groups[1].Value)
                        : "";

                    // Also try to find description in page content (multiple possible locations)
                    // Limit HTML size to prevent stack overflow
                    if ((string.IsNullOrWhiteSpace(description) || description.Length < 50) && html.Length < 2_000_000)
                    {
                        // Try different description div patterns (YouTube uses various structures)
                        var descPatterns = new[]
                        {
                            @"<div[^>]*id=[""']description[""'][^>]*>(.*?)</div>",
                            @"<div[^>]*class=[""'][^""']*description[""'][^>]*>(.*?)</div>",
                            @"<ytd-expander[^>]*>(.*?)</ytd-expander>",
                            @"<div[^>]*id=[""']watch-description[""'][^>]*>(.*?)</div>",
                        };

                        foreach (var pattern in descPatterns)
                        {
                            try
                            {
                                // Limit match size to prevent stack overflow (use safe regex)
                                var descDivMatch = SafeRegexMatch(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline, timeoutSeconds: 2);
                                if (descDivMatch.Success)
                                {
                                    var rawDesc = descDivMatch.Groups[1].Value;
                                    // Limit raw description size
                                    if (rawDesc.Length > 10_000) rawDesc = rawDesc.Substring(0, 10_000);
                                    
                                    // Remove HTML tags but preserve text (use safe regex)
                                    string cleaned;
                                    try
                                    {
                                        var tagRegex = new Regex("<[^>]+>", RegexOptions.None, TimeSpan.FromSeconds(1));
                                        cleaned = tagRegex.Replace(rawDesc, " ");
                                    }
                                    catch
                                    {
                                        // Fallback: simple string replacement
                                        cleaned = rawDesc.Replace("<", " ").Replace(">", " ");
                                    }
                                    cleaned = System.Net.WebUtility.HtmlDecode(cleaned);
                                    // Clean whitespace (use safe regex)
                                    try
                                    {
                                        var whitespaceRegex = new Regex(@"\s+", RegexOptions.None, TimeSpan.FromSeconds(1));
                                        cleaned = whitespaceRegex.Replace(cleaned, " ").Trim();
                                    }
                                    catch
                                    {
                                        // Fallback: simple replacement
                                        cleaned = cleaned.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ").Trim();
                                    }
                                    
                                    if (cleaned.Length > description.Length && cleaned.Length < 5_000)
                                    {
                                        description = cleaned;
                                        if (description.Length > 200) break; // Found a good description
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[YouTube Chapters] Description extraction error: {ex.Message}");
                                // Continue to next pattern
                            }
                        }
                    }

                    Console.WriteLine($"[YouTube Chapters] Description length: {description?.Length ?? 0} chars");
                    if (description.Length > 0)
                    {
                        Console.WriteLine($"[YouTube Chapters] Description preview (first 500 chars): {description.Substring(0, Math.Min(500, description.Length))}");
                    }

                    // Limit description size to prevent stack overflow
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        // Truncate description if too large (max 50KB to prevent regex issues)
                        if (description.Length > 50_000)
                        {
                            description = description.Substring(0, 50_000);
                            Console.WriteLine("[YouTube Chapters] Description truncated to 50KB for safety");
                        }
                        
                        foreach (var pattern in patterns)
                        {
                            try
                            {
                                // CRITICAL: Use safe regex helper with timeout to prevent stack overflow
                                var matches = SafeRegexMatches(description, pattern, RegexOptions.Multiline, timeoutSeconds: 2);
                                Console.WriteLine($"[YouTube Chapters] Pattern matched {matches.Count} times");
                                
                                // Limit matches to prevent processing too many (max 100 chapters)
                                int matchCount = 0;
                                const int MAX_CHAPTERS = 100;
                                foreach (Match match in matches)
                                {
                                    if (matchCount++ >= MAX_CHAPTERS)
                                    {
                                        Console.WriteLine($"[YouTube Chapters] Limiting to {MAX_CHAPTERS} chapters to prevent overflow");
                                        break;
                                    }
                                    
                                    // Skip if match failed
                                    if (!match.Success) continue;
                                    
                                    if (match.Groups.Count >= 4)
                                    {
                                        int totalSeconds = 0;
                                        string title = "";

                                        try
                                        {
                                            // Handle different group counts based on pattern
                                            // Pattern: (\d+)\.\s*(\d{1,2}):(\d{2}):(\d{2})\s+([^\r\n]{0,200}?) - 6 groups (1. HH:MM:SS Title)
                                            if (match.Groups.Count == 6 && match.Groups[1].Value.Contains("."))
                                            {
                                                var hours = int.Parse(match.Groups[2].Value);
                                                var minutes = int.Parse(match.Groups[3].Value);
                                                var seconds = int.Parse(match.Groups[4].Value);
                                                totalSeconds = hours * 3600 + minutes * 60 + seconds;
                                                title = match.Groups[5].Value.Trim();
                                            }
                                            // Pattern: (\d+)\.\s*(\d{1,2}):(\d{2})\s+([^\r\n]{0,200}?) - 5 groups (1. MM:SS Title)
                                            else if (match.Groups.Count == 5 && match.Groups[1].Value.Contains("."))
                                            {
                                                var minutes = int.Parse(match.Groups[2].Value);
                                                var seconds = int.Parse(match.Groups[3].Value);
                                                totalSeconds = minutes * 60 + seconds;
                                                title = match.Groups[4].Value.Trim();
                                            }
                                            // Pattern: (\d{1,2}):(\d{2}):(\d{2})\s+([^\r\n]{0,200}?) - 5 groups (HH:MM:SS Title)
                                            else if (match.Groups.Count == 5)
                                            {
                                                var hours = int.Parse(match.Groups[1].Value);
                                                var minutes = int.Parse(match.Groups[2].Value);
                                                var seconds = int.Parse(match.Groups[3].Value);
                                                totalSeconds = hours * 3600 + minutes * 60 + seconds;
                                                title = match.Groups[4].Value.Trim();
                                            }
                                            // Pattern: (\d{1,2}):(\d{2})\s+([^\r\n]{0,200}?) - 4 groups (MM:SS Title)
                                            else if (match.Groups.Count == 4)
                                            {
                                                var minutes = int.Parse(match.Groups[1].Value);
                                                var seconds = int.Parse(match.Groups[2].Value);
                                                totalSeconds = minutes * 60 + seconds;
                                                title = match.Groups[3].Value.Trim();
                                            }
                                            else
                                            {
                                                Console.WriteLine($"[YouTube Chapters] Unexpected group count: {match.Groups.Count}, skipping match. Pattern: {match.Value.Substring(0, Math.Min(50, match.Value.Length))}");
                                                continue;
                                            }

                                            // Clean up title (remove HTML, extra whitespace, special chars) - use safe regex
                                            try
                                            {
                                                var tagRegex = new Regex("<.*?>", RegexOptions.None, TimeSpan.FromSeconds(1));
                                                title = tagRegex.Replace(title, "").Trim();
                                            }
                                            catch { title = title.Replace("<", "").Replace(">", "").Trim(); }
                                            
                                            try
                                            {
                                                var whitespaceRegex = new Regex(@"\s+", RegexOptions.None, TimeSpan.FromSeconds(1));
                                                title = whitespaceRegex.Replace(title, " ");
                                            }
                                            catch { title = title.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ").Trim(); }
                                            
                                            // Remove common YouTube description artifacts (use safe regex)
                                            try
                                            {
                                                var prefixRegex = new Regex(@"^[•\-\*]\s*", RegexOptions.None, TimeSpan.FromSeconds(1));
                                                title = prefixRegex.Replace(title, "");
                                            }
                                            catch { }
                                            
                                            try
                                            {
                                                var suffixRegex = new Regex(@"\s*[•\-\*]\s*$", RegexOptions.None, TimeSpan.FromSeconds(1));
                                                title = suffixRegex.Replace(title, "");
                                            }
                                            catch { }

                                            if (!string.IsNullOrWhiteSpace(title) && title.Length < 150 && title.Length > 2 && !title.StartsWith("http"))
                                            {
                                                // Calculate duration for previous section
                                                if (sections.Count > 0)
                                                {
                                                    var prevSection = sections.Last();
                                                    if (prevSection.StartTimeSeconds.HasValue)
                                                    {
                                                        var duration = (totalSeconds - prevSection.StartTimeSeconds.Value) / 60.0;
                                                        prevSection.EstimatedMinutes = (int)Math.Max(1, Math.Ceiling(duration));
                                                    }
                                                }

                                                sections.Add(new ContentSection
                                                {
                                                    Title = title,
                                                    StartTimeSeconds = totalSeconds
                                                });
                                                
                                                Console.WriteLine($"[YouTube Chapters] Added chapter: {title} at {totalSeconds}s");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"[YouTube Chapters] Error parsing timestamp: {ex.Message}");
                                            // Continue to next match
                                        }
                                    }
                                }

                                if (sections.Count > 0)
                                {
                                    Console.WriteLine($"[YouTube Chapters] Found {sections.Count} chapters using pattern: {pattern.Substring(0, Math.Min(80, pattern.Length))}...");
                                    Console.WriteLine($"[YouTube Chapters] First 3 chapters: {string.Join(" | ", sections.Take(3).Select(s => $"{s.Title} ({s.StartTimeSeconds}s)"))}");
                                    break; // Found chapters with this pattern
                                }
                                else if (matches.Count > 0)
                                {
                                    Console.WriteLine($"[YouTube Chapters] Pattern matched {matches.Count} times but no valid chapters extracted. Pattern: {pattern.Substring(0, Math.Min(80, pattern.Length))}...");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[YouTube Chapters] Regex pattern error: {ex.Message}");
                                // Continue to next pattern
                            }
                        }
                    }
                    
                    // DISABLED: Full HTML search causes stack overflow on large pages
                    // Rely on description parsing only, which is safer and more reliable
                }

                // Calculate duration for last section if we have total duration
                if (sections.Count > 0)
                {
                    var lastSection = sections.Last();
                    if (lastSection.EstimatedMinutes == null || lastSection.EstimatedMinutes == 0)
                    {
                        lastSection.EstimatedMinutes = 5; // Default 5 minutes for last section
                    }
                    
                    // Calculate durations for all sections based on start times
                    for (int i = 0; i < sections.Count - 1; i++)
                    {
                        var current = sections[i];
                        var next = sections[i + 1];
                        
                        if (current.StartTimeSeconds.HasValue && next.StartTimeSeconds.HasValue)
                        {
                            var durationSeconds = next.StartTimeSeconds.Value - current.StartTimeSeconds.Value;
                            current.EstimatedMinutes = Math.Max(1, (int)Math.Ceiling(durationSeconds / 60.0));
                        }
                        else if (current.EstimatedMinutes == null || current.EstimatedMinutes == 0)
                        {
                            current.EstimatedMinutes = 5; // Default 5 minutes if no timing info
                        }
                    }
                    
                    Console.WriteLine($"[YouTube Chapters] Final sections count: {sections.Count}");
                    Console.WriteLine($"[YouTube Chapters] Sections with titles: {sections.Count(s => !string.IsNullOrWhiteSpace(s.Title))}");
                    Console.WriteLine($"[YouTube Chapters] Sections with timestamps: {sections.Count(s => s.StartTimeSeconds.HasValue)}");
                }
            }
            catch (StackOverflowException ex)
            {
                Console.WriteLine($"[YouTube Chapters] CRITICAL: StackOverflowException caught - {ex.Message}");
                Console.WriteLine("[YouTube Chapters] Returning empty sections to prevent server crash");
                return new List<ContentSection>(); // Return empty list instead of crashing
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine($"[YouTube Chapters] CRITICAL: OutOfMemoryException caught - {ex.Message}");
                Console.WriteLine("[YouTube Chapters] Returning empty sections to prevent server crash");
                return new List<ContentSection>(); // Return empty list instead of crashing
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YouTube Chapters] Extraction error: {ex.GetType().Name} - {ex.Message}");
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    var traceLength = Math.Min(500, ex.StackTrace.Length);
                    Console.WriteLine($"[YouTube Chapters] Stack trace: {ex.StackTrace.Substring(0, traceLength)}");
                }
            }

            return sections;
        }

        private static bool TryParseTimeOffset(string offset, out int seconds)
        {
            seconds = 0;
            if (string.IsNullOrEmpty(offset)) return false;
            
            // Try ISO 8601 duration format (PT1H2M10S) or seconds
            if (int.TryParse(offset, out var secs))
            {
                seconds = secs;
                return true;
            }
            
            // Use safe regex (offset string is small, but still use timeout)
            var match = SafeRegexMatch(offset, @"PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?", RegexOptions.None, timeoutSeconds: 1);
            if (match.Success)
            {
                var hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
                var mins = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
                var secs2 = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
                seconds = hours * 3600 + mins * 60 + secs2;
                return true;
            }
            return false;
        }

        private static bool TryExtractChaptersFromJson(JsonElement root, out List<ContentSection> chapters)
        {
            chapters = new List<ContentSection>();
            
            try
            {
                // Try various paths where YouTube might store chapters (removed deep paths that cause stack overflow)
                var paths = new[]
                {
                    new[] { "playerOverlays", "playerOverlayRenderer", "playerBar", "playerBarRenderer", "chapters" },
                    new[] { "playerOverlays", "playerOverlayRenderer", "chapters" },
                    new[] { "videoDetails", "chapters" }
                };

                foreach (var path in paths)
                {
                    var current = root;
                    bool found = true;
                    foreach (var key in path)
                    {
                        if (current.TryGetProperty(key, out var next))
                        {
                            current = next;
                        }
                        else
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        try
                        {
                            // Handle array of chapter items (with safety limit)
                            if (current.ValueKind == JsonValueKind.Array)
                            {
                                int count = 0;
                                const int MAX_CHAPTER_ITEMS = 200; // Limit to prevent excessive processing
                                foreach (var item in current.EnumerateArray())
                                {
                                    if (count++ >= MAX_CHAPTER_ITEMS)
                                    {
                                        Console.WriteLine($"[YouTube Chapters] Limiting to {MAX_CHAPTER_ITEMS} chapter items");
                                        break;
                                    }
                                    ExtractChapterFromItem(item, chapters);
                                }
                            }
                            // Handle object with chapters array
                            else if (current.ValueKind == JsonValueKind.Object)
                            {
                                if (current.TryGetProperty("chapters", out var chaptersArray) && chaptersArray.ValueKind == JsonValueKind.Array)
                                {
                                    int count = 0;
                                    const int MAX_CHAPTER_ITEMS = 200; // Limit to prevent excessive processing
                                    foreach (var item in chaptersArray.EnumerateArray())
                                    {
                                        if (count++ >= MAX_CHAPTER_ITEMS)
                                        {
                                            Console.WriteLine($"[YouTube Chapters] Limiting to {MAX_CHAPTER_ITEMS} chapter items");
                                            break;
                                        }
                                        ExtractChapterFromItem(item, chapters);
                                    }
                                }
                                // Try to find chapterRenderer directly
                                else
                                {
                                    ExtractChapterFromItem(current, chapters);
                                }
                            }

                            if (chapters.Count > 0)
                            {
                                Console.WriteLine($"[YouTube Chapters] Found {chapters.Count} chapters from JSON path: {string.Join(" -> ", path)}");
                                return true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[YouTube Chapters] Error processing path {string.Join(" -> ", path)}: {ex.Message}");
                            // Continue to next path instead of crashing
                        }
                    }
                }
                
                // DISABLED: Recursive search causes stack overflow on large JSON structures
                // Only use direct path extraction and description parsing
                // if (chapters.Count == 0)
                // {
                //     try
                //     {
                //         FindChaptersRecursive(root, chapters, 0, 500);
                //         if (chapters.Count > 0)
                //         {
                //             Console.WriteLine($"[YouTube Chapters] Found {chapters.Count} chapters via recursive search");
                //             return true;
                //         }
                //     }
                //     catch (Exception ex)
                //     {
                //         Console.WriteLine($"[YouTube Chapters] Recursive search failed: {ex.Message}");
                //     }
                // }
            }
            catch { }

            return false;
        }

        private static void ExtractChapterFromItem(JsonElement item, List<ContentSection> chapters)
        {
            if (item.TryGetProperty("chapterRenderer", out var chapterRenderer))
            {
                string? title = null;
                int? startTime = null;

                // Try different title paths
                if (chapterRenderer.TryGetProperty("title", out var titleEl))
                {
                    if (titleEl.TryGetProperty("simpleText", out var titleText))
                        title = titleText.GetString();
                    else if (titleEl.TryGetProperty("runs", out var runs) && runs.ValueKind == JsonValueKind.Array && runs.GetArrayLength() > 0)
                    {
                        if (runs[0].TryGetProperty("text", out var textEl))
                            title = textEl.GetString();
                    }
                }

                // Try different time paths
                if (chapterRenderer.TryGetProperty("timeRangeStartMillis", out var startMillis))
                {
                    startTime = (int)(startMillis.GetInt64() / 1000);
                }
                else if (chapterRenderer.TryGetProperty("timeRangeStartSeconds", out var startSeconds))
                {
                    startTime = startSeconds.GetInt32();
                }
                else if (chapterRenderer.TryGetProperty("startTimeSeconds", out var startSec))
                {
                    startTime = startSec.GetInt32();
                }

                if (!string.IsNullOrWhiteSpace(title))
                {
                    chapters.Add(new ContentSection
                    {
                        Title = title,
                        StartTimeSeconds = startTime
                    });
                }
            }
        }

        private static void FindChaptersRecursive(JsonElement element, List<ContentSection> chapters, int depth = 0, int maxItems = 1000)
        {
            // Stricter limits to prevent stack overflow
            if (depth > 5) return; // Reduced from 10 to 5
            if (maxItems <= 0) return; // Limit total items processed

            try
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    int propCount = 0;
                    foreach (var prop in element.EnumerateObject())
                    {
                        if (maxItems <= 0) break;
                        propCount++;
                        if (propCount > 100) break; // Limit properties per object

                        if (prop.Name.Contains("chapter", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            var array = prop.Value;
                            int itemCount = 0;
                            foreach (var item in array.EnumerateArray())
                            {
                                if (itemCount++ > 50) break; // Limit array items
                                ExtractChapterFromItem(item, chapters);
                                maxItems--;
                                // Continue extracting all chapters
                            }
                            if (chapters.Count > 0) return;
                        }
                        else if (depth < 3) // Only recurse deeper if not too deep
                        {
                            FindChaptersRecursive(prop.Value, chapters, depth + 1, maxItems - 1);
                            if (chapters.Count > 0) return;
                        }
                    }
                }
                else if (element.ValueKind == JsonValueKind.Array)
                {
                    int itemCount = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        if (itemCount++ > 50 || maxItems <= 0) break; // Limit array traversal
                        FindChaptersRecursive(item, chapters, depth + 1, maxItems - 1);
                        if (chapters.Count > 0) return;
                    }
                }
            }
            catch
            {
                // Silently fail to prevent crashes
                return;
            }
        }

        private async Task<UrlMetadata?> TryGetMediumMetadataAsync(string url, CancellationToken ct)
        {
            try
            {
                using var resp = await _http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) return null;
                var html = await resp.Content.ReadAsStringAsync(ct);

                var title = ExtractMetaTag(html, "og:title") ?? ExtractTag(html, "<title>");
                var desc = ExtractMetaTag(html, "og:description") ?? ExtractMetaTag(html, "description");
                var author = ExtractMetaTag(html, "author") ?? ExtractMetaTag(html, "article:author");
                
                // Estimate reading time from word count
                var readingTime = EstimateReadingTime(html);
                
                // Extract headings
                var headings = ExtractHeadings(html, maxCount: 6);

                if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(desc))
                    return null;

                return new UrlMetadata
                {
                    Title = title ?? "",
                    Description = desc ?? "",
                    Author = author ?? "",
                    Platform = "Medium",
                    Headings = headings,
                    EstimatedReadingMinutes = readingTime
                };
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[Medium Metadata] HTTP Request Error: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Console.WriteLine($"[Medium Metadata] Request Timeout: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"[Medium Metadata] Request Canceled: {ex.Message}");
                return null;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[Medium Metadata] Network Error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Medium Metadata] Error ({ex.GetType().Name}): {ex.Message}");
                return null;
            }
        }

        private async Task<UrlMetadata?> TryGetDevToMetadataAsync(string url, CancellationToken ct)
        {
            try
            {
                using var resp = await _http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) return null;
                var html = await resp.Content.ReadAsStringAsync(ct);

                var title = ExtractMetaTag(html, "og:title") ?? ExtractTag(html, "<title>");
                var desc = ExtractMetaTag(html, "og:description") ?? ExtractMetaTag(html, "description");
                
                // Dev.to articles have reading time in meta
                var readingTimeMatch = Regex.Match(html, @"(\d+)\s+min\s+read", RegexOptions.IgnoreCase);
                int? readingTime = readingTimeMatch.Success && int.TryParse(readingTimeMatch.Groups[1].Value, out var rt) ? rt : null;
                
                if (!readingTime.HasValue)
                    readingTime = EstimateReadingTime(html);

                var headings = ExtractHeadings(html, maxCount: 6);

                return new UrlMetadata
                {
                    Title = title ?? "",
                    Description = desc ?? "",
                    Platform = "Dev.to",
                    Headings = headings,
                    EstimatedReadingMinutes = readingTime
                };
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[Dev.to Metadata] HTTP Request Error: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Console.WriteLine($"[Dev.to Metadata] Request Timeout: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"[Dev.to Metadata] Request Canceled: {ex.Message}");
                return null;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[Dev.to Metadata] Network Error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Dev.to Metadata] Error ({ex.GetType().Name}): {ex.Message}");
                return null;
            }
        }

        private async Task<UrlMetadata?> TryGetGitHubMetadataAsync(string url, CancellationToken ct)
        {
            try
            {
                // For GitHub, try to get README or repo info
                if (url.Contains("/blob/") || url.Contains("/tree/"))
                {
                    // Convert blob/tree URL to raw content if possible
                    var rawUrl = url.Replace("/blob/", "/raw/").Replace("/tree/", "/raw/");
                    if (rawUrl != url)
                    {
                        try
                        {
                            using var rawResp = await _http.GetAsync(rawUrl, ct);
                            if (rawResp.IsSuccessStatusCode)
                            {
                                var content = await rawResp.Content.ReadAsStringAsync(ct);
                                var markdownTitle = ExtractMarkdownTitle(content);
                                var headings = ExtractMarkdownHeadings(content, maxCount: 8);
                                var readingTime = EstimateReadingTime(content);

                                return new UrlMetadata
                                {
                                    Title = markdownTitle ?? url.Split('/').LastOrDefault() ?? "GitHub Content",
                                    Description = ExtractMarkdownDescription(content),
                                    Platform = "GitHub",
                                    Headings = headings,
                                    EstimatedReadingMinutes = readingTime
                                };
                            }
                        }
                        catch { }
                    }
                }

                // Fallback to HTML parsing
                using var resp = await _http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) return null;
                var html = await resp.Content.ReadAsStringAsync(ct);

                var title = ExtractMetaTag(html, "og:title") ?? ExtractTag(html, "<title>");
                var desc = ExtractMetaTag(html, "og:description") ?? ExtractMetaTag(html, "description");

                return new UrlMetadata
                {
                    Title = title ?? "GitHub Repository",
                    Description = desc ?? "",
                    Platform = "GitHub",
                    Headings = ExtractHeadings(html, maxCount: 5)
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task<UrlMetadata?> TryGetDocumentationMetadataAsync(string url, CancellationToken ct)
        {
            try
            {
                using var resp = await _http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) return null;
                var html = await resp.Content.ReadAsStringAsync(ct);

                var title = ExtractMetaTag(html, "og:title") ?? ExtractTag(html, "<title>");
                var desc = ExtractMetaTag(html, "og:description") ?? ExtractMetaTag(html, "description");
                
                // Documentation sites usually have good heading structure
                var headings = ExtractHeadings(html, maxCount: 10);
                
                // Try to extract table of contents
                var tocSections = ExtractTableOfContents(html);
                if (tocSections.Any())
                {
                    return new UrlMetadata
                    {
                        Title = title ?? "",
                        Description = desc ?? "",
                        Author = ExtractMetaTag(html, "author") ?? ExtractMetaTag(html, "article:author") ?? "",
                        ThumbnailUrl = ExtractMetaTag(html, "og:image") ?? ExtractMetaTag(html, "twitter:image"),
                        Platform = "Documentation",
                        Headings = tocSections.Select(s => s.Title).ToList(),
                        Sections = tocSections,
                        EstimatedReadingMinutes = EstimateReadingTime(html)
                    };
                }

                var sections = ExtractArticleSections(html, maxCount: 16, defaultMinutes: 8);

                return new UrlMetadata
                {
                    Title = title ?? "",
                    Description = desc ?? "",
                    Author = ExtractMetaTag(html, "author") ?? ExtractMetaTag(html, "article:author") ?? "",
                    ThumbnailUrl = ExtractMetaTag(html, "og:image") ?? ExtractMetaTag(html, "twitter:image"),
                    Platform = "Documentation",
                    Headings = headings,
                    Sections = sections,
                    EstimatedReadingMinutes = EstimateReadingTime(html)
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task<UrlMetadata?> TryGetUdemyMetadataAsync(string url, CancellationToken ct)
        {
            try
            {
                using var resp = await _http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) return null;
                var html = await resp.Content.ReadAsStringAsync(ct);
                if (html.Length > 2_000_000) html = html.Substring(0, 2_000_000);

                var title = ExtractMetaTag(html, "og:title") ?? ExtractTag(html, "<title>");
                var desc = ExtractMetaTag(html, "og:description") ?? ExtractMetaTag(html, "description");
                
                int? durationMinutes = null;
                try
                {
                    var jsonLdPattern = @"<script[^>]*type=[""']application/ld\+json[""'][^>]*>(.*?)</script>";
                    var jsonLdMatch = Regex.Match(html, jsonLdPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    if (jsonLdMatch.Success && jsonLdMatch.Groups[1].Value.Length < 50_000)
                    {
                        using var doc = JsonDocument.Parse(jsonLdMatch.Groups[1].Value);
                        var root = doc.RootElement;
                        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                            root = root[0];
                        
                        if (root.TryGetProperty("timeRequired", out var timeRequired))
                        {
                            var timeStr = timeRequired.GetString();
                            if (!string.IsNullOrWhiteSpace(timeStr) && TryParseIso8601Duration(timeStr, out var mins))
                                durationMinutes = mins;
                        }
                    }
                }
                catch { }

                var sections = new List<ContentSection>();
                var headings = ExtractHeadings(html, maxCount: 20);
                var lecturePattern = @"lecture-title[""']?\s*[^>]*>(.*?)</";
                var lectureMatches = Regex.Matches(html, lecturePattern, RegexOptions.IgnoreCase);
                foreach (Match match in lectureMatches.Take(30))
                {
                    if (match.Groups.Count > 1)
                    {
                        var lectureTitle = System.Net.WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
                        if (!string.IsNullOrWhiteSpace(lectureTitle) && lectureTitle.Length < 200)
                        {
                            sections.Add(new ContentSection { Title = lectureTitle, EstimatedMinutes = 10 });
                        }
                    }
                }

                return new UrlMetadata
                {
                    Title = CleanTitle(title ?? "Udemy Course"),
                    Description = desc ?? "",
                    Platform = "Udemy",
                    Headings = headings,
                    Sections = sections.Any() ? sections : headings.Select(h => new ContentSection { Title = h, EstimatedMinutes = 15 }).ToList(),
                    DurationMinutes = durationMinutes,
                    EstimatedReadingMinutes = durationMinutes
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Udemy Metadata] Error: {ex.Message}");
                return null;
            }
        }

        private async Task<UrlMetadata?> TryGetCourseraMetadataAsync(string url, CancellationToken ct)
        {
            try
            {
                using var resp = await _http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) return null;
                var html = await resp.Content.ReadAsStringAsync(ct);
                if (html.Length > 2_000_000) html = html.Substring(0, 2_000_000);

                var title = ExtractMetaTag(html, "og:title") ?? ExtractTag(html, "<title>");
                var desc = ExtractMetaTag(html, "og:description") ?? ExtractMetaTag(html, "description");
                var headings = ExtractHeadings(html, maxCount: 20);
                var sections = new List<ContentSection>();

                var weekPattern = @"week\s+(\d+)[:""']?\s*([^<""']+)";
                var weekMatches = Regex.Matches(html, weekPattern, RegexOptions.IgnoreCase);
                foreach (Match match in weekMatches.Take(20))
                {
                    if (match.Groups.Count >= 3)
                    {
                        sections.Add(new ContentSection
                        {
                            Title = $"Week {match.Groups[1].Value}: {match.Groups[2].Value.Trim()}",
                            EstimatedMinutes = 120
                        });
                    }
                }

                if (!sections.Any())
                {
                    sections = headings.Select(h => new ContentSection { Title = h, EstimatedMinutes = 60 }).ToList();
                }

                return new UrlMetadata
                {
                    Title = CleanTitle(title ?? "Coursera Course"),
                    Description = desc ?? "",
                    Platform = "Coursera",
                    Headings = headings,
                    Sections = sections,
                    EstimatedReadingMinutes = sections.Count * 60
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Coursera Metadata] Error: {ex.Message}");
                return null;
            }
        }

        private async Task<UrlMetadata?> TryGetKhanAcademyMetadataAsync(string url, CancellationToken ct)
        {
            try
            {
                using var resp = await _http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) return null;
                var html = await resp.Content.ReadAsStringAsync(ct);
                if (html.Length > 1_000_000) html = html.Substring(0, 1_000_000);

                var title = ExtractMetaTag(html, "og:title") ?? ExtractTag(html, "<title>");
                var desc = ExtractMetaTag(html, "og:description") ?? ExtractMetaTag(html, "description");
                var headings = ExtractHeadings(html, maxCount: 15);

                return new UrlMetadata
                {
                    Title = CleanTitle(title ?? "Khan Academy"),
                    Description = desc ?? "",
                    Platform = "Khan Academy",
                    Headings = headings,
                    Sections = headings.Select(h => new ContentSection { Title = h, EstimatedMinutes = 10 }).ToList(),
                    EstimatedReadingMinutes = headings.Count * 10
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Khan Academy Metadata] Error: {ex.Message}");
                return null;
            }
        }

        private async Task<UrlMetadata?> TryGetEdxMetadataAsync(string url, CancellationToken ct)
        {
            try
            {
                using var resp = await _http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) return null;
                var html = await resp.Content.ReadAsStringAsync(ct);
                if (html.Length > 2_000_000) html = html.Substring(0, 2_000_000);

                var title = ExtractMetaTag(html, "og:title") ?? ExtractTag(html, "<title>");
                var desc = ExtractMetaTag(html, "og:description") ?? ExtractMetaTag(html, "description");
                var headings = ExtractHeadings(html, maxCount: 20);
                var sections = new List<ContentSection>();

                var modulePattern = @"module[""']?\s*[^>]*>(.*?)</";
                var moduleMatches = Regex.Matches(html, modulePattern, RegexOptions.IgnoreCase);
                foreach (Match match in moduleMatches.Take(20))
                {
                    if (match.Groups.Count > 1)
                    {
                        var moduleTitle = System.Net.WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
                        if (!string.IsNullOrWhiteSpace(moduleTitle) && moduleTitle.Length < 200)
                        {
                            sections.Add(new ContentSection { Title = moduleTitle, EstimatedMinutes = 90 });
                        }
                    }
                }

                if (!sections.Any())
                {
                    sections = headings.Select(h => new ContentSection { Title = h, EstimatedMinutes = 60 }).ToList();
                }

                return new UrlMetadata
                {
                    Title = CleanTitle(title ?? "EdX Course"),
                    Description = desc ?? "",
                    Platform = "EdX",
                    Headings = headings,
                    Sections = sections,
                    EstimatedReadingMinutes = sections.Count * 90
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EdX Metadata] Error: {ex.Message}");
                return null;
            }
        }

        private static string CleanTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return "";
            return title
                .Replace(" | Udemy", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" | Coursera", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" | Khan Academy", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" | edX", "", StringComparison.OrdinalIgnoreCase)
                .Trim();
        }

        private async Task<UrlMetadata?> TryGetHtmlMetadataAsync(string url, CancellationToken ct)
        {
            try
            {
                using var resp = await _http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) return null;

                var mediaType = resp.Content.Headers.ContentType?.MediaType ?? string.Empty;
                if (mediaType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return await TryGetPdfMetadataAsync(url, ct);
                }

                var html = await resp.Content.ReadAsStringAsync(ct);

                // Try Open Graph and standard meta tags first
                var title = ExtractMetaTag(html, "og:title") 
                    ?? ExtractMetaTag(html, "twitter:title")
                    ?? ExtractTag(html, "<title>");
                
                var desc = ExtractMetaTag(html, "og:description")
                    ?? ExtractMetaTag(html, "twitter:description")
                    ?? ExtractMetaTag(html, "description");
                
                var author = ExtractMetaTag(html, "author") 
                    ?? ExtractMetaTag(html, "article:author");
                
                var thumbnail = ExtractMetaTag(html, "og:image")
                    ?? ExtractMetaTag(html, "twitter:image");

                var headings = ExtractHeadings(html, maxCount: 5);
                var sections = ExtractArticleSections(html, maxCount: 12, defaultMinutes: 10);
                var readingTime = EstimateReadingTime(html);

                if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(desc) && headings.Count == 0)
                    return null;

                return new UrlMetadata
                {
                    Title = title ?? "",
                    Description = desc ?? "",
                    Author = author ?? "",
                    Platform = "Website",
                    Headings = headings,
                    Sections = sections,
                    EstimatedReadingMinutes = readingTime,
                    ThumbnailUrl = thumbnail
                };
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[HTML Metadata] HTTP Request Error: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Console.WriteLine($"[HTML Metadata] Request Timeout: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"[HTML Metadata] Request Canceled: {ex.Message}");
                return null;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[HTML Metadata] Network Error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HTML Metadata] Error ({ex.GetType().Name}): {ex.Message}");
                return null;
            }
        }

        // Helper methods

        private async Task<UrlMetadata?> TryGetPdfMetadataAsync(string url, CancellationToken ct)
        {
            try
            {
                using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!resp.IsSuccessStatusCode)
                    return null;

                var mediaType = resp.Content.Headers.ContentType?.MediaType ?? string.Empty;
                var likelyPdf = mediaType.Contains("pdf", StringComparison.OrdinalIgnoreCase)
                    || LooksLikePdfUrl(url);
                if (!likelyPdf)
                    return null;

                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                var bytes = await ReadUpToBytesAsync(stream, 2_000_000, ct);
                if (bytes.Length < 4)
                    return null;

                var header = Encoding.ASCII.GetString(bytes.Take(4).ToArray());
                if (!header.StartsWith("%PDF", StringComparison.Ordinal))
                    return null;

                var contentText = Encoding.Latin1.GetString(bytes);
                var extractedTitle = ExtractPdfInfoToken(contentText, "Title");
                var extractedAuthor = ExtractPdfInfoToken(contentText, "Author");

                var pageCount = Regex.Matches(contentText, @"/Type\s*/Page\b", RegexOptions.IgnoreCase).Count;
                if (pageCount <= 0) pageCount = 8;

                var filename = Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    ? Path.GetFileName(uri.LocalPath)
                    : "Document.pdf";
                filename = System.Net.WebUtility.UrlDecode(filename ?? "Document.pdf");
                if (string.IsNullOrWhiteSpace(filename)) filename = "Document.pdf";

                var title = !string.IsNullOrWhiteSpace(extractedTitle)
                    ? extractedTitle
                    : Regex.Replace(filename, @"\.pdf$", "", RegexOptions.IgnoreCase);

                var headings = BuildPdfHeadingCandidates(contentText, maxCount: 10);
                var sections = headings.Any()
                    ? headings.Select(h => new ContentSection { Title = h, EstimatedMinutes = 12 }).ToList()
                    : Enumerable.Range(1, Math.Min(10, Math.Max(3, pageCount / 2)))
                        .Select(i => new ContentSection { Title = $"Section {i}", EstimatedMinutes = 12 })
                        .ToList();

                var estimatedMinutes = Math.Max(10, Math.Min(240, pageCount * 3));

                return new UrlMetadata
                {
                    Title = title,
                    Description = $"PDF document ({pageCount} pages)",
                    Author = extractedAuthor ?? string.Empty,
                    Platform = "PDF",
                    Headings = headings.Any() ? headings : sections.Select(s => s.Title).ToList(),
                    Sections = sections,
                    EstimatedReadingMinutes = estimatedMinutes,
                    DurationMinutes = estimatedMinutes
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PDF Metadata] Error ({ex.GetType().Name}): {ex.Message}");
                return null;
            }
        }

        private static async Task<byte[]> ReadUpToBytesAsync(Stream stream, int maxBytes, CancellationToken ct)
        {
            using var ms = new MemoryStream(capacity: Math.Min(maxBytes, 256_000));
            var buffer = new byte[8192];
            int total = 0;
            while (total < maxBytes)
            {
                var toRead = Math.Min(buffer.Length, maxBytes - total);
                var read = await stream.ReadAsync(buffer.AsMemory(0, toRead), ct);
                if (read <= 0) break;
                ms.Write(buffer, 0, read);
                total += read;
            }
            return ms.ToArray();
        }

        private static string? ExtractPdfInfoToken(string content, string key)
        {
            var match = Regex.Match(content, $@"/{Regex.Escape(key)}\s*\((.*?)\)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success || match.Groups.Count < 2)
                return null;

            var value = match.Groups[1].Value;
            value = value.Replace("\\(", "(").Replace("\\)", ")").Replace("\\n", " ").Replace("\\r", " ");
            value = Regex.Replace(value, @"\s+", " ").Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static List<string> BuildPdfHeadingCandidates(string content, int maxCount)
        {
            var lines = Regex.Split(content, @"\r?\n");
            var headingCandidates = new List<string>();

            foreach (var rawLine in lines)
            {
                if (headingCandidates.Count >= maxCount) break;
                var line = Regex.Replace(rawLine, @"\s+", " ").Trim();
                if (line.Length < 8 || line.Length > 120) continue;
                if (!Regex.IsMatch(line, @"[A-Za-z]{3,}")) continue;
                if (line.Contains("obj") || line.Contains("endobj") || line.Contains("stream")) continue;

                if (Regex.IsMatch(line, @"^(\d+(\.\d+){0,2}|[IVXLC]+\.)\s+", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(line, @"^(abstract|introduction|background|method|methods|results|discussion|conclusion|references)\b", RegexOptions.IgnoreCase))
                {
                    headingCandidates.Add(System.Net.WebUtility.HtmlDecode(line));
                }
            }

            return headingCandidates
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(maxCount)
                .ToList();
        }

        private static List<ContentSection> ExtractArticleSections(string html, int maxCount, int defaultMinutes)
        {
            var headings = ExtractHeadings(html, maxCount: maxCount);
            if (!headings.Any())
                return new List<ContentSection>();

            return headings
                .Select(h => new ContentSection
                {
                    Title = h,
                    EstimatedMinutes = defaultMinutes
                })
                .ToList();
        }

        private static string? ExtractMetaTag(string html, string propertyName)
        {
            var patterns = new[]
            {
                $@"<meta[^>]+property=[""']{Regex.Escape(propertyName)}[""'][^>]*content=[""'](.*?)[""']",
                $@"<meta[^>]+name=[""']{Regex.Escape(propertyName)}[""'][^>]*content=[""'](.*?)[""']",
                $@"<meta[^>]+content=[""'](.*?)[""'][^>]+property=[""']{Regex.Escape(propertyName)}[""']",
                $@"<meta[^>]+content=[""'](.*?)[""'][^>]+name=[""']{Regex.Escape(propertyName)}[""']"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (match.Success && match.Groups.Count > 1)
                {
                    var value = System.Net.WebUtility.HtmlDecode(match.Groups[1].Value.Trim());
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }

            return null;
        }

        private static string? ExtractTag(string html, string tag)
        {
            var pattern = tag == "<title>" 
                ? @"<title[^>]*>(.*?)</title>"
                : $@"{Regex.Escape(tag)}[^>]*>(.*?)</{Regex.Escape(tag.Replace("<", "</"))}";
            
            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success && match.Groups.Count > 1)
            {
                var value = System.Net.WebUtility.HtmlDecode(Regex.Replace(match.Groups[1].Value, "<.*?>", "").Trim());
                return !string.IsNullOrWhiteSpace(value) ? value : null;
            }
            return null;
        }

        private static List<string> ExtractHeadings(string html, int maxCount = 5)
        {
            return Regex.Matches(html, "<h[1-4][^>]*>(.*?)</h[1-4]>", RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .Cast<Match>()
                .Select(m => Regex.Replace(m.Groups[1].Value, "<.*?>", " "))
                .Select(s => System.Net.WebUtility.HtmlDecode(Regex.Replace(s, @"\s+", " ").Trim()))
                .Where(h => !string.IsNullOrWhiteSpace(h) && h.Length is >= 3 and <= 180)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(maxCount)
                .ToList();
        }

        private static List<ContentSection> ExtractTableOfContents(string html)
        {
            var sections = new List<ContentSection>();
            
            // Look for common TOC patterns
            var tocPatterns = new[]
            {
                @"<nav[^>]*class=[""'][^""']*toc[^""']*[""'][^>]*>(.*?)</nav>",
                @"<ul[^>]*class=[""'][^""']*toc[^""']*[""'][^>]*>(.*?)</ul>",
                @"<div[^>]*class=[""'][^""']*table-of-contents[^""']*[""'][^>]*>(.*?)</div>"
            };

            foreach (var pattern in tocPatterns)
            {
                var tocMatch = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (tocMatch.Success)
                {
                    var tocContent = tocMatch.Groups[1].Value;
                    var links = Regex.Matches(tocContent, @"<a[^>]*href=[""']#([^""']+)[""'][^>]*>(.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    
                    foreach (Match link in links)
                    {
                        if (link.Groups.Count >= 3)
                        {
                            var title = System.Net.WebUtility.HtmlDecode(Regex.Replace(link.Groups[2].Value, "<.*?>", "").Trim());
                            if (!string.IsNullOrWhiteSpace(title))
                            {
                                sections.Add(new ContentSection { Title = title });
                            }
                        }
                    }
                    
                    if (sections.Any()) break;
                }
            }

            return sections;
        }

        private static string? ExtractMarkdownTitle(string markdown)
        {
            // First H1
            var h1Match = Regex.Match(markdown, @"^#\s+(.+)$", RegexOptions.Multiline);
            if (h1Match.Success)
                return h1Match.Groups[1].Value.Trim();
            
            // First line if it looks like a title
            var firstLine = markdown.Split('\n').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(firstLine) && firstLine.Length < 100 && !firstLine.StartsWith("#"))
                return firstLine;
            
            return null;
        }

        private static string ExtractMarkdownDescription(string markdown)
        {
            // First paragraph after title
            var lines = markdown.Split('\n').Skip(1).Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("#"));
            var firstPara = string.Join(" ", lines.Take(3)).Trim();
            return firstPara.Length > 300 ? firstPara.Substring(0, 300) + "..." : firstPara;
        }

        private static List<string> ExtractMarkdownHeadings(string markdown, int maxCount = 8)
        {
            return Regex.Matches(markdown, @"^#{1,3}\s+(.+)$", RegexOptions.Multiline)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value.Trim())
                .Where(h => !string.IsNullOrWhiteSpace(h) && h.Length < 150)
                .Take(maxCount)
                .ToList();
        }

        private static int EstimateReadingTime(string content)
        {
            // Remove HTML tags
            var text = Regex.Replace(content, "<.*?>", " ");
            text = System.Net.WebUtility.HtmlDecode(text);
            
            // Count words (rough estimate)
            var words = Regex.Matches(text, @"\b\w+\b").Count;
            
            // Average reading speed: 200-250 words per minute
            // Use 200 for conservative estimate
            var minutes = (int)Math.Ceiling(words / 200.0);
            
            return Math.Max(1, Math.Min(minutes, 120)); // Cap at 2 hours
        }
    }
}




