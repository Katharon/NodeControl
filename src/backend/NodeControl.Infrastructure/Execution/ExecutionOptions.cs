namespace NodeControl.Infrastructure.Execution;

public sealed class ExecutionOptions
{
    public const string SectionName = "NodeControl:Execution";

    public string RunWorkspaceRoot { get; set; } = "/var/lib/nodecontrol/runs";

    public string AnsiblePlaybookPath { get; set; } = "ansible-playbook";

    public bool AllowLocalControlNodeExecution { get; set; } = true;

    public string[] LocalControlNodeHostnames { get; set; } = ["localhost", "127.0.0.1", "::1"];
}
