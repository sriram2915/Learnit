# API Services (client)

This folder holds the client-side wrappers for every backend endpoint. All calls are **relative** (`/api/...`) and flow through `http.js`, which adds `Authorization: Bearer <token>` when present, logs requests/responses to the console in dev, parses JSON automatically, and throws on non-2xx responses with `error.status` and `error.data` attached.

## Files

- `http.js` — thin fetch wrapper (relative base, JSON parse, console logging, error bubbling).
- `authApi.js` — login/register/logout.
- `courseApi.js` — courses, modules/submodules, external links, study sessions, stats, active-time tracking.
- `scheduleApi.js` — calendar CRUD, auto-schedule, link/unlink modules.
- `profileApi.js` — profile info, preferences, password change.
- `progressApi.js` — progress dashboard data.
- `aiApi.js` — AI chat, course draft, schedule/progress insights, friend compare, and friend list CRUD.
- `index.js` — central export (named exports preferred).

## Usage patterns

```javascript
// Prefer named imports from the barrel
import { courseApi, scheduleApi } from "../../services";

// Handle errors to surface user-friendly messages
try {
  const courses = await courseApi.getCourses({ search: "python" });
} catch (err) {
  toast.error(err.message);
}
```

Token flow: `AuthContext` stores the JWT in `localStorage`. `http.js` reads it per request and injects headers. 401s mean expired/invalid token; clear storage and re-login.

## Endpoint guides

### authApi

- `login(email, password)` → `{ token, user? }`. Persist token in context/localStorage. Backend returns only `token` currently.
- `register(fullName, email, password)` → `{ message }` (no token issued). You may auto-login by calling `login` after success.
- `logout()` → best-effort POST; primarily clear local token on the client even if the call fails (stateless JWT).

### courseApi

- `getCourses(filters)` → list with progress snapshots. Filters: `search`, `priority`, `difficulty`, `duration`, `sortBy`, `sortOrder`.
- `getCourse(id)` → course with modules, submodules, external links, and any active session.
- `createCourse(courseDto)` → creates course with nested `modules` (each with `subModules`) and `externalLinks`. Orders preserved by array position.
- `updateCourse(id, partial)` → partial update of course fields (title/description/subjectArea/learningObjectives/difficulty/priority/totalEstimatedHours/targetCompletionDate/notes).
- `deleteCourse(id)` → removes course, modules, submodules, and linked schedule events.
- `getCourseStats()` → small summary of active courses/weekly focus/next milestone.

**Modules & submodules**

- `createModule(courseId, payload)` → add module (or submodule when `parentModuleId` is supplied in payload).
- `updateModule(moduleId, updates)` → updates module or submodule matching `moduleId` (API resolves type server-side).
- `toggleModuleCompletion(moduleId)` → flips completion; returns updated completion and hours remaining.

**External links**

- `addExternalLink(courseId, link)` → `{ id, platform, title, url, createdAt }`.
- `updateExternalLink(linkId, updates)`.
- `deleteExternalLink(linkId)`.

**Study sessions & time**

- `startStudySession(courseId, moduleId?)` → starts an active session (one at a time per course).
- `stopStudySession(sessionId, notes)` → stops, records duration, updates activity log and course `lastStudiedAt`.
- `getCourseSessions(courseId)` → history ordered by start time.
- `updateCourseActiveTime(courseId, hours)` → adjusts active in-flight time block and recomputes `hoursRemaining`.

### scheduleApi

- `getScheduleEvents({ from, to }?)` → events with optional date range (UTC on the wire).
- `createScheduleEvent(event)` / `updateScheduleEvent(id, event)` / `deleteScheduleEvent(id)` — basic CRUD; `event` includes `title`, `startUtc`, `endUtc`, `allDay`, optional `courseModuleId`.
- `resetSchedule()` → deletes all events for the user.
- `getAvailableModules()` → modules not yet scheduled.
- `autoScheduleModules(options)` → server places modules into work windows. Options: `startDateTime`, `preferredStartHour`, `preferredEndHour`, `includeWeekends`, `maxDailyHours`, `maxSessionMinutes`, `bufferMinutes`, `weeklyLimitHours`, `timezoneOffsetMinutes` (browser `getTimezoneOffset()`), `courseOrderIds` (priority ordering). Response includes created events and applied limits.
- `linkEventToModule(eventId, moduleId)` / `unlinkEventFromModule(eventId)` — attaches/detaches module and adjusts title.

### profileApi

- `getProfile()` → `{ profile, preferences }`.
- `updateProfile(profileData)` → update name/email (server checks uniqueness).
- `updatePreferences(preferencesData)` → study speed, max session minutes, weekly limit, dark mode flag.
- `changePassword({ currentPassword, newPassword, confirmPassword })` → server verifies current password and length.

### progressApi

- `getProgressDashboard()` → weekly scheduled/completed hours, streaks, efficiency, per-course progress, and 60-day heatmap array. Shape matches Progress dashboard UI directly.

### aiApi

- `chat({ message, history? })` → AI chat reply (short action-focused bullets; server builds context).
- `createCourse(prompt)` → AI course draft JSON; UI hydrates course modal fields.
- `scheduleInsights(prompt)` → concise scheduling suggestions using user context.
- `progressInsights(prompt)` → concise progress insights/next steps.
- `compareFriends(friendIds)` → compare current user vs up to 2 friends.
- Friend helpers (reuse same backend): `listFriends()`, `addFriend(friendDto)`, `deleteFriend(id)`.

## Error handling

Every method throws on non-2xx. Catch and inspect `error.status` and `error.data` for server messages. Typical patterns:

```javascript
try {
  await scheduleApi.autoScheduleModules({
    preferredStartHour: 9,
    preferredEndHour: 17,
  });
} catch (error) {
  if (error.status === 401) {
    logout();
  }
  setToast(error.message);
}
```

## Adding or extending endpoints

1. Add a method in the appropriate `*Api.js` that calls `http.get/post/put/patch/delete` with a **relative** path.
2. Export it from `index.js` (named export and default spread provided).
3. Keep request/response shapes close to backend contracts; pass plain objects (the http client JSON-stringifies bodies).
4. Logically group endpoints by domain—avoid a grab-bag file.

## Best practices

- Prefer named imports over the default export to avoid pulling unused modules.
- Keep calls in `try/catch`; surface friendly messages and handle 401 by clearing auth state.
- Pass dates to scheduling endpoints in UTC or include `timezoneOffsetMinutes` when auto-scheduling.
- Do not hard-code hosts; rely on the Vite proxy and keep paths relative.
