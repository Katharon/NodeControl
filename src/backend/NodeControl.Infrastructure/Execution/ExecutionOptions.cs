namespace NodeControl.Infrastructure.Execution;

public sealed class ExecutionOptions
{
    public const string SectionName = "NodeControl:Execution";

    public string RunWorkspaceRoot { get; set; } = "/var/lib/nodecontrol/runs";

    public string AnsiblePlaybookPath { get; set; } = "ansible-playbook";
}
