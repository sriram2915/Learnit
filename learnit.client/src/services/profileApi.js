/**
 * Profile API - User profile and preferences endpoints
 */
import http from "./http";

export const profileApi = {
  async getProfile() {
    return http.get("/api/profile");
  },

  async updateProfile(profileData) {
    return http.put("/api/profile/info", profileData);
  },

  async updatePreferences(preferencesData) {
    return http.put("/api/profile/preferences", preferencesData);
  },

  async changePassword(passwordData) {
    return http.put("/api/profile/password", passwordData);
  },
};
