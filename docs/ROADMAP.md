# NodeControl Roadmap

## Current Status

NodeControl has moved beyond the initial skeleton. The repository now contains a working backend, Worker,
Next.js frontend, EF Core migrations, integration/unit tests, Docker development infrastructure, and local
dev/demo bootstrap scripts.

The current product is a hardened dev/demo MVP: a customer-scoped Ansible control plane with users,
memberships, hosts, inventories, playbooks, variable sets, Actions, Runs, logs, schedules, Hostzustand,
secrets/templates metadata surfaces, audit, and an Acme showcase flow. It is credible for a local demo and
portfolio review, but it is not a production deployment package yet.

The explicit current/supporting/Post-MVP boundary is maintained in `docs/MVP_BOUNDARY.md`.

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
- Inline YAML and managed artifact-directory playbooks
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
- Planned placeholder pages that clearly mark Post-MVP surfaces
- Frontend shell stabilization
- Local scripts for infrastructure, migrations, API, Worker, frontend, and smoke checks
- API-driven Acme showcase seed flow for hosts, inventory, playbook, variables, action, paused schedule, and optional queued Run
- Minimal `deploy/` notes that clearly avoid claiming production readiness

### MVP Hardening

- Explicit permission checks for sensitive operations
- Customer-scoped lookup patterns for list/detail/action flows
- Secret metadata responses without raw secret values
- Lightweight response-time redaction for obvious sensitive log/error patterns
- Confirmation guardrails for cancel, retry, archive, pause/resume, and secret archive flows
- Clearer forbidden/not-found messaging in core user-facing areas

## Current Architecture Boundaries

- The API never executes Ansible, SSH, TCP checks, shell commands, or process starts.
- The Worker is the only process that runs `ansible-playbook` or performs Hostzustand TCP checks.
- Manual and scheduled Runs use the same queued JobRun execution path.
- Schedules use a database-backed Worker poller. Quartz.NET is not part of the current implementation.
- Templates are not rendered or connected to Worker execution yet.
- Secret values are not returned through the API and are not decrypted into execution yet.
- Git-backed playbooks remain Post-MVP.

## Near-Term Expansion Paths

Good next slices should stay small, visible, and aligned with the control-plane boundary:

- Screenshots or a short reviewer demo guide
- More focused frontend tests around shell, run wizard, permissions, and critical empty states
- Production configuration documentation and deployment hardening
- Basic operational health endpoint and version display
- More Worker test coverage for edge cases around cancellation, timeouts, and host checks
- Secret runtime integration design before any execution-time decryption work
- Hardening the managed playbook asset lifecycle before adding repository-backed execution

These are likely next steps, not commitments. Each should be delivered as a vertical slice with authorization,
customer scoping, tests, and documentation.

## Deferred Post-MVP Areas

These areas are intentionally outside the current MVP and should not be treated as partially implemented just
because some placeholder routes or docs mention them:

- Git-backed playbooks
- Richer playbook asset upload/import lifecycle
- Ansible Collections dependency management
- Import workflows
- Cloud-provider inventory integrations
- Notifications
- Advanced secret/key lifecycle and external secret-vault integration
- Broader security and system administration surfaces
- Approval workflow
- Live log streaming
- OIDC provider configuration per customer
- SAML
- Dynamic role editor
- Policy engine
- Multi-control-node dispatching
- Production Docker Compose packaging
- Kubernetes/Helm only if explicitly needed later
- Billing/licensing

Exploratory or business-model ideas such as plugin systems, marketplace behavior, licensing, and billing should
remain deferred until the core self-hosted operations story is mature.
