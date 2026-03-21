/**
 * Auth API - Authentication related endpoints
 */
import http from "./http";

export const authApi = {
  async login(email, password) {
    try {
      const response = await http.post("/api/auth/login", { email, password });

      console.log("[AuthAPI] Login response:", response);

      // API returns: { token, user } or { data: { token, user } }
      if (response.data) {
        return response.data;
      }

      return response;
    } catch (error) {
      console.error("[AuthAPI] Login failed:", error);
      throw error;
    }
  },

  async register(fullName, email, password) {
    try {
      const response = await http.post("/api/auth/register", {
        fullName,
        email,
        password,
      });

      console.log("[AuthAPI] Register response:", response);

      // API returns: { token, user } or { data: { token, user } }
      if (response.data) {
        return response.data;
      }

      return response;
    } catch (error) {
      console.error("[AuthAPI] Register failed:", error);
      throw error;
    }
  },

  async logout() {
    const token = localStorage.getItem("token");

    if (!token) {
      console.log("[AuthAPI] No token found, skipping logout API call");
      return { message: "Already logged out" };
    }

    try {
      const response = await http.post("/api/auth/logout", {});
      console.log("[AuthAPI] Logout response:", response);
      return response;
    } catch (error) {
      if (error.status === 401 || error.message.includes("Failed to fetch")) {
        console.log(
          "[AuthAPI] Logout API call failed, proceeding with local logout:",
          error.message
        );
        return { message: "Logged out successfully" };
      }
      console.warn("[AuthAPI] Logout error:", error.message);
      return { message: "Logged out successfully" };
    }
  },
};
