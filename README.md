# NodeControl

NodeControl is a self-hosted B2B web platform for safely managing and executing Ansible automation through a professional web interface.

The project is designed for IT service providers, managed service providers, system houses, and internal IT teams that want to run Ansible workflows without giving every operator direct terminal, SSH, or full Ansible access.

NodeControl is not intended to replace Ansible. It acts as a control plane around Ansible: customer separation, permissions, inventory management, playbook execution, scheduled jobs, job history, logs, and auditability.

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
- Quartz.NET for scheduling
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

This repository starts with architecture and product documentation only.

No application code should be generated before the initial project contract is accepted.
