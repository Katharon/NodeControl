# MVP Boundary

This document defines the line between the implemented NodeControl MVP, supporting MVP surfaces, and intentionally
deferred Post-MVP areas.

## Current MVP

The current MVP is a customer-scoped Ansible control plane for local dev/demo use. It is meant to show the core
operational loop end to end:

- Customers, users, customer memberships, static roles, and permissions
- Control Hosts, Hosts, inventory groups, and inventory preview
- Inline YAML playbooks, managed artifact-directory playbooks, and variable sets
- Actions as reusable execution definitions
- Manual Runs, Run Center, persisted logs, cancellation, and retry
- Worker-side local/dev and SSH remote dispatch for configured Control Hosts
- Database-polled schedules that create scheduled Runs through the same execution path
- Hostzustand checks queued by the API and processed by the Worker
- Customer-scoped audit logs
- Local Fake Auth plus OIDC-capable API configuration
- Docker Compose development infrastructure, scripts, and Acme showcase data

## Supporting MVP Surfaces

These areas are implemented, but their current scope is deliberately narrow:

- Templates are managed text resources that Actions can map to relative run workspace files. They are not uploaded
  directly to hosts and do not provide a full orchestration system.
- Secrets store protected metadata and accept create/rotate values. API responses expose metadata and
  `secret://...` references only; values are resolved only by the Worker during execution workspace preparation.
- User overview is a platform-admin review surface, not a full identity administration system.
- `deploy/` and the dev scripts support local development and demo. They are not a production packaging story.

## Explicit Post-MVP Areas

These areas may be visible as placeholder routes or documented future directions, but they are not part of the
current MVP:

- Git-backed playbooks and richer playbook asset lifecycle
- Ansible Collections dependency management
- Import workflows for existing inventories or automation definitions
- Cloud-provider integrations as inventory sources
- Notifications for run outcomes and operational events
- External secret providers, key recovery flows, or Vault-style integrations
- Broader security and system administration surfaces
- Health/version endpoints and production deployment hardening
- Approval workflows, live log streaming, dynamic roles, policy engine, billing, licensing, and plugin systems
- SAML, Kubernetes/Helm, message buses, and other enterprise infrastructure unless explicitly chosen later

## Boundary Rules

- The API remains the HTTP control plane. It never executes Ansible, SSH, TCP checks, shell commands, or process starts
  as product behavior.
- The Worker remains responsible for queued Run execution, SSH remote dispatch, Hostzustand TCP checks, workspaces,
  and log capture.
- Future surfaces should become real only through small vertical slices with authorization, customer scoping, tests,
  documentation, and demo behavior.
- Placeholder UI should describe future intent honestly and must not imply that deferred capabilities already work.
