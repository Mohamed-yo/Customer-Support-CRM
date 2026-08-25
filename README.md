# Customer Support CRM

A Customer Support CRM application. This repository currently contains the technical foundation only — backend and frontend scaffolding, wired together, with no CRM business functionality yet.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) and npm 10+
- A reachable SQL Server instance (LocalDB, a local SQL Server install, or a containerised instance) — only required if you want the backend to actually connect to a database; the API starts and serves `/api/health` without one.

## Repository structure

```text
/
├── backend/                          ASP.NET Core Web API (.NET 8, EF Core 8, SQL Server)
│   ├── CustomerSupportCrm.sln
│   └── src/
│       └── CustomerSupportCrm.Api/
│           ├── CustomerSupportCrm.Api.csproj
│           ├── Program.cs
│           ├── appsettings.json
│           ├── appsettings.Development.json.example
│           ├── Controllers/
│           │   └── HealthController.cs
│           ├── Data/
│           │   └── AppDbContext.cs
│           └── Properties/
│               └── launchSettings.json
│
├── frontend/                         React + TypeScript + Vite app
│   ├── package.json
│   ├── vite.config.ts
│   ├── tailwind.config.js
│   ├── .env.example
│   └── src/
│       ├── main.tsx
│       ├── App.tsx
│       ├── index.css
│       ├── routes/AppRouter.tsx
│       ├── store/useAppStore.ts
│       ├── api/httpClient.ts
│       └── pages/HomePage.tsx
│
├── .gitignore
├── README.md
└── .squad/                           squad-kit planning/configuration (not part of the app)
```

Backend and frontend are kept strictly separate: no shared `node_modules`, no root `package.json` or `.sln`, no cross-imports between the two.

## Backend setup

```bash
cd backend
dotnet restore
cp src/CustomerSupportCrm.Api/appsettings.Development.json.example src/CustomerSupportCrm.Api/appsettings.Development.json
# edit the copied file with your local SQL Server connection string if you need one
dotnet run --project src/CustomerSupportCrm.Api
```

The API listens on `http://localhost:5000` (and `https://localhost:5001`) by default. `GET /api/health` works even without a configured database connection.

## Frontend setup

```bash
cd frontend
cp .env.example .env
npm install
npm run dev
```

The dev server runs on `http://localhost:5173` and can be used standalone; it only calls the backend when rendering the health status on the home page.

## Running both together

1. Start the backend (`dotnet run --project backend/src/CustomerSupportCrm.Api`).
2. Start the frontend (`npm run dev` from `frontend/`).
3. Open `http://localhost:5173` — the home page displays the app name and the live backend health status (`ok` if the backend is reachable, `unreachable` otherwise).

## Swagger

In Development, Swagger UI is available at `http://localhost:5000/swagger` and currently lists only the `Health` endpoint.

## Environment configuration

No secrets are committed. Only environment **templates** are tracked in git; copy them to their real, git-ignored counterparts before running each app:

| Template | Real file (git-ignored) | Controls |
|---|---|---|
| `frontend/.env.example` | `frontend/.env` | `VITE_API_BASE_URL` — base URL the frontend's Axios client uses to call the backend |
| `backend/src/CustomerSupportCrm.Api/appsettings.Development.json.example` | `backend/src/CustomerSupportCrm.Api/appsettings.Development.json` | `ConnectionStrings:DefaultConnection` — SQL Server connection string used in Development |

`appsettings.json` ships with an empty `DefaultConnection` so no environment-specific secrets leak into source control.

## Notes

- If you change the Vite dev server port away from `5173`, update both `frontend/vite.config.ts` and the CORS policy in `backend/src/CustomerSupportCrm.Api/Program.cs`.
- This story ships no EF Core migrations, no authentication/authorization, no CRM domain entities, and no tests — those arrive in later stories.
