# Learnit: Personal Learning Planner

Learnit is a full-stack personal learning planner designed to help users organize, schedule, and track their learning goals. The project consists of a modern React + Vite front-end and a robust ASP.NET Core + Entity Framework Core back-end, with secure JWT-based authentication and AI-powered features.

---

## Table of Contents

- [Project Overview](#project-overview)
- [Architecture](#architecture)
- [Features](#features)
- [Setup & Installation](#setup--installation)
- [Configuration](#configuration)
- [Development Workflow](#development-workflow)
- [API Overview](#api-overview)
- [Troubleshooting](#troubleshooting)
- [Project Structure](#project-structure)

---

## Project Overview

Learnit helps users:

- Plan and track courses, modules, and study sessions
- Schedule learning events with auto-scheduling and calendar integration
- Monitor progress, streaks, and efficiency
- Collaborate and compare with friends
- Leverage AI to draft courses, generate insights, and assist with planning

All data is securely scoped per user, and all date/time values are handled in UTC.

---

## Architecture

### Backend (Learnit.Server)

- **Framework:** ASP.NET Core 9, Entity Framework Core, PostgreSQL
- **Security:** JWT Bearer authentication, all endpoints require valid tokens
- **Data Model:** Users, Courses, Modules, Submodules, External Links, Schedule Events, Study Sessions, Activity Logs, Friends, Achievements
- **AI Integration:** Supports Groq and OpenAI (with fallback to stub responses)
- **CORS:** Configured for local Vite dev origins
- **User Scoping:** All data and operations are per-user, enforced by claims

### Frontend (learnit.client)

- **Framework:** React 18+, Vite, React Router
- **State Management:** Context API for auth and user state
- **Styling:** CSS Modules, global resets
- **API Access:** All calls are relative (`/api/*`), proxied via Vite to the backend
- **Auth:** JWT stored in localStorage, injected into requests
- **UI Composition:** Modular, with clear separation between pages, components, and services

---

## Features

### Authentication

- Register, login, and logout (JWT-based)
- Passwords securely hashed
- All API endpoints require authentication

### Courses & Modules

- CRUD for courses, modules, and submodules
- Nested structure with ordering, estimated hours, completion toggles
- External links per module
- Progress tracking (scheduled/completed hours, hours remaining)
- Study sessions: start, stop, and log time

### Scheduling

- Calendar view with CRUD for events
- Auto-scheduler: generates events based on user preferences (work window, timezone, caps, priorities)
- Module linking/unlinking to events
- Respects user time constraints and avoids overlaps

### Progress & Analytics

- Dashboard: weekly scheduled vs completed hours, streaks, per-course progress, 60-day heatmap
- Efficiency and focus summaries

### Profile Management

- View and update profile info (name, email, preferences)
- Change password with verification
- Study preferences: speed, session limits, weekly caps, theme

### Friends & Social

- Add, list, and remove friends by email
- Compare stats (completion rate, weekly hours)
- AI-powered benchmarking and suggestions

### AI Assistant

- Draft courses from prompts
- Generate schedule and progress insights
- Chat for action-oriented learning suggestions
- Robust fallback for missing/expired API keys

---

## Setup & Installation

### Prerequisites

- **Backend:** .NET 9 SDK, PostgreSQL
- **Frontend:** Node.js 18+

### Backend Setup

1. **Restore and build:**
   ```
   dotnet restore && dotnet build
   ```
2. **Apply migrations:**
   ```
   dotnet ef database update
   ```
3. **Run the server:**
   ```
   dotnet run --project Learnit.Server/Server.csproj
   ```

### Frontend Setup

1. **Install dependencies:**
   ```
   npm install
   ```
2. **Start the dev server:**
   ```
   npm run dev
   ```
   - Use `npm run dev:chrome` to open in Chrome
   - Use `npm run lint` to check code style
   - Use `npm run build && npm run preview` to serve a production build

---

## Configuration

### Backend

- **Secrets:** Do not store secrets in `appsettings.json`. Use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for local dev or environment variables in production.
- **Required settings:**
  - `ConnectionStrings:Default` (PostgreSQL connection string)
  - `Jwt:Key` (JWT signing key)
  - `Groq:ApiKey` and/or `OpenAi:ApiKey` (for AI features)
- **Example:**
  ```json
  {
    "ConnectionStrings": {
      "Default": "Host=localhost;Database=learnit;Username=postgres;Password=yourpassword"
    },
    "Jwt": {
      "Key": "<strong-secret>",
      "Issuer": "learnit",
      "Audience": "learnit"
    },
    "Groq": {
      "ApiKey": "<your-groq-key>",
      "Model": "llama-3.1-8b-instant"
    },
    "OpenAi": {
      "ApiKey": "<optional-openai-key>",
      "Model": "gpt-4o-mini"
    }
  }
  ```
- **Set secrets:**
  ```
  dotnet user-secrets set "ConnectionStrings:Default" "..."
  dotnet user-secrets set "Jwt:Key" "..."
  ```

### Frontend

- No hard-coded API hosts; all requests are relative and proxied by Vite
- Ensure HTTPS dev certs are trusted for local API access

---

## Development Workflow

- **Backend:**
  - All controllers are `[Authorize]` by default
  - Data is always scoped to the authenticated user
  - UTC is enforced for all date/time values
  - AI endpoints gracefully degrade if keys are missing or quota is exceeded
- **Frontend:**
  - AuthContext manages JWT and user state
  - All API calls go through a central HTTP service
  - Protected routes are guarded by `RequireAuth`
  - UI is modular and easy to extend

---

## API Overview

- **Auth:** `/api/auth` (register, login, logout)
- **Courses:** `/api/courses` (CRUD, modules, submodules, external links, study sessions, stats)
- **Schedule:** `/api/schedule` (CRUD, auto-schedule, link/unlink modules, reset, available modules)
- **Progress:** `/api/progress/dashboard` (weekly stats, streaks, heatmap)
- **Profile:** `/api/profile` (info, preferences, password)
- **Friends:** `/api/friends` (CRUD, stats)
- **AI:** `/api/ai/*` (chat, create-course, schedule/progress insights, compare)

All endpoints require a valid JWT in the `Authorization: Bearer` header.

---

## Troubleshooting

- **Backend unreachable or 401s:**
  - Ensure the server is running and HTTPS dev certs are trusted
  - Clear `localStorage` and re-login on the client
- **DB connectivity:**
  - Check connection string and run migrations if schema mismatches
- **CORS errors:**
  - Vite dev origin must match `AllowFrontend` in server config
- **AI draft fails:**
  - Check for valid API keys; stub responses are used if missing
- **Calendar drift:**
  - All times are UTC; ensure browser timezone offset is passed when auto-scheduling

---

## Project Structure

```
learnit.client/           # React + Vite front-end
  src/
    components/          # UI components
    context/             # Auth and global state
    hooks/               # Custom React hooks
    layouts/             # Layout shells
    services/            # API wrappers
    utils/               # Utility functions
  App.jsx, main.jsx, ... # Entry points
  index.html, App.css, ...

Learnit.Server/          # ASP.NET Core + EF Core back-end
  Controllers/           # API controllers
  Data/                  # DbContext and migrations
  Models/                # Entity and DTO classes
  Services/              # Business logic and helpers
  Properties/            # Launch settings
  appsettings.json       # Configuration (no secrets)
  Program.cs, ...        # Startup and main entry
```

---
