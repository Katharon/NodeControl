using System.Text;

namespace NodeControl.Application.JobRuns;

public sealed record JobRunFailureDiagnostic(
    JobRunFailureCategory Category,
    string Title,
    string Summary,
    string? NextStep)
{
    public JobRunFailureDiagnosticDto ToDto()
    {
        return new JobRunFailureDiagnosticDto(Category.ToString(), Title, Summary, NextStep);
    }

    public string ToErrorMessage()
    {
        return string.IsNullOrWhiteSpace(NextStep)
            ? $"{Title}: {Summary}"
            : $"{Title}: {Summary} Next step: {NextStep}";
    }

    public string ToLogMessage()
    {
        return string.IsNullOrWhiteSpace(NextStep)
            ? $"Diagnostic: {Title}. {Summary}"
            : $"Diagnostic: {Title}. {Summary} Next step: {NextStep}";
    }
}

public static class JobRunFailureDiagnostics
{
    public static JobRunFailureDiagnostic Classify(
        JobRunFailurePhase phase,
        string? errorMessage,
        int? exitCode = null,
        IEnumerable<string>? recentLogLines = null)
    {
        var text = BuildSearchText(errorMessage, recentLogLines);

        if (phase == JobRunFailurePhase.Cancellation)
        {
            return Cancelled(errorMessage);
        }

        if (phase == JobRunFailurePhase.Timeout || ContainsAny(text, "timed out", "timeout", "exceeded the timeout"))
        {
            return TimedOut();
        }

        if (ContainsAny(text, "host key verification failed", "remote host identification has changed", "offending", "known_hosts"))
        {
            return new JobRunFailureDiagnostic(
                JobRunFailureCategory.HostKeyVerificationFailed,
                "Host key verification failed",
                "SSH refused the host key or known_hosts entry for a control host, managed host, or jump host.",
                "Verify the host fingerprint and update the known_hosts state used by the Worker or control host.");
        }

        if (ContainsAny(text, "unprotected private key file", "bad permissions", "are too open"))
        {
            return new JobRunFailureDiagnostic(
                JobRunFailureCategory.SshPrivateKeyFilePermissionsTooOpen,
                "SSH private key file permissions are too open",
                "SSH refused to use a materialized private-key file because its permissions allow broader access than OpenSSH permits.",
                "Ensure materialized private key files on the remote Control Host are chmod 600 and key directories are chmod 700.");
        }

        if (ContainsAny(text, "proxycommand", "jump host connection failed", "jump host", "bastion", "stdio forwarding failed", "channel 0: open failed", "unknown port 65535"))
        {
            return new JobRunFailureDiagnostic(
                JobRunFailureCategory.JumpHostConnectionFailed,
                "Jump host connection failed",
                "The target host is configured through a jump/bastion path, and that proxy path failed before Ansible could reach the target.",
                "Check the jump host address, SSH user, port, key Secret, and network path from the control host.");
        }

        if (ContainsAny(text, "ssh authentication failed", "permission denied (publickey", "permission denied, please try again", "authentication failed", "too many authentication failures"))
        {
            return new JobRunFailureDiagnostic(
                JobRunFailureCategory.SshAuthenticationFailed,
                "SSH authentication failed",
                "SSH reached the host but authentication was rejected.",
                "Check the configured SSH user and private-key Secret for the control host, managed host, or jump host.");
        }

        if (ContainsAny(text, "ssh key or secret is unavailable", "secret reference", "ssh private key secret", "private key material is unavailable", "identity file", "not accessible", "invalid format", "error in libcrypto"))
        {
            if (ContainsAny(text, "belongs to a different customer"))
            {
                return new JobRunFailureDiagnostic(
                    JobRunFailureCategory.MissingSecretOrSshKey,
                    "SSH key Secret belongs to a different customer",
                    "A referenced SSH private-key Secret exists, but it is outside the Run's customer scope.",
                    "Select an active SSH private-key Secret from the same customer.");
            }

            if (ContainsAny(text, "expected sshprivatekey"))
            {
                return new JobRunFailureDiagnostic(
                    JobRunFailureCategory.MissingSecretOrSshKey,
                    "SSH key Secret has the wrong kind",
                    "A referenced Secret is not an SSH private-key Secret.",
                    "Change the reference to an active SshPrivateKey Secret or rotate/recreate the Secret with the correct kind.");
            }

            if (ContainsAny(text, " is archived", " is inactive"))
            {
                return new JobRunFailureDiagnostic(
                    JobRunFailureCategory.MissingSecretOrSshKey,
                    "SSH key Secret is inactive",
                    "A referenced SSH private-key Secret is not active.",
                    "Select an active SSH private-key Secret or rotate/recreate the archived one.");
            }

            if (ContainsAny(text, "could not be unprotected"))
            {
                return new JobRunFailureDiagnostic(
                    JobRunFailureCategory.MissingSecretOrSshKey,
                    "SSH key Secret could not be decrypted",
                    "A referenced SSH private-key Secret could not be unprotected with the current Data Protection key ring.",
                    "Rotate or recreate the Secret after confirming API and Worker use the same Data Protection key ring.");
            }

            if (ContainsAny(text, "has no protected value"))
            {
                return new JobRunFailureDiagnostic(
                    JobRunFailureCategory.MissingSecretOrSshKey,
                    "SSH key Secret has no value",
                    "A referenced SSH private-key Secret has no protected value stored.",
                    "Rotate or recreate the Secret with the SSH private key value.");
            }

            if (ContainsAny(text, "was not found"))
            {
                return new JobRunFailureDiagnostic(
                    JobRunFailureCategory.MissingSecretOrSshKey,
                    "SSH key Secret was not found",
                    "A referenced SSH private-key Secret no longer exists.",
                    "Select an active SSH private-key Secret that still exists for this customer.");
            }

            return new JobRunFailureDiagnostic(
                JobRunFailureCategory.MissingSecretOrSshKey,
                "SSH key or Secret is unavailable",
                "A required Secret or SSH private-key file could not be resolved or used for execution.",
                "Check that referenced Secrets are active, same-customer, the correct kind, and contain valid SSH key material.");
        }

        if (ContainsAny(text, "host unreachable", "ssh: connect to host", "connection refused", "connection timed out", "no route to host", "network is unreachable", "could not resolve hostname", "name or service not known", "temporary failure in name resolution", "failed to connect to the host via ssh", "unreachable!"))
        {
            return new JobRunFailureDiagnostic(
                JobRunFailureCategory.HostUnreachable,
                "Host unreachable",
                "The control host or a managed host could not be reached over the configured network path.",
                "Check hostname, port, DNS, firewall rules, VPN/routing, and whether the host is online.");
        }

        if (ContainsAny(text, "inventory generation failed", "inventory group has no active managed nodes", "inventory group contains", "managed node with an unavailable jump host", "unavailable inventory group"))
        {
            return new JobRunFailureDiagnostic(
                JobRunFailureCategory.InventoryGenerationFailed,
                "Inventory generation failed",
                "NodeControl could not build a valid customer-scoped inventory for this run.",
                "Check that the inventory contains active hosts and that jump-host references are valid.");
        }

        if (phase == JobRunFailurePhase.Workspace
            || ContainsAny(text, "workspace generation failed", "execution workspace", "artifact-directory playbook", "artifact file", "template artifact", "playbook source type", "inline playbook content"))
        {
            return new JobRunFailureDiagnostic(
                JobRunFailureCategory.WorkspaceGenerationFailed,
                "Workspace generation failed",
                "The Worker could not materialize the run workspace before Ansible started.",
                "Check playbook artifacts, template artifact paths, workspace permissions, and available disk space.");
        }

        if (phase == JobRunFailurePhase.ProcessStart || ContainsAny(text, "ansible-playbook could not be started"))
        {
            return new JobRunFailureDiagnostic(
                JobRunFailureCategory.AnsibleProcessStartFailed,
                "ansible-playbook could not be started",
                "The Worker or remote control host could not start the configured ansible-playbook executable.",
                "Check the ansible-playbook path and whether Ansible is installed on the execution host.");
        }

        if (ContainsAny(text, "control host dispatch failed"))
        {
            return new JobRunFailureDiagnostic(
                JobRunFailureCategory.ControlHostDispatchFailed,
                "Control host dispatch failed",
                "The Worker could not stage, promote, or start execution on the selected Control Host.",
                "Check the Control Host SSH settings, remote workspace root, Worker access, and ansible-playbook availability there.");
        }

        if (phase == JobRunFailurePhase.Dispatch
            && ContainsAny(text, "control host dispatch failed", "remote staging workspace", "remote control node", "remote dispatch", "dispatch manifest", "scp could not be started", "ssh could not be started", "requires ssh remote dispatch settings"))
        {
            return new JobRunFailureDiagnostic(
                JobRunFailureCategory.ControlHostDispatchFailed,
                "Control host dispatch failed",
                "The Worker could not stage, promote, or start execution on the selected Control Host.",
                "Check the Control Host SSH settings, remote workspace root, Worker access, and ansible-playbook availability there.");
        }

        if (phase == JobRunFailurePhase.PlaybookExecution
            || ContainsAny(text, "ansible playbook execution failed", "play recap", "task [", "fatal:", "failed=")
            || exitCode is not null)
        {
            return new JobRunFailureDiagnostic(
                JobRunFailureCategory.PlaybookExecutionFailed,
                "Ansible playbook execution failed",
                "Ansible started but the playbook or one of its tasks returned a failure.",
                "Inspect stdout/stderr logs for the failing task and its module output.");
        }

        if (phase == JobRunFailurePhase.Dispatch)
        {
            return new JobRunFailureDiagnostic(
                JobRunFailureCategory.ControlHostDispatchFailed,
                "Control host dispatch failed",
                "The run failed while dispatching through the selected Control Host.",
                "Check Control Host connectivity, credentials, workspace permissions, and remote Ansible setup.");
        }

        return new JobRunFailureDiagnostic(
            JobRunFailureCategory.Unknown,
            "Execution failed",
            string.IsNullOrWhiteSpace(errorMessage)
                ? "The run failed, but NodeControl could not match it to a known failure class."
                : errorMessage.Trim(),
            "Review the raw system, stdout, and stderr logs for details.");
    }

    public static JobRunFailureDiagnosticDto? FromStoredErrorMessage(string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return null;
        }

        return Classify(JobRunFailurePhase.Unknown, errorMessage).ToDto();
    }

    private static JobRunFailureDiagnostic TimedOut()
    {
        return new JobRunFailureDiagnostic(
            JobRunFailureCategory.TimedOut,
            "Execution timed out",
            "The run exceeded its configured timeout before completion.",
            "Check whether the playbook is waiting on SSH, package managers, long-running tasks, or unreachable hosts.");
    }

    private static JobRunFailureDiagnostic Cancelled(string? errorMessage)
    {
        return new JobRunFailureDiagnostic(
            JobRunFailureCategory.Cancelled,
            "Run cancelled",
            string.IsNullOrWhiteSpace(errorMessage) ? "Execution stopped because cancellation was requested." : errorMessage.Trim(),
            null);
    }

    private static string BuildSearchText(string? errorMessage, IEnumerable<string>? recentLogLines)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            builder.AppendLine(errorMessage);
        }

        if (recentLogLines is not null)
        {
            foreach (var line in recentLogLines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    builder.AppendLine(line);
                }
            }
        }

        return builder.ToString().ToLowerInvariant();
    }

    private static bool ContainsAny(string value, params string[] patterns)
    {
        return patterns.Any(pattern => value.Contains(pattern, StringComparison.Ordinal));
    }
}
