# NodeControl

NodeControl is a self-hosted B2B web platform for safely managing and executing Ansible automation through a professional web interface.

The project is designed for IT service providers, managed service providers, system houses, and internal IT teams that want to run Ansible workflows without giving every operator direct terminal, SSH, or full Ansible access.

NodeControl is not intended to replace Ansible. It acts as a control plane around Ansible: customer separation, permissions, inventory management, playbook execution, scheduled jobs, job history, logs, and auditability.

## Product Terminology

The backend keeps the pragmatic domain names used by the execution model, while the frontend presents product-facing labels:

- `Job` is shown as **Action**.
- `JobRun` is shown as **Run**.
- `ManagedNode` is shown as **Host**.
- `ControlNode` is shown as **Control Host**.
- `InventoryGroup` is shown as **Inventar**.
- `Customer` is shown as **Kunde** where the UI uses German labels.

## Product Goal

NodeControl should become a product-grade portfolio project and a realistic self-hosted B2B product.

The main value proposition is:

> Run Ansible workflows safely, repeatedly, and auditably across customer environments without direct terminal work.

## Core Capabilities

Initial product scope:

- Customer management
- User profiles and customer memberships
- External enterprise authentication through OIDC
- Internal NodeControl roles and permissions
- Control nodes
- Managed nodes
- Inventory groups
- Playbooks
- Variable sets
- Manual jobs
- Scheduled jobs / cron jobs
- Job run history
- Job logs
- Audit logs
- Health/status overview
- Docker-based self-hosted deployment

## Target Users

Primary users:

- IT service providers
- Managed service providers
- System houses
- Internal IT departments
- Administrators who already use Ansible but need a safer UI-based workflow

## Non-Goals

NodeControl is intentionally not:

- A full AWX clone
- A replacement for Ansible
- A microservice experiment
- A Keycloak-only application
- A Kubernetes-first product
- A workflow engine for every possible automation technology
- A platform that rebuilds every Ansible feature as a custom UI feature

## High-Level Architecture

```text
Browser
  |
  | HTTPS
  v
Next.js Web UI
  |
  | /api/v1
  v
ASP.NET Core API
  |
  | PostgreSQL
  v
NodeControl Database
  |
  | queued JobRuns / schedules
  v
NodeControl Worker
  |
  | ansible-playbook
  v
Control Node
  |
  | SSH
  v
Managed Nodes
```

External identity providers are integrated through OIDC. Keycloak may be used as a local development/demo provider, but it is not a required product dependency.

## Technology Direction

Backend:

- C#
- ASP.NET Core Minimal APIs
- Entity Framework Core
- PostgreSQL
- Database-backed Worker schedule polling
- Background worker for execution
- xUnit and integration tests

Frontend:

- Next.js
- React
- TypeScript
- MUI
- TanStack Query
- React Hook Form
- Zod

Deployment:

- Docker Compose for MVP/self-hosted installations
- Kubernetes may be added later only if needed

## Local Dev/Demo Quick Start

Prerequisites:

- .NET SDK 10
- Node.js and npm
- Docker with Compose support
- Local .NET tools restored from `.config/dotnet-tools.json`

Restore the local .NET tools:

```bash
dotnet tool restore
```

Start the local PostgreSQL and Keycloak development services:

```bash
./scripts/dev-up.sh
```

Apply the existing EF Core migrations to the local PostgreSQL database:

```bash
./scripts/dev-migrate.sh
```

Run the application in three separate terminals:

```bash
./scripts/dev-run-api.sh
./scripts/dev-run-worker.sh
./scripts/dev-run-frontend.sh
```

Open `http://localhost:3000`. In the default Development configuration, the API uses Fake Auth and signs you in as `Dev Admin`.

Useful local URLs:

- Frontend: `http://localhost:3000`
- API: `http://localhost:5257`
- Current user check: `http://localhost:5257/api/v1/me`
- Keycloak dev provider: `http://localhost:18080`

Run the local validation and smoke checks:

```bash
./scripts/dev-smoke.sh
```

Stop the local infrastructure when you are done:

```bash
./scripts/dev-down.sh
```

`dev-down.sh` preserves Docker volumes. If you need to reset the database, use Docker Compose manually and be explicit about removing volumes.

## Dev/Demo Scripts

The `scripts/` directory contains small shell scripts for repeatable local workflows:

- `dev-up.sh` starts the Docker Compose development infrastructure.
- `dev-down.sh` stops the development infrastructure without deleting volumes.
- `dev-migrate.sh` runs `dotnet ef database update` against `NodeControlDbContext`.
- `dev-run-api.sh` starts the API in Development mode on `http://localhost:5257`.
- `dev-run-worker.sh` starts the Worker in Development mode.
- `dev-run-frontend.sh` starts the Next.js dev server and points it at the local API.
- `dev-smoke.sh` runs backend restore/build/test, frontend lint/build, the API execution-boundary grep, and optional HTTP checks when local services are running.

The scripts use these defaults and can be overridden through environment variables:

- `NODECONTROL_CONNECTION_STRING`
- `NODECONTROL_API_URL`
- `NODECONTROL_FRONTEND_URL`
- `NODECONTROL_API_ORIGIN`
- `ASPNETCORE_URLS`

`deploy/` currently contains dev/demo notes only. It is not a production deployment package.

## Development Strategy

Development is performed in vertical slices.

A vertical slice means that one user-visible feature is implemented end-to-end across:

- Domain model
- Application service
- Persistence
- API endpoint
- Frontend screen
- Authorization
- Audit behavior
- Tests

Avoid creating speculative abstractions or empty folders before they are needed.

## First Vertical Slices

1. Project skeleton and documentation
2. OIDC authentication and current user profile
3. Customers and memberships
4. Managed nodes and inventory groups
5. Playbooks and variable sets
6. Manual job execution
7. Scheduled job execution
8. Dashboard and health overview
9. Portfolio/demo polish

## Repository Layout

```text
nodecontrol/
├── AGENTS.md
├── README.md
├── docs/
├── src/
│   ├── backend/
│   └── frontend/
├── tests/
├── deploy/
└── scripts/
```

## Current Status

NodeControl is implemented through small vertical slices. The current repository includes backend, Worker, frontend, tests, EF Core migrations, Docker-based local infrastructure, and dev/demo bootstrap scripts.
