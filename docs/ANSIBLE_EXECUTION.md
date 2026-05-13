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

Git repository sources are a one-time import aid in the MVP. NodeControl stores customer-scoped repository metadata
such as URL, branch/revision, and subpath. The import UI can fetch selected public GitHub files in the browser and then
save the resulting content into the existing managed Playbook or Template records. The Worker still materializes only
managed NodeControl content during Runs; it does not clone Git repositories during execution.

## JobRun Workspace

Each JobRun gets its own workspace. The Worker deletes and recreates that run workspace before materializing artifacts
for the run, so a retried or reprocessed run does not accidentally inherit stale files from a previous materialization
of the same JobRun. Retry runs are separate JobRun records and therefore get separate workspace paths.

Suggested path:

```text
/var/lib/nodecontrol/runs/{customerId}/control-nodes/{controlNodeId}/runs/{jobRunId}/
├── inventory.yml
├── vars.yml
├── playbook/
│   └── site.yml
├── .nodecontrol/
│   └── control-node-dispatch.json
├── stdout.log
└── stderr.log
```

For inline YAML playbooks, the Worker writes the content to `playbook/site.yml`. For artifact-directory playbooks,
the Worker writes the complete managed file tree under `playbook/` and invokes `ansible-playbook` with the configured
entry file path.

The Worker implements local execution for queued JobRuns. The workspace root is configured with
`NodeControl:Execution:RunWorkspaceRoot`; development uses `.nodecontrol/runs`, while production-style
configuration uses `/var/lib/nodecontrol/runs`.

Each JobRun snapshots the selected Control Node when it is queued. Worker execution loads that Control Node through the
JobRun binding, scopes the workspace path under that Control Node, and writes a small dispatch manifest into the
workspace. Later changes to the Action's Control Node do not change already queued Runs.

The current remote-dispatch MVP has an explicit Worker-side dispatch boundary:

- The API still queues Runs and reads state only.
- The Worker prepares execution artifacts and a control-node dispatch manifest.
- Local/dev execution is allowed only for configured local Control Node hostnames such as `localhost`, `127.0.0.1`, and `::1`.
- Non-local Control Nodes use Worker-side SSH dispatch when an SSH username, SSH private key Secret reference, and
  remote workspace root are configured.
- The Worker materializes the SSH private key into a temporary file, stages the prepared workspace to the remote
  Control Node with `scp`, starts `ansible-playbook` there with `ssh`, captures stdout/stderr into the existing JobRun
  log model, and removes the temporary key file after dispatch.
- For remote Control Nodes, the workspace is authored with a Control-Host workspace path. Managed Host and Jump Host
  private-key inventory variables point at files under the remote run directory, not at Worker-local filesystem paths.
- Remote staging uses a unique temporary path beside the final run directory, then promotes that staged tree to the
  final path after a successful copy. If staging fails before promotion, the Worker makes a best-effort attempt to
  remove the temporary remote staging directory.
- Before upload, the Worker explicitly creates the remote run parent directory and the temporary staging directory.
  `scp` uploads the prepared workspace contents into that existing staging directory with directory-target semantics;
  it does not rely on `scp` to create deep remote paths implicitly.
- The final remote run path is scoped as
  `{remoteWorkspaceRoot}/{customerId}/control-nodes/{controlNodeId}/runs/{jobRunId}` and is retained after execution
  for operational diagnosis.
- Before remote execution starts, the Worker asks the Control Host shell to resolve the configured
  `RemoteAnsiblePlaybookPath`; if `ansible-playbook` is unavailable there, the run fails through the normal dispatch
  diagnostics and log path instead of falling back to Worker-local Ansible.

## Execution Command

Conceptually:

```text
ansible-playbook -i inventory.yml playbook/site.yml -e @vars.yml
```

The exact implementation belongs to Infrastructure/Worker.

For local/dev Control Hosts, the Worker runs `ansible-playbook` from `NodeControl.Worker` using
`ProcessStartInfo.ArgumentList` with `UseShellExecute = false`. The executable path is configured with
`NodeControl:Execution:AnsiblePlaybookPath` and defaults to `ansible-playbook`.

For remote Control Hosts, the Worker starts `ansible-playbook` over SSH on the selected Control Host. The remote
executable path is configured with `NodeControl:Execution:RemoteAnsiblePlaybookPath` and defaults to
`ansible-playbook`.

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

When a run fails, the Worker also classifies common failure patterns from the captured execution result and recent
Worker output. The persisted `JobRun.ErrorMessage` contains a concise product-oriented diagnosis, and a System log
entry records the same diagnostic while stdout/stderr remain available as raw captured Ansible or SSH output.

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
          ansible_port: 22
          ansible_user: deploy
          ansible_ssh_private_key_file: .nodecontrol/managed-host-keys/{managedNodeId}.key
          ansible_ssh_common_args: -o IdentitiesOnly=yes
```

Do not overbuild dynamic inventory in the MVP.

The current implementation includes target structure, inventory group assignment, and a read-only inventory preview.

- Control Nodes are modeled as customer-scoped records. Hostzustand can queue TCP reachability checks, but
  NodeControl does not perform SSH authentication checks.
- Managed Nodes are modeled as customer-scoped records and can be linked to Inventory Groups. They can carry minimal
  SSH execution metadata: port, optional username, an optional SSH private key Secret reference, and an optional
  one-hop Jump Host reference to another active same-customer Managed Node.
- Inventory Groups generate a YAML preview from active Managed Nodes.
- Archived Managed Nodes are excluded from preview output.
- The API still must not execute Ansible; execution belongs to the Worker.

For Worker execution, the selected Job inventory group is rendered to `inventory.yml`. Active managed nodes
linked to that group become hosts, and archived nodes are excluded. If the group has no active managed nodes,
the JobRun fails before Ansible starts. Simple local/dev targets such as `localhost`, `127.0.0.1`, and `::1`
are rendered with `ansible_connection: local` when no explicit SSH user or key Secret is configured. This keeps the
local showcase path working without pretending to authenticate over SSH.

For real SSH targets, Managed Nodes can provide an SSH port, optional SSH username, and optional SSH private key
Secret reference. If a Managed Node references an SSH private key Secret, only the Worker decrypts that key and
writes a temporary per-host key file under `.nodecontrol/managed-host-keys/` in the run workspace. For local/dev
Control Hosts, inventory points Ansible at that relative file path. For remote Control Hosts, inventory points Ansible
at the corresponding absolute path under the remote Control-Host run directory. In both cases
`ansible_ssh_common_args: -o IdentitiesOnly=yes` is added so Ansible prefers the materialized host key. The dispatcher
removes those key files after local execution and after remote execution best-effort, while leaving the non-sensitive
run workspace structure available for diagnosis.

For one-hop Jump Host execution, the target Managed Node references another active Managed Node from the same customer.
The Application layer rejects self-references, cross-customer references, and nested jump chains. During Worker-side
workspace preparation, inventory keeps the target host's own SSH user/port/key settings and adds an OpenSSH
`ProxyCommand` in `ansible_ssh_common_args` for the jump path. If the Jump Host has its own SSH username, port, or
SSH private key Secret reference, those settings are used in that proxy command and the jump key is materialized by
the Worker into the same temporary managed-host key directory. The API only stores and validates metadata; it never
performs SSH or command execution.

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
- Git imports copy content into managed artifacts and do not create a continuous sync relationship.

## Secrets

Secrets are customer-scoped protected values managed through metadata-only API responses. Plaintext values are accepted
only on create and rotate, protected before persistence, and never returned from normal API responses.

API and Worker share one ASP.NET Data Protection configuration for these protected values:

- `NodeControl:DataProtection:ApplicationName` defaults to `NodeControl`.
- `NodeControl:DataProtection:KeyRingPath` points both processes at the same persistent key ring.
- Development uses `.nodecontrol/data-protection-keys` from the repository root.
- Production-style configuration uses `/var/lib/nodecontrol/data-protection-keys` and should mount/persist that path
  for both API and Worker.

Relative key-ring paths are resolved from the repository root when NodeControl is launched from the source tree, so
API and Worker keep using the same development key ring even when an IDE starts them from different project working
directories.

Secrets protected before this shared key-ring setup, or protected with a different key ring, may not be decryptable by
the Worker. Rotate or recreate those Secrets so they are protected with the shared key ring. SSH private-key Secret
resolution failures distinguish missing, cross-customer, inactive, wrong-kind, missing-value, and unprotect failures
without logging secret plaintext or private key material.

Safe `secret://secret-slug` references are supported for Templates and VariableSets. Reference validation checks
same-customer active metadata without decrypting. During JobRun execution, only the Worker decrypts active referenced
secrets and substitutes them while materializing `vars.yml` / `vars.json` and configured template artifact files.
Resolved values may exist in the local run workspace as execution-ready artifacts, but they are redacted from persisted
run logs and are not returned in API-facing models.

Control Host and Managed Host SSH private keys are also referenced through existing Secret records. API responses expose
only selected Secret ids as configuration metadata; key values are decrypted only by the Worker while preparing or
dispatching a queued Run.

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
- system log entries
- exit code
- final status
- duration

NodeControl does perform a small deterministic failure classification for operational diagnosis. Current classes include
host unreachable, SSH authentication failed, host key / known_hosts failure, missing Secret or SSH key material,
jump-host path failure, inventory or workspace generation failure, Control Host dispatch failure, ansible-playbook
process start failure, timeout, cancellation, and playbook/task failure after Ansible started. This classification is
Worker-side only and does not change the raw log capture model.

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
