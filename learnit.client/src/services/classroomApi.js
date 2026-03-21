/**
 * Classroom API - Community classroom management endpoints
 */
import http from "./http";

export const classroomApi = {
  // Get all classrooms user is part of
  async getClassrooms() {
    return http.get("/api/classrooms");
  },

  // Get classroom details
  async getClassroom(id) {
    return http.get(`/api/classrooms/${id}`);
  },

  // Create new classroom
  async createClassroom(classroomData) {
    return http.post("/api/classrooms", classroomData);
  },

  // Update classroom (creator only)
  async updateClassroom(id, classroomData) {
    return http.put(`/api/classrooms/${id}`, classroomData);
  },

  // Delete classroom (creator only)
  async deleteClassroom(id) {
    return http.delete(`/api/classrooms/${id}`);
  },

  // Join classroom by invite code
  async joinClassroom(inviteCode) {
    return http.post("/api/classrooms/join", { inviteCode });
  },

  // Join classroom by ID and invite code
  async joinClassroomById(id, inviteCode) {
    return http.post(`/api/classrooms/${id}/join`, { inviteCode });
  },

  // Leave classroom
  async leaveClassroom(id) {
    return http.post(`/api/classrooms/${id}/leave`);
  },

  // Get classroom members
  async getMembers(id) {
    return http.get(`/api/classrooms/${id}/members`);
  },

  // Remove member (creator only)
  async removeMember(classroomId, memberId) {
    return http.delete(`/api/classrooms/${classroomId}/members/${memberId}`);
  },

  // Share courses to classroom
  async shareCourses(classroomId, courseIds) {
    return http.post(`/api/classrooms/${classroomId}/courses/share`, {
      courseIds: Array.isArray(courseIds) ? courseIds : [courseIds]
    });
  },

  // Unshare course from classroom
  async unshareCourse(classroomId, courseId) {
    return http.delete(`/api/classrooms/${classroomId}/courses/${courseId}`);
  },

  // Get shared courses in classroom
  async getSharedCourses(classroomId) {
    return http.get(`/api/classrooms/${classroomId}/courses`);
  },

  // Copy shared course to personal library
  async copyCourse(classroomCourseId) {
    return http.post(`/api/classrooms/courses/${classroomCourseId}/copy`);
  },

  // Get public classrooms
  async getPublicClassrooms() {
    return http.get("/api/classrooms/public");
  },

  // Get member progress for classroom
  async getMemberProgress(classroomId) {
    return http.get(`/api/classrooms/${classroomId}/progress`);
  }
};

