/**
 * Quiz API - Quiz management endpoints
 */
import http from "./http";

export const quizApi = {
  async getQuizForModule(moduleId) {
    if (!moduleId || moduleId === null || moduleId === undefined) {
      throw new Error("Module ID is required");
    }
    return http.get(`/api/quizzes/module/${moduleId}`);
  },

  async submitQuizAttempt(quizId, submission) {
    return http.post(`/api/quizzes/${quizId}/attempt`, submission);
  },

  async getQuizAttempts(quizId) {
    return http.get(`/api/quizzes/${quizId}/attempts`);
  },
};

