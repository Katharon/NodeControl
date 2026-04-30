# Ansible Execution

## Execution Goal

NodeControl must execute Ansible playbooks safely, repeatably, and auditably.

NodeControl does not replace Ansible. It prepares inputs, starts Ansible, captures outputs, and stores results.

## Main Execution Rule

The API must never execute Ansible directly.

Only `NodeControl.Worker` executes Ansible.

## Core Concepts

### Control Node

The system or execution environment where Ansible runs.

### Managed Node

A target host managed by Ansible.

### Inventory Group

A group of Managed Nodes used to generate an Ansible inventory.

### Playbook

An automation definition.

### VariableSet

Variables passed to the playbook.

### Job

A reusable execution template.

### JobRun

One concrete execution.

## Current Playbook Storage

The current implementation supports two playbook source types:

- Inline YAML playbooks, where the playbook content is stored directly on the Playbook record.
- Managed artifact-directory playbooks, where NodeControl stores a small set of relative playbook files and an
  entry file path such as `site.yml`.

When a Run is processed, the Worker materializes the selected playbook into that JobRun workspace. The API stores
metadata/content and validates obvious invalid combinations, but it never creates execution workspaces or runs
Ansible.

A later production-style artifact path outside individual run workspaces could be:

```text
/var/lib/nodecontrol/playbooks/{customerId}/{playbookId}/
```

For inline YAML playbooks, a future artifact-backed layout could write:

```text
/var/lib/nodecontrol/playbooks/{customerId}/{playbookId}/site.yml
```

Managed artifact-directory playbooks may include:

```text
site.yml
roles/
templates/
files/
group_vars/
host_vars/
```

The product UI lets operators import local text files, edit file paths/content as explicit rows, and see the configured
entry file plus stored artifact file list. The API still stores and validates metadata/content only; it does not create
execution workspaces or run Ansible.

## JobRun Workspace

Each JobRun gets its own workspace.

Suggested path:

```text
/var/lib/nodecontrol/runs/{jobRunId}/
├── inventory.yml
├── vars.yml
├── playbook/
│   └── site.yml
├── stdout.log
└── stderr.log
```

For inline YAML playbooks, the Worker writes the content to `playbook/site.yml`. For artifact-directory playbooks,
the Worker writes the complete managed file tree under `playbook/` and invokes `ansible-playbook` with the configured
entry file path.

The Worker implements local execution for queued JobRuns. The workspace root is configured with
`NodeControl:Execution:RunWorkspaceRoot`; development uses `.nodecontrol/runs`, while production-style
configuration uses `/var/lib/nodecontrol/runs`.

## Execution Command

Conceptually:

```text
ansible-playbook -i inventory.yml playbook/site.yml -e @vars.yml
```

The exact implementation belongs to Infrastructure/Worker.

The Worker runs `ansible-playbook` from `NodeControl.Worker` only, using `ProcessStartInfo.ArgumentList`
with `UseShellExecute = false`. The executable path is configured with
`NodeControl:Execution:AnsiblePlaybookPath` and defaults to `ansible-playbook`.

## MVP Captured Data

For every JobRun, capture:

- Status
- StartedAt
- FinishedAt
- Duration
- ExitCode
- Stdout log path
- Stderr log path
- ErrorMessage
- TriggerType
- TriggeredByUserId if manual
- ScheduleId if scheduled

The Worker writes persisted System, StdOut, and StdErr JobRun log entries while a run
is processed. The API exposes these entries read-only for authorized customer users; it does not execute or
resume JobRuns.

## JobRun Statuses

Initial statuses:

- Queued
- Running
- Cancelling
- Succeeded
- Failed
- Cancelled
- TimedOut

## Inventory Generation

MVP inventory generation is based on:

- Customer
- InventoryGroup
- ManagedNodes

Example conceptual output:

```yaml
all:
  children:
    webservers:
      hosts:
        web-01:
          ansible_host: 10.0.0.10
```

Do not overbuild dynamic inventory in the MVP.

The current implementation includes target structure, inventory group assignment, and a read-only inventory preview.

- Control Nodes are modeled as customer-scoped records. Hostzustand can queue TCP reachability checks, but
  NodeControl does not perform SSH authentication checks.
- Managed Nodes are modeled as customer-scoped records and can be linked to Inventory Groups.
- Inventory Groups generate a YAML preview from active Managed Nodes.
- Archived Managed Nodes are excluded from preview output.
- The API still must not execute Ansible; execution belongs to the Worker.

For Worker execution, the selected Job inventory group is rendered to `inventory.yml`. Active managed nodes
linked to that group become hosts, and archived nodes are excluded. If the group has no active managed nodes,
the JobRun fails before Ansible starts.

## Variable Sets

VariableSets may be stored as YAML or JSON content.

They are written into the JobRun workspace as `vars.yml` or `vars.json` depending on the VariableSet format.

If the Worker sees `secret://...` references in the selected VariableSet, it resolves those references while writing the
workspace file. The API stores and validates references only; it does not decrypt secrets or create execution-ready
variable files.

Rules:

- Do not log variable content.
- Do not expose secret variable values through API responses.
- Resolve secret references only in the Worker execution path.

## Templates

Templates are reusable customer-scoped text/Jinja2/config/script resources. Actions may map managed templates to
relative file paths under `playbook/` in the per-Run workspace. During workspace preparation, the Worker materializes
those files and resolves any `secret://...` references in the template content.

The UI can import a local text file into a Template and can configure multiple Action template artifact mappings with
explicit relative workspace paths. These paths are validated as relative artifact paths and are still materialized only
by the Worker during run preparation.

This is intentionally narrow:

- The API stores template content and Action mappings only.
- The Worker writes execution-ready files into the run workspace.
- NodeControl does not run a separate Jinja/Python template engine in this slice.
- Template artifact paths must be relative and must not overwrite existing playbook files.

## Secrets

Secrets are customer-scoped protected values managed through metadata-only API responses. Plaintext values are accepted
only on create and rotate, protected before persistence, and never returned from normal API responses.

Safe `secret://secret-slug` references are supported for Templates and VariableSets. Reference validation checks
same-customer active metadata without decrypting. During JobRun execution, only the Worker decrypts active referenced
secrets and substitutes them while materializing `vars.yml` / `vars.json` and configured template artifact files.
Resolved values may exist in the local run workspace as execution-ready artifacts, but they are redacted from persisted
run logs and are not returned in API-facing models.

## Dangerous Execution Concerns

Ansible can change real systems.

NodeControl must therefore provide:

- Clear target preview
- Customer scoping
- Permission checks
- Audit logs
- Job history
- Logs
- Explicit run action

Post-MVP features:

- Approval workflow
- Dry-run/check mode
- Diff mode
- Tags/skip-tags
- Limit
- Execution confirmation
- Secret vault integration

## Output Parsing

MVP should not attempt full semantic parsing of all Ansible output.

MVP captures:

- stdout
- stderr
- exit code
- final status
- duration

Stdout and stderr are also stored as ordered log entries for display. NodeControl still treats these as text
lines rather than parsing Ansible task semantics.

Post-MVP may add:

- JSON callback plugin
- task-level event timeline
- per-host result summary
- changed/failed/unreachable counts

## Cancellation

The API can cancel queued JobRuns or request cancellation for running JobRuns.

- Queued cancellation immediately sets the JobRun to Cancelled.
- Running cancellation sets the JobRun to Cancelling.
- The API never kills operating system processes.
- The Worker observes cancellation state in the database while `ansible-playbook` is running.
- The Worker terminates the process tree, writes cancellation logs, and marks the JobRun Cancelled.

Cancelled, failed, and timed-out JobRuns can be retried by creating a new queued JobRun with
`TriggerType = Retry`. The retry keeps a link to the original JobRun through `RetriedFromJobRunId`.

Manual run creation, cancellation requests, retries, and scheduled run creation write audit entries. These
entries record the business operation and affected customer/entity only. Worker stdout/stderr and detailed
execution progress continue to belong to JobRun log entries, not audit entries.

## Timeouts

Jobs should have a default timeout.

If exceeded:

- Worker terminates execution.
- JobRun status becomes TimedOut.
- Logs are retained.
- AuditLog records timeout.

## Post-MVP Features

Potential later improvements:

- Git-backed playbooks
- Richer artifact upload/import lifecycle
- Ansible roles support in UI
- Vault integration
- Secret masking
- Check mode
- Diff mode
- Tags/skip-tags
- Limit hosts
- Live logs via WebSocket/SSE
- Structured Ansible event parsing
