#!/usr/bin/env python3
"""Push the Smart AI Travel Agent backlog (Epics > Features > User Stories) to Azure DevOps.

Usage:
    ADO_PAT=<pat-with-work-items-read-write> python backlog/push_to_ado.py

Idempotent: skips any work item whose (type, title) already exists in the project.
"""

import base64
import json
import os
import sys
import urllib.parse
import urllib.request

ORG = "thjonnala"
PROJECT = "smart-ai-travel-agent"
BASE = f"https://dev.azure.com/{ORG}/{PROJECT}/_apis"
API = "api-version=7.1"

# ---------------------------------------------------------------------------
# Backlog definition (Agile process: Epic > Feature > User Story)
# ---------------------------------------------------------------------------

BACKLOG = [
    {
        "type": "Epic",
        "title": "AI-Powered Trip Planning Experience",
        "description": (
            "Travelers describe a trip in natural language and receive a personalized, editable, "
            "day-by-day itinerary with lodging and activity suggestions and a running cost estimate. "
            "The plan is refined through conversational chat and informed by a saved traveler "
            "preference profile. Covers milestones 3, 5, 6 and 7 (delivered in demo) of the roadmap."
        ),
        "tags": "AI; Planning; UX",
        "priority": 1,
        "children": [
            {
                "type": "Feature",
                "title": "Natural-Language Trip Planning (AI Planner)",
                "description": (
                    "Turn a free-text trip description (e.g. '5 relaxed days in Lisbon in October, "
                    "mid-range budget, into food and history') into a structured, validated, day-by-day "
                    "itinerary via Azure OpenAI behind the IAiPlannerService abstraction, with a "
                    "deterministic mock planner fallback for keyless demo/dev."
                ),
                "tags": "AI; Backend; Delivered",
                "priority": 1,
                "children": [
                    {
                        "type": "User Story",
                        "title": "Generate an itinerary from a natural-language trip description",
                        "description": (
                            "As a traveler, I want to describe my trip in plain language — destination, "
                            "duration, dates, budget and interests — so that I get a personalized "
                            "day-by-day itinerary without filling out forms."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>POST /api/trips/plan accepts a free-text prompt and returns a structured itinerary draft.</li>"
                            "<li>The itinerary contains destination, date range, and per-day plans with activities and lodging suggestions.</li>"
                            "<li>The draft passes ItineraryValidator checks (dates consistent, days contiguous, required fields present) before being returned.</li>"
                            "<li>Invalid or unplannable prompts return a clear, actionable error message.</li>"
                            "</ul>"
                        ),
                        "points": 8,
                        "tags": "AI; API; Delivered",
                    },
                    {
                        "type": "User Story",
                        "title": "Constrain AI output to a structured itinerary schema",
                        "description": (
                            "As the system, I want Azure OpenAI responses constrained to a JSON schema "
                            "(structured outputs) so that every generated itinerary parses reliably into "
                            "domain objects."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>AzureOpenAiPlannerService requests structured outputs using ItinerarySchema.</li>"
                            "<li>Malformed or schema-violating responses are rejected and surfaced as planner errors, never persisted.</li>"
                            "<li>The planner sits behind IAiPlannerService so the AI provider is swappable.</li>"
                            "</ul>"
                        ),
                        "points": 5,
                        "tags": "AI; Backend; Delivered",
                    },
                    {
                        "type": "User Story",
                        "title": "Fall back to a deterministic mock planner when no AI keys are configured",
                        "description": (
                            "As a developer or demo user, I want a deterministic mock planner used "
                            "automatically when AzureOpenAI settings are absent so that the app works "
                            "end-to-end without secrets."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>When AzureOpenAI endpoint/key/deployment are empty, MockAiPlannerService is registered instead of the real planner.</li>"
                            "<li>Mock output is deterministic and schema-valid, covered by unit tests.</li>"
                            "<li>Adding the AzureOpenAI settings switches to real AI planning with no code change.</li>"
                            "</ul>"
                        ),
                        "points": 3,
                        "tags": "AI; DX; Delivered",
                    },
                    {
                        "type": "User Story",
                        "title": "Show estimated costs per day and a trip total",
                        "description": (
                            "As a traveler, I want estimated costs on itinerary items, per-day subtotals "
                            "and a trip total so that I can judge affordability at a glance."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>Each itinerary item carries a cost estimate; each day shows a subtotal; the trip shows a running total.</li>"
                            "<li>Currency is displayed consistently.</li>"
                            "<li>A disclaimer notes that costs are AI-generated estimates to verify before booking.</li>"
                            "</ul>"
                        ),
                        "points": 3,
                        "tags": "AI; UX; Delivered",
                    },
                ],
            },
            {
                "type": "Feature",
                "title": "Conversational Refinement & Memory",
                "description": (
                    "Let travelers refine a generated itinerary through chat, with conversation memory "
                    "(trimmed history window) so follow-up requests are coherent. Chat messages are "
                    "persisted per trip."
                ),
                "tags": "AI; Chat; Delivered",
                "priority": 1,
                "children": [
                    {
                        "type": "User Story",
                        "title": "Refine an itinerary through chat",
                        "description": (
                            "As a traveler, I want to ask for changes in chat (e.g. 'make day 2 more "
                            "relaxed', 'swap the museum for a food tour') so that the itinerary updates "
                            "without starting over."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>A chat message triggers a re-plan that uses the existing itinerary as context.</li>"
                            "<li>The updated itinerary replaces the previous version and renders inline in the planner UI.</li>"
                            "<li>Unrelated parts of the plan are preserved across refinements.</li>"
                            "</ul>"
                        ),
                        "points": 8,
                        "tags": "AI; Chat; Delivered",
                    },
                    {
                        "type": "User Story",
                        "title": "Maintain conversation memory with a trimmed history window",
                        "description": (
                            "As a traveler, I want the assistant to remember earlier requests in the "
                            "conversation so that successive refinements build on each other."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>ChatMessage entities are persisted per trip in order.</li>"
                            "<li>A trimmed window of recent history is included in each planning prompt to bound token usage.</li>"
                            "<li>Reopening a trip restores its conversation history.</li>"
                            "</ul>"
                        ),
                        "points": 5,
                        "tags": "AI; Backend; Delivered",
                    },
                ],
            },
            {
                "type": "Feature",
                "title": "Traveler Preferences Profile",
                "description": (
                    "A per-user preferences profile (pace, budget level, interests, dietary needs) that "
                    "is stored once and automatically feeds every planning prompt."
                ),
                "tags": "Personalization; Delivered",
                "priority": 2,
                "children": [
                    {
                        "type": "User Story",
                        "title": "Manage my travel preferences",
                        "description": (
                            "As a traveler, I want to save my travel preferences (pace, budget level, "
                            "interests, dietary needs) so that I don't have to repeat them for every trip."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>GET and PUT /api/preferences read and update the current user's TravelerPreference profile.</li>"
                            "<li>Preferences are user-scoped and persisted in the database.</li>"
                            "<li>The frontend offers a preferences page/form with sensible defaults.</li>"
                            "</ul>"
                        ),
                        "points": 5,
                        "tags": "Personalization; API; Delivered",
                    },
                    {
                        "type": "User Story",
                        "title": "Apply saved preferences to new trip plans automatically",
                        "description": (
                            "As a traveler, I want my saved preferences automatically included in planning "
                            "prompts so that new itineraries match my style by default."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>The planner prompt incorporates the stored preference profile.</li>"
                            "<li>Explicit instructions in the trip description override stored preferences.</li>"
                            "</ul>"
                        ),
                        "points": 3,
                        "tags": "Personalization; AI; Delivered",
                    },
                ],
            },
            {
                "type": "Feature",
                "title": "Planner UI: Dashboard, Chat & Itinerary Rendering",
                "description": (
                    "React 18 + TypeScript SPA (Vite, React Router, TanStack Query, Tailwind, shadcn/ui) "
                    "with a trips dashboard, a planner chat surface, and rich day-by-day itinerary "
                    "rendering with cost breakdowns."
                ),
                "tags": "Frontend; UX; Delivered",
                "priority": 1,
                "children": [
                    {
                        "type": "User Story",
                        "title": "Browse my saved trips on a dashboard",
                        "description": (
                            "As a traveler, I want a dashboard listing my saved trips so that I can "
                            "revisit, resume, duplicate or remove them."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>Dashboard lists trips with destination, dates and status.</li>"
                            "<li>Each trip offers open, duplicate and delete actions.</li>"
                            "<li>An empty state invites the user to plan their first trip.</li>"
                            "</ul>"
                        ),
                        "points": 5,
                        "tags": "Frontend; Delivered",
                    },
                    {
                        "type": "User Story",
                        "title": "Plan and refine trips in a chat interface",
                        "description": (
                            "As a traveler, I want a chat-style planner page so that describing and "
                            "refining a trip feels like a conversation."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>Chat input submits planning/refinement prompts and shows assistant responses.</li>"
                            "<li>Conversation history for the trip is visible and scrollable.</li>"
                            "<li>Loading and error states are handled gracefully.</li>"
                            "</ul>"
                        ),
                        "points": 8,
                        "tags": "Frontend; Chat; Delivered",
                    },
                    {
                        "type": "User Story",
                        "title": "View the itinerary day by day with a cost breakdown",
                        "description": (
                            "As a traveler, I want the itinerary rendered day by day with activities, "
                            "lodging and per-day costs so that the plan is easy to scan and trust."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>Each day shows its items in order with names, notes and cost estimates.</li>"
                            "<li>Per-day subtotals and the trip total are visible.</li>"
                            "<li>An 'AI-generated — verify before booking' disclaimer is displayed.</li>"
                            "</ul>"
                        ),
                        "points": 5,
                        "tags": "Frontend; UX; Delivered",
                    },
                ],
            },
        ],
    },
    {
        "type": "Epic",
        "title": "Trip Management & Data Platform",
        "description": (
            "Durable, user-scoped persistence of trips, itineraries and conversations in Azure SQL via "
            "EF Core 10 (code-first migrations), with full lifecycle APIs and itinerary export. Covers "
            "milestone 4 (delivered) and the export portion of milestone 8."
        ),
        "tags": "Backend; Data",
        "priority": 2,
        "children": [
            {
                "type": "Feature",
                "title": "Trip Persistence & Lifecycle APIs",
                "description": (
                    "REST endpoints to save, list, view, update, delete and duplicate trips, all scoped "
                    "to the current user via ICurrentUserService."
                ),
                "tags": "API; Backend; Delivered",
                "priority": 1,
                "children": [
                    {
                        "type": "User Story",
                        "title": "Save a planned trip and see it in my trip list",
                        "description": (
                            "As a traveler, I want my planned trip saved automatically so that I can "
                            "close the app and come back to it later."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>Planned trips persist with their full itinerary (days, items) and chat history.</li>"
                            "<li>GET /api/trips lists the current user's trips; GET /api/trips/{id} returns full detail.</li>"
                            "<li>Requesting another user's trip returns 404/403.</li>"
                            "</ul>"
                        ),
                        "points": 5,
                        "tags": "API; Delivered",
                    },
                    {
                        "type": "User Story",
                        "title": "Update or delete a saved trip",
                        "description": (
                            "As a traveler, I want to edit trip details or delete trips I no longer need "
                            "so that my dashboard stays relevant."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>PUT /api/trips/{id} updates editable fields; DELETE removes the trip and its children.</li>"
                            "<li>Both operations are restricted to the owning user.</li>"
                            "<li>Covered by integration tests (TripsApiTests).</li>"
                            "</ul>"
                        ),
                        "points": 3,
                        "tags": "API; Delivered",
                    },
                    {
                        "type": "User Story",
                        "title": "Duplicate an existing trip as a starting point",
                        "description": (
                            "As a traveler, I want to duplicate a past trip so that I can replan a "
                            "similar journey without starting from scratch."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>POST /api/trips/{id}/duplicate creates a deep copy (itinerary days and items) owned by the same user.</li>"
                            "<li>The copy is clearly distinguishable (e.g. title suffix) and independently editable.</li>"
                            "</ul>"
                        ),
                        "points": 3,
                        "tags": "API; Delivered",
                    },
                    {
                        "type": "User Story",
                        "title": "Keep all data access scoped to the current user",
                        "description": (
                            "As the system, I want every query filtered through ICurrentUserService so "
                            "that data stays isolated per user and real auth can slot in later without "
                            "rework."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>All trip/preference queries filter by the current user id.</li>"
                            "<li>Demo mode auto-provisions a single demo user via DemoCurrentUserService.</li>"
                            "<li>Swapping in an Entra-backed implementation requires no changes to application services.</li>"
                            "</ul>"
                        ),
                        "points": 3,
                        "tags": "Backend; Security; Delivered",
                    },
                ],
            },
            {
                "type": "Feature",
                "title": "Itinerary Export & Cost Summary Polish",
                "description": (
                    "Round out milestone 8: a polished trip cost summary and a downloadable PDF export "
                    "of the itinerary."
                ),
                "tags": "UX; Export",
                "priority": 3,
                "children": [
                    {
                        "type": "User Story",
                        "title": "Export my itinerary as a PDF",
                        "description": (
                            "As a traveler, I want to download my itinerary as a PDF so that I can share "
                            "it or use it offline while traveling."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>An export action produces a well-formatted PDF: trip header, day-by-day plan, costs, disclaimer.</li>"
                            "<li>Export works for any saved trip from the dashboard or trip view.</li>"
                            "</ul>"
                        ),
                        "points": 5,
                        "tags": "Export; Frontend",
                    },
                    {
                        "type": "User Story",
                        "title": "Polish the trip cost summary",
                        "description": (
                            "As a traveler, I want a clear cost summary view (per category, per day, "
                            "total) so that I can quickly understand where the budget goes."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>Cost summary aggregates by day and category with a trip total.</li>"
                            "<li>Summary stays in sync after chat refinements.</li>"
                            "</ul>"
                        ),
                        "points": 3,
                        "tags": "UX; Frontend",
                    },
                ],
            },
        ],
    },
    {
        "type": "Epic",
        "title": "Security, Identity & Production Readiness",
        "description": (
            "Replace demo-mode identity with Microsoft Entra ID, provision Azure infrastructure as code, "
            "and stand up CI/CD so the app runs securely in production (App Service + Static Web Apps + "
            "Azure SQL + Key Vault). Covers milestone 2 (deferred for demo) and the deployment portion "
            "of milestone 8."
        ),
        "tags": "Security; DevOps",
        "priority": 2,
        "children": [
            {
                "type": "Feature",
                "title": "Authentication with Microsoft Entra ID",
                "description": (
                    "End-user sign-in via Entra ID (External ID) in the SPA and JWT bearer validation in "
                    "the API, replacing the auto-provisioned demo user."
                ),
                "tags": "Security; Auth",
                "priority": 1,
                "children": [
                    {
                        "type": "User Story",
                        "title": "Sign in to the app with my Microsoft Entra identity",
                        "description": (
                            "As a traveler, I want to sign in with Entra ID so that my trips and "
                            "preferences are private to my account across devices."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>SPA implements the Entra External ID sign-in/sign-out flow (VITE_* settings from .env).</li>"
                            "<li>Access tokens are attached to API requests; expired sessions re-authenticate gracefully.</li>"
                            "</ul>"
                        ),
                        "points": 8,
                        "tags": "Auth; Frontend",
                    },
                    {
                        "type": "User Story",
                        "title": "Protect the API with JWT bearer authentication",
                        "description": (
                            "As the system, I want all API endpoints to require a valid Entra-issued JWT "
                            "so that only authenticated users reach trip data."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>JWT bearer middleware validates Entra tokens using Entra:* configuration.</li>"
                            "<li>ICurrentUserService resolves the user from token claims, auto-provisioning a User row on first sign-in.</li>"
                            "<li>Unauthenticated requests receive 401; demo-mode bypass is removed outside Development.</li>"
                            "</ul>"
                        ),
                        "points": 8,
                        "tags": "Auth; API",
                    },
                ],
            },
            {
                "type": "Feature",
                "title": "Azure Infrastructure as Code & Secrets Management",
                "description": (
                    "Bicep templates under infra/ for Azure SQL, App Service, Static Web Apps, Azure "
                    "OpenAI and Key Vault; all secrets resolved from Key Vault, none in source control."
                ),
                "tags": "Infra; DevOps",
                "priority": 2,
                "children": [
                    {
                        "type": "User Story",
                        "title": "Provision Azure resources with Bicep templates",
                        "description": (
                            "As an operator, I want repeatable Bicep templates for all Azure resources so "
                            "that environments can be created and rebuilt reliably."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>infra/ contains Bicep for Azure SQL, App Service (API), Static Web App (frontend), Azure OpenAI and Key Vault.</li>"
                            "<li>A single deployment command provisions a working environment.</li>"
                            "</ul>"
                        ),
                        "points": 8,
                        "tags": "Infra",
                    },
                    {
                        "type": "User Story",
                        "title": "Source all runtime secrets from Key Vault",
                        "description": (
                            "As an operator, I want the API to read its connection string and OpenAI keys "
                            "from Key Vault so that no secrets live in config files or pipelines."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>App Service configuration references Key Vault for ConnectionStrings:TravelAgentDb and AzureOpenAI:* settings.</li>"
                            "<li>Local development continues to use .NET user secrets.</li>"
                            "<li>API CORS is locked to the Static Web App origin.</li>"
                            "</ul>"
                        ),
                        "points": 5,
                        "tags": "Infra; Security",
                    },
                ],
            },
            {
                "type": "Feature",
                "title": "CI/CD Pipeline & Database Migrations",
                "description": (
                    "Automated build, test and deploy: backend tests, frontend build with "
                    "VITE_API_BASE_URL, EF Core migration bundles against Azure SQL, and deployment to "
                    "App Service / Static Web Apps."
                ),
                "tags": "DevOps; CI/CD",
                "priority": 2,
                "children": [
                    {
                        "type": "User Story",
                        "title": "Build and test on every push",
                        "description": (
                            "As a developer, I want CI to run backend unit/integration tests and the "
                            "frontend build/lint on every push so that regressions are caught early."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>Pipeline runs dotnet test on TravelAgent.slnx and npm build/lint for the frontend.</li>"
                            "<li>Failures block merge to main.</li>"
                            "</ul>"
                        ),
                        "points": 5,
                        "tags": "CI/CD",
                    },
                    {
                        "type": "User Story",
                        "title": "Apply EF Core migrations safely during deployment",
                        "description": (
                            "As an operator, I want database migrations applied automatically and safely "
                            "from CI so that schema changes ship with the code that needs them."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>CI produces an EF Core migration bundle and applies it to Azure SQL before app deployment.</li>"
                            "<li>Deployment fails fast (without swapping traffic) if the migration fails.</li>"
                            "</ul>"
                        ),
                        "points": 5,
                        "tags": "CI/CD; Data",
                    },
                    {
                        "type": "User Story",
                        "title": "Deploy API and frontend to Azure automatically",
                        "description": (
                            "As an operator, I want merged changes deployed to App Service (API) and "
                            "Static Web Apps (frontend) automatically so that releases are fast and "
                            "repeatable."
                        ),
                        "acceptance": (
                            "<ul>"
                            "<li>API deploys to Azure App Service; frontend builds with VITE_API_BASE_URL set to the API origin and deploys frontend/dist to Static Web Apps.</li>"
                            "<li>A smoke check verifies the deployed API responds after release.</li>"
                            "</ul>"
                        ),
                        "points": 5,
                        "tags": "CI/CD; Infra",
                    },
                ],
            },
        ],
    },
]

# ---------------------------------------------------------------------------
# ADO REST helpers
# ---------------------------------------------------------------------------


def auth_header(pat: str) -> str:
    token = base64.b64encode(f":{pat}".encode()).decode()
    return f"Basic {token}"


def request(method: str, url: str, pat: str, body=None, content_type="application/json"):
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", auth_header(pat))
    req.add_header("Content-Type", content_type)
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode())


def existing_items(pat: str) -> dict:
    """Map of (type, title) -> work item id for everything already in the project."""
    wiql = {
        "query": "SELECT [System.Id] FROM WorkItems "
                 "WHERE [System.TeamProject] = @project "
                 "AND [System.WorkItemType] IN ('Epic','Feature','User Story')"
    }
    result = request("POST", f"{BASE}/wit/wiql?{API}", pat, wiql)
    ids = [str(wi["id"]) for wi in result.get("workItems", [])]
    found = {}
    for i in range(0, len(ids), 200):
        batch = ",".join(ids[i:i + 200])
        if not batch:
            break
        items = request(
            "GET",
            f"{BASE}/wit/workitems?ids={batch}&fields=System.Title,System.WorkItemType&{API}",
            pat,
        )
        for wi in items["value"]:
            key = (wi["fields"]["System.WorkItemType"], wi["fields"]["System.Title"])
            found[key] = wi["id"]
    return found


def create_item(pat: str, item: dict, parent_url: str | None) -> dict:
    ops = [
        {"op": "add", "path": "/fields/System.Title", "value": item["title"]},
        {"op": "add", "path": "/fields/System.Description", "value": item["description"]},
    ]
    if item.get("tags"):
        ops.append({"op": "add", "path": "/fields/System.Tags", "value": item["tags"]})
    if item.get("priority"):
        ops.append({"op": "add", "path": "/fields/Microsoft.VSTS.Common.Priority",
                    "value": item["priority"]})
    if item.get("points"):
        ops.append({"op": "add", "path": "/fields/Microsoft.VSTS.Scheduling.StoryPoints",
                    "value": item["points"]})
    if item.get("acceptance"):
        ops.append({"op": "add", "path": "/fields/Microsoft.VSTS.Common.AcceptanceCriteria",
                    "value": item["acceptance"]})
    if parent_url:
        ops.append({
            "op": "add",
            "path": "/relations/-",
            "value": {"rel": "System.LinkTypes.Hierarchy-Reverse", "url": parent_url},
        })
    wi_type = urllib.parse.quote(item["type"])
    return request("POST", f"{BASE}/wit/workitems/${wi_type}?{API}", pat, ops,
                   content_type="application/json-patch+json")


def walk(pat: str, items: list, parent_url: str | None, existing: dict, indent=0):
    created, skipped = 0, 0
    for item in items:
        key = (item["type"], item["title"])
        if key in existing:
            wi_id = existing[key]
            url = f"https://dev.azure.com/{ORG}/{PROJECT}/_apis/wit/workItems/{wi_id}"
            print(f"{'  ' * indent}= {item['type']} #{wi_id}: {item['title']} (exists, skipped)")
            skipped += 1
        else:
            wi = create_item(pat, item, parent_url)
            wi_id, url = wi["id"], wi["url"]
            existing[key] = wi_id
            print(f"{'  ' * indent}+ {item['type']} #{wi_id}: {item['title']}")
            created += 1
        c, s = walk(pat, item.get("children", []), url, existing, indent + 1)
        created += c
        skipped += s
    return created, skipped


def main():
    pat = os.environ.get("ADO_PAT", "").strip()
    if not pat:
        pat_file = os.path.join(os.path.expanduser("~"), ".ado_pat")
        if os.path.exists(pat_file):
            pat = open(pat_file).read().strip()
    if not pat:
        sys.exit("Set ADO_PAT env var (or write the PAT to ~/.ado_pat) with Work Items Read & Write scope.")

    print(f"Target: https://dev.azure.com/{ORG}/{PROJECT}\n")
    existing = existing_items(pat)
    created, skipped = walk(pat, BACKLOG, None, existing)
    print(f"\nDone: {created} created, {skipped} already existed.")


if __name__ == "__main__":
    main()
