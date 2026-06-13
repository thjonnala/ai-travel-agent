# Smart AI Travel Agent

Describe a trip in natural language ("5 relaxed days in Lisbon in October, mid-range budget, into food and history") and get a personalized, editable itinerary with day-by-day plans, lodging and activity suggestions, and a running cost estimate. Chat to refine the plan, save trips, and revisit them later.

> Itineraries are AI-generated suggestions — always verify details (opening hours, prices, availability) before booking.

**Open-source / free-tier stack.** No auth (every request runs as one auto-provisioned demo user; `ICurrentUserService` keeps all queries user-scoped so real auth can slot in later). The AI planner talks to any OpenAI-compatible provider — **Groq** (free, open-weight models) by default. If no `Ai` settings are configured, a deterministic **mock planner** keeps the app fully working without any keys.

## Architecture

| Layer | Technology |
|---|---|
| Frontend | React 19 + TypeScript, Vite, React Router, TanStack Query, Tailwind CSS |
| API | .NET 10 controller-based Web API (clean architecture: Api → Application → Domain ← Infrastructure) |
| Database | PostgreSQL via EF Core 10 (Npgsql), code-first migrations |
| AI | OpenAI-compatible provider behind an `IAiPlannerService` abstraction — Groq (open models) by default; mock fallback when unconfigured |
| Hosting | GitHub Pages (frontend) + Render (API as a Docker web service) + Render PostgreSQL |
| CI/CD | GitHub Actions (Pages deploy + backend tests); Render auto-deploys the API from `main` |

Repo layout:

```
backend/   .NET solution (src/ = Api, Application, Domain, Infrastructure; tests/) + Dockerfile
frontend/  React + Vite app (deployed to GitHub Pages)
designs/   architecture & design diagrams
render.yaml          Render blueprint (API web service + free PostgreSQL)
.github/workflows/   Pages deploy + backend CI
```

## Prerequisites

- .NET 10 SDK
- Node.js 20+
- PostgreSQL 14+ (local) — or just rely on the mock planner and a local Postgres for data

## Run locally

### Backend

```powershell
# create a local database (matches appsettings.Development.json)
createdb travelagent   # or: psql -U postgres -c "CREATE DATABASE travelagent;"

# run the API → http://localhost:5054 (Swagger UI at /swagger in Development).
# Migrations are applied automatically on startup.
dotnet run --project backend/src/TravelAgent.Api
```

The default local connection string (`appsettings.Development.json`) is
`Host=localhost;Port=5432;Database=travelagent;Username=postgres;Password=postgres` —
override it with user secrets if your Postgres differs:

```powershell
dotnet user-secrets set "ConnectionStrings:TravelAgentDb" "<npgsql-connection-string>" --project backend/src/TravelAgent.Api
```

To use real AI locally, set a free [Groq](https://console.groq.com) key:

```powershell
dotnet user-secrets set "Ai:ApiKey" "<groq-api-key>" --project backend/src/TravelAgent.Api
```

### Frontend

```powershell
cd frontend
npm install
npm run dev   # http://localhost:5173, /api/* proxied to the backend
```

## Configuration

No secrets live in source control. Local dev uses .NET user secrets; on Render
they are environment variables (the Groq key is set in the dashboard).

| Setting | Where | Purpose |
|---|---|---|
| `ConnectionStrings__TravelAgentDb` | user secrets / Render env | PostgreSQL connection string (accepts Npgsql key-value **or** a `postgres://` URL) |
| `Ai__Endpoint` | appsettings / Render env | OpenAI-compatible base URL (default `https://api.groq.com/openai/v1`) |
| `Ai__Model` | appsettings / Render env | Model id (default `llama-3.3-70b-versatile`) |
| `Ai__ApiKey` | user secrets / Render env | Provider key — leave empty to use the mock planner |
| `Cors__AllowedOrigins` | Render env | Comma-separated allowed frontend origins |
| `VITE_API_BASE_URL` | `frontend/.env.local` / GitHub repo variable | Absolute API origin for the deployed SPA |

## Database migrations

```powershell
dotnet ef migrations add <Name> --project backend/src/TravelAgent.Infrastructure --startup-project backend/src/TravelAgent.Api --output-dir Persistence/Migrations
# applied automatically on API startup; or manually:
dotnet ef database update --project backend/src/TravelAgent.Infrastructure --startup-project backend/src/TravelAgent.Api
```

## Tests

```powershell
dotnet test backend/TravelAgent.slnx   # 51 tests; integration tests use SQLite in-memory
```

## Deployment

**Frontend → GitHub Pages** (automated by `.github/workflows/deploy-pages.yml`):

1. Push to `main`; the workflow builds the SPA and publishes it to Pages.
2. In repo **Settings → Pages**, set Source = "GitHub Actions".
3. Set repo **variable** `VITE_API_BASE_URL` to the Render API origin so the SPA calls the right backend.
4. Custom domain: `frontend/public/CNAME` already contains `aitravelagent.thiruapps.com`. Point a DNS `CNAME` record for `aitravelagent` at `thjonnala.github.io`, then enable "Enforce HTTPS" in Settings → Pages (GitHub provisions the certificate automatically).

**Backend + database → Render** (`render.yaml` blueprint):

1. In Render: **New → Blueprint**, connect this GitHub repo. It creates the Docker web service and a free PostgreSQL database, wiring the connection string automatically.
2. Set the `Ai__ApiKey` secret (your Groq key) in the service's Environment settings.
3. Migrations apply on startup; once live, the API serves at `https://ai-travel-agent-api.onrender.com` (use that for `VITE_API_BASE_URL` above and confirm it's in `Cors__AllowedOrigins`).
