# Architecture Decision Record

This document stores important project decisions.

## DEC-001: NodeControl is an Ansible control plane, not an Ansible replacement

Status: Accepted

NodeControl wraps Ansible with customer separation, permissions, scheduling, job history, logs, and auditability.

It does not rebuild every Ansible feature as a custom UI feature.

## DEC-002: Use a monorepo

Status: Accepted

Backend, frontend, deployment files, scripts, and documentation live in one repository.

Reason:

- Easier development
- Easier Codex context
- Easier demo setup
- Easier portfolio review

## DEC-003: Use pragmatic Clean Architecture

Status: Accepted

Backend projects:

- Domain
- Application
- Infrastructure
- Api
- Worker

Reason:

- Clean separation without excessive complexity
- Good testability
- Easy to explain in interviews
- Avoids microservice over-engineering

## DEC-004: Use ASP.NET Core Minimal APIs

Status: Accepted

The API uses Minimal APIs instead of MVC controllers.

Reason:

- Less ceremony
- Good fit for small endpoint groups
- Clear and modern ASP.NET Core style

## DEC-005: No MediatR/CQRS/Event Sourcing in MVP

Status: Accepted

Reason:

- The project does not need that complexity.
- Vertical slices can be implemented with explicit application services.
- The goal is a product, not an architecture showcase.

## DEC-006: OIDC-first authentication

Status: Accepted

NodeControl integrates external identity providers through OIDC.

Reason:

- Better fit for B2B customers
- Works with providers like Entra ID, Okta, Keycloak, Google Workspace
- Avoids forcing customers to run NodeControl's preferred identity provider

## DEC-007: Keycloak is not a required product dependency

Status: Accepted

Keycloak may be used for local development, demo, and self-host testing.

NodeControl must also support other OIDC providers.

## DEC-008: Separate external identity from internal authorization

Status: Accepted

The external provider identifies the user.

NodeControl decides what the user may do.

Internal authorization is based on:

- User
- ExternalIdentity
- CustomerMembership
- Role
- Permission

## DEC-009: Use static roles in the MVP

Status: Accepted

MVP roles:

- Owner
- Admin
- Operator
- Viewer
- Auditor

Dynamic role management is postponed.

Reason:

- Dynamic RBAC is a separate product feature.
- Static roles are easier to secure and test.

## DEC-010: Start with database-polled schedules

Status: Accepted

Reason:

- Scheduling is a product feature.
- Cron support is needed.
- The current implementation keeps the MVP simple by polling due active schedules from `NodeControl.Worker`.
- Quartz.NET remains a possible later implementation if richer misfire handling or clustering becomes necessary.

## DEC-011: Keep scheduling domain independent from Quartz

Status: Accepted

NodeControl owns the Schedule domain model.

Any scheduler implementation is an infrastructure detail.

## DEC-012: API must not execute Ansible

Status: Accepted

The API creates JobRuns and validates permissions.

The Worker executes jobs.

Reason:

- Avoid long-running HTTP requests
- Isolate dangerous execution logic
- Improve reliability
- Easier testing

## DEC-013: Use PostgreSQL as the coordination point in MVP

Status: Accepted

Queued JobRuns are persisted in PostgreSQL.

No message bus is used in MVP.

Reason:

- Simpler deployment
- Good enough for MVP
- Avoids unnecessary distributed infrastructure

## DEC-014: Use MUI for frontend UI

Status: Accepted

Reason:

- NodeControl is an admin/operations UI
- MUI provides strong components for dashboards, forms, dialogs, tables, and navigation
- Faster MVP development than designing every component from scratch

## DEC-015: Use Next.js for the frontend

Status: Accepted

Reason:

- Strong React framework
- Good routing/layout model
- Professional portfolio value
- Works well for dashboard-style applications

## DEC-016: Store playbook metadata in DB and materialize artifacts in run workspaces

Status: Accepted

Reason:

- Real Ansible playbooks may contain multiple files, roles, templates, and assets.
- The current MVP stores inline YAML or a small managed set of artifact-directory files with the Playbook record.
- The Worker materializes playbook files into the filesystem-backed JobRun workspace before execution.
- A later durable artifact store may move larger playbook assets out of the database without changing the API/Worker boundary.

## DEC-017: Git-backed playbooks are post-MVP

Status: Accepted

Reason:

- Valuable but not required for the first working product.
- Adds complexity around credentials, sync, branches, and updates.

## DEC-018: SAML is post-MVP

Status: Accepted

Reason:

- OIDC covers the first professional integration path.
- SAML may matter for enterprise customers later.

## DEC-019: Resolve execution secret references only in the Worker

Status: Accepted

Actions may reference managed templates as relative run workspace artifacts. VariableSets and those configured
template artifacts may contain `secret://...` references.

Reason:

- The API remains non-operational and never decrypts execution secrets.
- Secret references stay as references at rest in normal definitions.
- The Worker already owns run workspace materialization and is the right boundary for producing execution-ready files.
- Persisted run logs and API-facing models must not expose resolved secret values.

## DEC-020: Snapshot Control Node binding on JobRuns and dispatch only from Worker

Status: Accepted

JobRuns store the selected ControlNodeId when they are queued. The Worker loads that run-bound Control Node, prepares a
control-node-scoped workspace, writes a dispatch manifest, and then dispatches through a Worker-side abstraction.

Reason:

- A Run should remain tied to the Control Node selected at queue time.
- Editing an Action after queueing must not silently retarget already queued Runs.
- The API remains non-operational and never performs Ansible, SSH, shell, TCP, or process execution.
- The MVP keeps local/dev execution for configured local Control Nodes and uses a small Worker-side SSH transport for
  configured non-local Control Nodes.

## DEC-021: Use Worker-side OpenSSH CLI for the remote dispatch MVP

Status: Accepted

NodeControl stores minimal SSH dispatch configuration on Control Nodes: username, remote workspace root, and an SSH
private key Secret reference. For non-local Control Nodes, the Worker materializes that key into a temporary file,
uses `scp` to stage the already-prepared run workspace, and uses `ssh` to start `ansible-playbook` remotely. Remote
dispatch stages into a unique `.staging-*` directory beside the final run path, promotes the staged tree after a
successful copy, and cleans up temporary local SSH material plus best-effort remote staging leftovers.

Reason:

- It keeps API behavior non-operational.
- It reuses existing Secret protection and Worker-side secret resolution.
- It is credible for an MVP without introducing agents, message buses, or a broad orchestration framework.
- It gives retries and reprocessing deterministic workspace handling without adding a remote file-management subsystem.

## DEC-022: Model minimal Managed Host SSH execution settings

Status: Accepted

Managed Nodes store SSH port, optional SSH username, and an optional SSH private key Secret reference. During queued
Run processing, the Worker resolves those host key references, writes per-host key files into the run workspace, and
generates inventory variables such as `ansible_user` and `ansible_ssh_private_key_file`.

Reason:

- It makes Ansible target-host execution more realistic without moving SSH behavior into the API.
- It reuses existing customer-scoped Secret records instead of adding a separate credential store.
- It keeps the model small enough for MVP use and provides the credential foundation for one-hop bastion behavior.
- The Worker can remove temporary key files after dispatch while retaining useful non-sensitive run artifacts.

## DEC-023: Model one-hop Managed Host Jump Host paths

Status: Accepted

Managed Nodes may optionally reference exactly one Jump Host, modeled as another active Managed Node in the same
customer. The Application layer validates same-customer scope, rejects self-reference, and rejects nested jump chains.
The API remains metadata-only. During Worker-side run workspace preparation, inventory renders an OpenSSH
`ProxyCommand` in `ansible_ssh_common_args` so Ansible reaches the target through the Jump Host.

Reason:

- It closes the common bastion-host realism gap without introducing agents, proxy services, message buses, or a
  generic network-routing platform.
- It keeps customer scoping and Secret-backed SSH key handling in the existing Managed Host model.
- It preserves direct-host and localhost/dev execution paths.
- It keeps SSH command/process behavior in the Worker execution boundary.
