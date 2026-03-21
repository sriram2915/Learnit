# Learnit Client (React + Vite)

Front-end for Learnit, a personal learning planner. The SPA is built with Vite + React, React Router, and CSS modules. All API calls are relative (`/api/*`) and proxied to the ASP.NET Core backend; never hard-code hosts.

## Quick start

Prereqs: Node.js 18+, backend running over HTTPS through Vite proxy (`/api`).

```
npm install
npm run dev
```

Helpful commands: `npm run dev:chrome` opens the dev URL; `npm run lint` runs ESLint; `npm run build && npm run preview` serves a production bundle.

## Architecture

- Routing/layouts: `src/router.jsx` defines public landing (`/`), auth (`/auth/*`), and protected app (`/app/*`). `RequireAuth` guards protected routes and redirects to `/auth/login` when the token is missing/expired. Layout shells live in `src/layouts`.
- State/auth: `AuthContext` stores the JWT (`localStorage`), user, and login/logout helpers. Refresh-less auth: the token is simply added to headers; logout clears storage.
- Data access: `src/services/http.js` wraps `fetch` with JSON parsing, error bubbling, and console logging. Domain APIs (`authApi`, `courseApi`, `scheduleApi`, `profileApi`, `progressApi`, `aiApi`) build on it with semantic methods and relative endpoints.
- UI composition: pages in `src/components/main/*` orchestrate data fetching and pass props into presentational pieces in `components/course/*`, `components/auth/*`, etc. CSS modules live beside components; global resets in `App.css`/`index.css`.
- Assets/build: Vite handles bundling, Fast Refresh, and proxying `/api` to the ASP.NET backend. No absolute URLs should be used in client code.

## Data flow and auth

1. User hits `/auth/login` → `authApi.login` posts credentials → server returns JWT → `AuthContext` saves token to `localStorage` and React state.
2. Any subsequent request uses `http.js` which injects `Authorization: Bearer <token>` if present; 401s typically indicate expired/cleared tokens.
3. Navigation to `/app/*` is blocked by `RequireAuth`; missing token triggers redirect to login.
4. Dates/times shown in the UI come from UTC values the API returns; the schedule view may offset to the browser locale.

## Features (with technical notes)

- Authentication: Signup/login via `/api/auth`; logout calls the API but primarily clears local token. Guarding is client-side; backend enforces JWT.
- Courses: CRUD against `/api/courses`. Create/edit payloads include nested `modules` and `subModules` with `order`, `estimatedHours`, `isCompleted`, and `externalLinks`. Progress cards show `hoursRemaining` derived from API snapshots. Module toggles call `PATCH /api/courses/modules/{id}/toggle-completion`. Study sessions: start/stop via `/api/courses/{courseId}/sessions/*` to track time.
- AI drafting: In the create/edit modal, “Ask AI” posts a freeform prompt to `/api/ai/create-course`. The response is JSON with course + modules; UI hydrates form fields. `http.js` logs the raw response in dev to help diagnose malformed JSON.
- Scheduling: Calendar page fetches `/api/schedule` (optionally with `from/to`). Create/update/delete map to POST/PUT/DELETE. Auto-scheduler posts preferences (work window, timezone offset, daily/weekly caps, course priority order) to `/api/schedule/auto-schedule`, which returns created events. Link/unlink endpoints rename titles when tied to modules.
- Progress: Dashboard pulls `/api/progress/dashboard`, which returns weekly scheduled vs completed hours, streaks, per-course percentages, and a 60-day heatmap array. Charts (e.g., Recharts) render directly from that shape.
- Profile: `/api/profile` read; `/api/profile/info` for name/email; `/api/profile/preferences` for study limits/theme; `/api/profile/password` for password changes with server-side hash verification.
- AI assistant: `/api/ai/chat` uses context built on the server; `/api/ai/schedule-insights` and `/api/ai/progress-insights` request concise bullets; `/api/ai/compare` benchmarks against selected friends. Friend list CRUD uses `/api/friends`.

## Project layout (selected)

- `src/router.jsx` — route tree and auth gate wiring.
- `src/context/AuthContext.jsx` — token/user state and login/logout helpers.
- `src/services/http.js` — request wrapper; everything else calls through here.
- `src/services/*Api.js` — domain-specific methods mapping 1:1 with backend endpoints.
- `src/components/main/*` — top-level pages: Course, Schedule, Progress, Profile, Ai.
- `src/components/course/*` — course modals, module trees, details, cards.
- `src/components/auth/*` — signin/signup screens plus guard.

## Build, lint, and preview

- Development: `npm run dev` (HTTPS, proxied API).
- Lint: `npm run lint` (ESLint via Vite config).
- Production build: `npm run build` outputs to `dist`; `npm run preview` serves the built bundle locally.

## API usage tips

- Always call relative paths (`/api/...`); Vite proxy handles host/port and HTTPS dev certs.
- Handle errors: `http.js` throws on non-2xx and attaches `status` and `data`; catch in pages to surface UI messages.
- Auth renewal: there is no refresh token; re-login when expired.

## Troubleshooting

- Backend unreachable or 401s: confirm server is running and dev cert trusted; clear `localStorage` and re-login.
- AI draft fails: check dev console for the raw AI payload; backend may be stubbed if no API key is set.
- Calendar drift: times are UTC from the API; ensure browser timezone offset is passed when auto-scheduling.
- CORS errors: verify Vite dev origin matches the `AllowFrontend` list in the server.
