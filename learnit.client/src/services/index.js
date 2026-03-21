/**
 * API Services - Unified export for all API modules
 * Provides a clean interface for importing API functions
 */
import { authApi } from "./authApi";
import { courseApi } from "./courseApi";
import { scheduleApi } from "./scheduleApi";
import { profileApi } from "./profileApi";
import { progressApi } from "./progressApi";
import { aiApi } from "./aiApi";
import { quizApi } from "./quizApi";
import { classroomApi } from "./classroomApi";
import awardApi from "./awardApi";

export { authApi, courseApi, scheduleApi, profileApi, progressApi, aiApi, quizApi, classroomApi, awardApi };

// Default export for backward compatibility
export default {
  ...authApi,
  ...courseApi,
  ...scheduleApi,
  ...profileApi,
  ...progressApi,
  ...aiApi,
  ...classroomApi,
  ...awardApi,
};
