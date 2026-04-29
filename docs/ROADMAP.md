# NodeControl Roadmap

## Current Status

NodeControl has moved beyond the initial skeleton. The repository now contains a working backend, Worker,
Next.js frontend, EF Core migrations, integration/unit tests, Docker development infrastructure, and local
dev/demo bootstrap scripts.

The current product is credible for a local demo and portfolio review, but it is not a production deployment
package yet.

## Delivered Slice Themes

### Foundation

- Documentation and architecture rules
- Backend projects: Domain, Application, Infrastructure, Api, Worker
- Frontend app with Next.js App Router, React, TypeScript, MUI, and TanStack Query
- PostgreSQL persistence through EF Core
- Docker Compose development infrastructure

### Identity and Tenancy

- Fake Auth for fast local demo
- OIDC support in the API
- Current user endpoint and user provisioning
- Internal users, external identities, customer memberships, static roles, and permissions
- Platform admin user overview
- Customer-scoped authorization and cross-tenant test coverage

### Inventory and Automation Inputs

- Customers
- Control Hosts
- Hosts
- Inventory groups and inventory preview
- Inline playbooks
- Variable sets with YAML/JSON validation
- Template management as plain text resources
- Secret metadata management, rotation, and safe `secret://...` reference validation

### Runs, Scheduling, and Operations

- Actions as reusable execution definitions
- Manual queued Runs
- Worker execution pipeline for inline playbooks
- Generated run workspaces and inventories
- Captured stdout/stderr/system log entries
- Run status transitions, cancellation, retry, and Run Center views
- Database-polled schedules that create scheduled Runs
- Customer-scoped audit logs
- Hostzustand checks queued by the API and processed by the Worker as TCP connect attempts

### Demo and Bootstrap

- Dashboard and customer-aware navigation
- Run wizard
- Planned placeholder pages for post-MVP surfaces
- Frontend shell stabilization
- Local scripts for infrastructure, migrations, API, Worker, frontend, and smoke checks
- API-driven Acme showcase seed flow for hosts, inventory, playbook, variables, action, paused schedule, and optional queued Run
- Minimal `deploy/` notes that clearly avoid claiming production readiness

## Current Architecture Boundaries

- The API never executes Ansible, SSH, TCP checks, shell commands, or process starts.
- The Worker is the only process that runs `ansible-playbook` or performs Hostzustand TCP checks.
- Manual and scheduled Runs use the same queued JobRun execution path.
- Schedules use a database-backed Worker poller. Quartz.NET is not part of the current implementation.
- Templates are not rendered or connected to Worker execution yet.
- Secret values are not returned through the API and are not decrypted into execution yet.
- Git-backed and artifact-directory playbooks remain post-MVP.

## Next Useful Slices

Good next slices should stay small and visible:

- Screenshots or a short demo guide for reviewers
- More focused frontend tests around shell, run wizard, and critical empty states
- Production configuration documentation and deployment hardening
- Basic operational health endpoint and version display
- More Worker test coverage for edge cases around cancellation, timeouts, and host checks

## Post-MVP Features

Potential later features:

- OIDC provider configuration per customer
- SAML
- Approval workflow
- Git-backed playbooks
- Artifact-directory playbooks
- Notifications
- Secret vault integration
- Live log streaming
- Dynamic role editor
- Policy engine
- Multi-control-node dispatching
- Production Docker Compose packaging
- Kubernetes/Helm only if explicitly needed later
- Billing/licensing
