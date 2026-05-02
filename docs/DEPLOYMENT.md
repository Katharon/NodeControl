# Deployment and Local Runtime

## Current Status

NodeControl currently has a reproducible local dev/demo runtime. It does not yet provide a production-ready
deployment package.

`deploy/` is intentionally minimal and documents the dev/demo posture. The root `docker-compose.dev.yml` starts
supporting infrastructure only: PostgreSQL and a Keycloak development container. The API, Worker, and frontend are
run from source with scripts in `scripts/`.

## Local Dev/Demo Runtime

Prerequisites:

- .NET SDK 10
- Node.js and npm
- Docker with Compose support
- Local .NET tools restored from `.config/dotnet-tools.json`

Bootstrap:

```bash
dotnet tool restore
./scripts/dev-up.sh
./scripts/dev-migrate.sh
```

Optional showcase data, after starting the API in one terminal:

```bash
./scripts/dev-run-api.sh
./scripts/dev-seed-demo.sh
```

With the API still running, start the Worker and frontend in separate terminals:

```bash
./scripts/dev-run-worker.sh
./scripts/dev-run-frontend.sh
```

Useful URLs:

- Frontend: `http://localhost:3000`
- API: `http://localhost:5257`
- Current user: `http://localhost:5257/api/v1/me`
- Keycloak dev container: `http://localhost:18080`

The default Development API configuration uses Fake Auth and signs in as Dev Admin. Keycloak is available for
OIDC development, but it is not required for the default demo path.

## Scripts

- `scripts/dev-up.sh`: starts PostgreSQL and Keycloak through `docker-compose.dev.yml`.
- `scripts/dev-down.sh`: stops the development infrastructure and preserves volumes.
- `scripts/dev-migrate.sh`: restores local `dotnet-ef` if needed and applies EF Core migrations.
- `scripts/dev-run-api.sh`: starts `NodeControl.Api` in Development mode.
- `scripts/dev-run-worker.sh`: starts `NodeControl.Worker` in Development mode.
- `scripts/dev-run-frontend.sh`: starts the Next.js dev server.
- `scripts/dev-seed-demo.sh`: seeds or updates the Acme showcase story through the running Development API.
- `scripts/dev-smoke.sh`: runs restore/build/test/lint/build, the API execution-boundary grep, and optional local HTTP checks.

## Runtime Responsibilities

- PostgreSQL stores product data, users, memberships, schedules, run metadata, run log entries, audit logs,
  templates, and secret metadata.
- The API exposes HTTP endpoints, validates input, performs authorization, and queues work.
- The Worker processes queued Runs, due schedules, Hostzustand checks, run workspaces, local/SSH remote Ansible
  execution, logs, and status transitions.
- The frontend provides the demo/product UI.

The API must never execute Ansible, SSH, TCP checks, shell commands, or process starts as product behavior.

## Production Readiness Gap

Production deployment still needs explicit work:

- Packaged API, Worker, and frontend services
- Reverse proxy and TLS termination
- External OIDC provider configuration
- Secure cookie and host configuration
- Persistent volume layout for Worker workspaces
- Backup and restore procedure for PostgreSQL and runtime artifacts
- Log handling and operational monitoring
- Production configuration validation and documentation

## Post-MVP Deployment Features

Potential later features:

- Production Docker Compose profile/package
- Automated database backup guidance
- Observability stack
- External object/file storage
- Multi-worker dispatch coordination and hardened remote control-node operations
- Kubernetes/Helm only if explicitly requested later
