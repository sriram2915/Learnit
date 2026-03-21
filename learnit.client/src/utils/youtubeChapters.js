// src/utils/youtubeChapters.js

/**
 * Parses YouTube chapter timestamps and titles from a video description.
 * Supports various formats like "0:00 Intro", "[1:23:45] Main Topic", "Chapter 1: 12:34 Title".
 * @param {string} description The YouTube video description.
 * @returns {Array<{time: number, title: string}>} An array of chapter objects, sorted by time.
 */
export const parseYouTubeChapters = (description) => {
  if (!description) return [];

  const chapters = [];
  // Regex to find timestamps:
  // (?:^|\n|\s) - start of line, newline, or space
  // (?:\[?(\d{1,2}):(\d{2})(?::(\d{2}))?\]?) - optional bracket, HH:MM:SS or MM:SS
  // (?:[ -]*) - optional separator
  // (.+) - chapter title
  const chapterRegex = /(?:^|\n|\s)(?:\[?(\d{1,2}):(\d{2})(?::(\d{2}))?\]?)(?:[ -]*)(.+)/g;
  let match;

  while ((match = chapterRegex.exec(description)) !== null) {
    const hours = parseInt(match[1] || '0', 10);
    const minutes = parseInt(match[2], 10);
    const seconds = parseInt(match[3] || '0', 10);
    const title = match[4].trim();

    // Calculate total seconds
    let totalSeconds = 0;
    if (match[3]) { // HH:MM:SS format
      totalSeconds = hours * 3600 + minutes * 60 + seconds;
    } else { // MM:SS format
      totalSeconds = hours * 60 + minutes;
    }

    if (title) {
      chapters.push({ time: totalSeconds, title });
    }
  }

  // Sort chapters by time to ensure correct order
  return chapters.sort((a, b) => a.time - b.time);
};

/**
 * Maps parsed YouTube chapters to existing course submodules.
 * Prioritizes exact title matches, then fuzzy matches, then index-based fallback.
 * @param {Array<{time: number, title: string}>} chapters Parsed YouTube chapters.
 * @param {Array<Object>} modules Course modules with submodules.
 * @param {number} videoDuration Total video duration in seconds.
 * @returns {Array<{subModule: Object, startTime: number, endTime: number, duration: number}>} Mapped submodules with time ranges.
 */
export const mapChaptersToSubModules = (chapters, modules, videoDuration) => {
  if (!chapters?.length || !modules?.length) return [];

  const allSubModules = modules.flatMap(module =>
    (module.subModules || []).map(subModule => ({ ...subModule, parentModule: module }))
  ).sort((a, b) => (a.parentModule.order || 0) - (b.parentModule.order || 0) || (a.order || 0) - (b.order || 0));

  const mappedSubModules = [];
  let chapterIndex = 0;
  let currentCumulativeTime = 0;

  for (let i = 0; i < allSubModules.length; i++) {
    const subModule = allSubModules[i];
    let startTime = currentCumulativeTime;
    let endTime = currentCumulativeTime;
    let duration = 0;

    // Try to find a matching chapter
    let matchedChapter = null;
    let bestMatchIndex = -1;

    // 1. Exact title match (case-insensitive, contains)
    for (let j = chapterIndex; j < chapters.length; j++) {
      if (subModule.title && chapters[j].title &&
          (subModule.title.toLowerCase().includes(chapters[j].title.toLowerCase()) ||
           chapters[j].title.toLowerCase().includes(subModule.title.toLowerCase()))) {
        matchedChapter = chapters[j];
        bestMatchIndex = j;
        break;
      }
    }

    // If a chapter was matched, use its time
    if (matchedChapter) {
      startTime = matchedChapter.time;
      // Determine end time from the next chapter or video duration
      endTime = (bestMatchIndex + 1 < chapters.length) ? chapters[bestMatchIndex + 1].time : videoDuration;
      duration = endTime - startTime;
      chapterIndex = bestMatchIndex + 1; // Advance chapter index
    } else {
      // Fallback: if no chapter matches, use estimated hours or distribute evenly
      const estimatedHours = subModule.estimatedHours || 0;
      duration = estimatedHours > 0 ? estimatedHours * 3600 : (videoDuration / allSubModules.length);
      startTime = currentCumulativeTime;
      endTime = startTime + duration;
    }

    // Ensure duration is positive
    duration = Math.max(0, duration);
    endTime = startTime + duration;

    mappedSubModules.push({
      subModule,
      module: subModule.parentModule,
      startTime: startTime,
      endTime: endTime,
      duration: duration,
    });
    currentCumulativeTime = endTime;
  }

  // If there are remaining chapters not mapped to submodules,
  // or if the total mapped duration is less than video duration,
  // adjust the last submodule's end time to match video duration.
  if (mappedSubModules.length > 0 && videoDuration > 0) {
    const lastMapped = mappedSubModules[mappedSubModules.length - 1];
    if (lastMapped.endTime < videoDuration) {
      lastMapped.duration = videoDuration - lastMapped.startTime;
      lastMapped.endTime = videoDuration;
    }
  }

  return mappedSubModules;
};

