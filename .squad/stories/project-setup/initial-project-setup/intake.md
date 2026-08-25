# Story Intake — Initial Project Setup

## Project

Customer Support CRM

This is a greenfield project. The repository currently contains only the `.squad/` configuration and planning files.

The purpose of this story is to create the complete technical foundation for the project before implementing any CRM business functionality.

---

## Mandatory Technology Stack

### Backend

- ASP.NET Core Web API
- .NET 8
- C#
- Entity Framework Core 8
- SQL Server
- Swagger / OpenAPI
- REST API architecture

### Frontend

- React
- TypeScript
- Vite
- Tailwind CSS v3
- React Router
- Zustand
- Axios

### Repository

- Monorepo
- Git
- GitHub
- Backend and frontend must remain clearly separated.

---

## Repository Structure

The final repository must have this structure:

```text
/
├── backend/
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
├── frontend/
│   ├── package.json
│   ├── package-lock.json
│   ├── vite.config.ts
│   ├── tsconfig.json
│   ├── tsconfig.node.json
│   ├── tailwind.config.js
│   ├── postcss.config.js
│   ├── index.html
│   ├── .env.example
│   └── src/
│       ├── main.tsx
│       ├── App.tsx
│       ├── index.css
│       ├── routes/
│       │   └── AppRouter.tsx
│       ├── store/
│       │   └── useAppStore.ts
│       ├── api/
│       │   └── httpClient.ts
│       └── pages/
│           └── HomePage.tsx
│
├── .gitignore
├── README.md
└── .squad/