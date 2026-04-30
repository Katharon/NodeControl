namespace NodeControl.Application.GitRepositories;

public sealed record UpdateGitRepositoryRequest(
    string Name,
    string RepositoryUrl,
    string? Branch,
    string? Revision,
    string? Subpath);
