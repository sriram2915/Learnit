/**
 * Progress API - Progress tracking endpoints
 */
import http from "./http";

export const progressApi = {
  async getProgressDashboard(params = {}) {
    const queryParams = new URLSearchParams();
    if (
      params.timezoneOffsetMinutes !== undefined &&
      params.timezoneOffsetMinutes !== null
    ) {
      queryParams.append(
        "timezoneOffsetMinutes",
        String(params.timezoneOffsetMinutes)
      );
    }

    if (params.weekOffset !== undefined && params.weekOffset !== null) {
      queryParams.append("weekOffset", String(params.weekOffset));
    }

    const queryString = queryParams.toString();
    const endpoint = `/api/progress/dashboard${
      queryString ? `?${queryString}` : ""
    }`;
    return http.get(endpoint);
  },
};
