namespace NodeControl.Application.GitRepositories;

public sealed record CreateGitRepositoryRequest(
    string Name,
    string RepositoryUrl,
    string? Branch,
    string? Revision,
    string? Subpath);
