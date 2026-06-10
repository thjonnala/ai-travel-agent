# Smart AI Travel Agent

Describe a trip in natural language ("5 relaxed days in Lisbon in October, mid-range budget, into food and history") and get a personalized, editable itinerary with day-by-day plans, lodging and activity suggestions, and a running cost estimate. Chat to refine the plan, save trips, and revisit them later.

> Itineraries are AI-generated suggestions — always verify details (opening hours, prices, availability) before booking.

**Demo mode:** authentication is currently skipped — every request runs as a single auto-provisioned demo user (`ICurrentUserService` keeps all queries user-scoped so Entra ID can slot in later). If no Azure OpenAI settings are configured, a deterministic **mock planner** is used so the app works end-to-end without keys; add the `AzureOpenAI` settings (below) to switch to real AI planning automatically.

## Architecture

| Layer | Technology |
|---|---|
| Frontend | React 18 + TypeScript, Vite, React Router, TanStack Query, Tailwind CSS, shadcn/ui |
| API | .NET 10 controller-based Web API (clean architecture: Api → Application → Domain ← Infrastructure) |
| Database | Azure SQL (LocalDB for local dev) via EF Core 10, code-first migrations |
| AI | Azure OpenAI behind an `IAiPlannerService` abstraction (provider-swappable) |
| Auth | Microsoft Entra ID (External ID) with JWT bearer tokens |
| Hosting | Azure App Service (API) + Azure Static Web Apps (frontend) + Azure SQL |

Repo layout:

```
backend/   .NET solution (src/ = Api, Application, Domain, Infrastructure; tests/)
frontend/  React + Vite app
infra/     Bicep templates (stretch, not yet present)
```

## Prerequisites

- .NET 10 SDK
- Node.js 20+
- SQL Server LocalDB (ships with Visual Studio) **or** an Azure SQL database

## Run locally

### Backend

```powershell
# from the repo root — restores the dotnet-ef local tool
dotnet tool restore

# create/update the database (LocalDB by default, see appsettings.Development.json)
dotnet ef database update --project backend/src/TravelAgent.Infrastructure --startup-project backend/src/TravelAgent.Api

# run the API → http://localhost:5054 (Swagger UI at /swagger in Development)
dotnet run --project backend/src/TravelAgent.Api
```

To use Azure SQL instead of LocalDB, override the connection string without
touching source-controlled files:

```powershell
dotnet user-secrets set "ConnectionStrings:TravelAgentDb" "<azure-sql-connection-string>" --project backend/src/TravelAgent.Api
```

### Frontend

```powershell
cd frontend
npm install
npm run dev   # http://localhost:5173, /api/* proxied to the backend
```

## Configuration

No secrets live in source control. Local development uses .NET user secrets;
deployed environments read from App Service configuration / Azure Key Vault.

| Setting | Where | Purpose |
|---|---|---|
| `ConnectionStrings:TravelAgentDb` | user secrets / Key Vault | SQL connection string |
| `AzureOpenAI:Endpoint`, `AzureOpenAI:ApiKey`, `AzureOpenAI:Deployment` | user secrets / Key Vault | AI planner — leave empty to use the mock planner |
| `Entra:*` | appsettings + user secrets | JWT bearer auth (deferred for demo) |
| `VITE_*` | `frontend/.env.local` (copy from `.env.example`) | SPA auth + API base URL |

## Database migrations

```powershell
# add a migration after changing entities/configurations
dotnet ef migrations add <Name> --project backend/src/TravelAgent.Infrastructure --startup-project backend/src/TravelAgent.Api --output-dir Persistence/Migrations

# apply
dotnet ef database update --project backend/src/TravelAgent.Infrastructure --startup-project backend/src/TravelAgent.Api
```

## Tests

```powershell
dotnet test backend/TravelAgent.slnx   # backend unit + integration tests
cd frontend; npm test                  # frontend component tests (coming with the UI milestones)
```

## Deployment (outline)

1. Provision Azure SQL, App Service (API), Static Web App (frontend), Azure OpenAI, and Key Vault (Bicep templates planned under `infra/`).
2. Point the App Service at Key Vault for the connection string and OpenAI keys; run `dotnet ef database update` (or migration bundles) against Azure SQL from CI.
3. Build the frontend with `VITE_API_BASE_URL` set to the API origin and deploy `frontend/dist` to the Static Web App; lock API CORS to that origin.

## Milestone status

- [x] 1 — Solution scaffold, EF Core model + initial migration (LocalDB verified)
- [ ] 2 — Entra ID auth (API + frontend login) — *skipped for demo; demo user + mock planner instead*
- [x] 3 — `IAiPlannerService` + Azure OpenAI structured outputs + `POST /api/trips/plan`
- [x] 4 — Trip persistence + list/get/update/delete/duplicate endpoints
- [x] 5 — Frontend: dashboard, planner chat, itinerary rendering
- [x] 6 — Refinement chat + conversation memory (trimmed history window)
- [x] 7 — Preferences profile feeding prompts
- [ ] 8 — Cost summary polish, PDF export, deployment docs (cost breakdown per day already shown)
