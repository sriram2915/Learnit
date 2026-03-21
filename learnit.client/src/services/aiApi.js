/**
 * AI API - AI helper endpoints
 */
import http from "./http";

// Helper function to detect YouTube URLs
const isYouTubeUrl = (url) => {
  if (!url || typeof url !== "string") return false;
  return url.includes("youtube.com") || url.includes("youtu.be");
};

export const aiApi = {
  chat(payload) {
    return http.post("/api/ai/chat", payload);
  },
  createCourse(payload) {
    // Check if payload contains a YouTube URL
    const url = typeof payload === "string" ? null : (payload?.url || payload?.Url);
    
    if (url && isYouTubeUrl(url)) {
      // Route to YouTube endpoint
      return http.post("/api/youtube/create-course", {
        url: url,
        title: payload?.title || payload?.Title || null,
        description: payload?.description || payload?.Description || null,
      });
    }
    
    // Backwards compatibility: allow string prompt or structured object
    if (typeof payload === "string") {
      return http.post("/api/ai/create-course", { prompt: payload });
    }
    return http.post("/api/ai/create-course", payload);
  },
  scheduleInsights(prompt = "") {
    return http.post("/api/ai/schedule-insights", { prompt });
  },
  progressInsights(prompt = "") {
    return http.post("/api/ai/progress-insights", { prompt });
  },
  compareFriends(friendIds) {
    return http.post("/api/ai/compare", { friendIds });
  },
  listFriends() {
    return http.get("/api/friends");
  },
  addFriend(friend) {
    return http.post("/api/friends", friend);
  },
  deleteFriend(id) {
    return http.delete(`/api/friends/${id}`);
  },
};
