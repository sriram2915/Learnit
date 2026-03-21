/**
 * Schedule API - Schedule and calendar management endpoints
 */
import http from "./http";

export const scheduleApi = {
  // Schedule Events
  async getScheduleEvents(params = {}) {
    const queryParams = new URLSearchParams();
    if (params.from) queryParams.append("from", params.from);
    if (params.to) queryParams.append("to", params.to);

    const queryString = queryParams.toString();
    const endpoint = `/api/schedule${queryString ? `?${queryString}` : ""}`;
    return http.get(endpoint);
  },

  async createScheduleEvent(event) {
    try {
      return await http.post("/api/schedule", event);
    } catch (error) {
      // Enhance error with status code for better handling
      if (error.status) {
        error.response = { status: error.status };
      }
      throw error;
    }
  },

  async updateScheduleEvent(id, event) {
    try {
      return await http.put(`/api/schedule/${id}`, event);
    } catch (error) {
      // Enhance error with status code for better handling
      if (error.status) {
        error.response = { status: error.status };
      }
      throw error;
    }
  },

  async deleteScheduleEvent(id) {
    return http.delete(`/api/schedule/${id}`);
  },

  async resetSchedule() {
    return http.delete("/api/schedule/reset");
  },

  // Module Scheduling
  async getAvailableModules() {
    return http.get("/api/schedule/available-modules");
  },

  async autoScheduleModules(options = {}) {
    return http.post("/api/schedule/auto-schedule", options);
  },

  async linkEventToModule(eventId, moduleId) {
    return http.post(`/api/schedule/${eventId}/link-module/${moduleId}`, {});
  },

  async unlinkEventFromModule(eventId) {
    return http.delete(`/api/schedule/${eventId}/unlink-module`);
  },

  async autoAdjustSchedule(options = {}) {
    return http.post("/api/schedule/auto-adjust", options);
  },
};
