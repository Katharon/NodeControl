using System.Text.RegularExpressions;

namespace NodeControl.Domain.GitRepositories;

public sealed partial class GitRepository
{
    private GitRepository()
    {
    }

    private GitRepository(
        Guid id,
        Guid customerId,
        string name,
        string repositoryUrl,
        string? branch,
        string? revision,
        string? subpath,
        DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        Name = NormalizeName(name);
        RepositoryUrl = NormalizeRepositoryUrl(repositoryUrl);
        Branch = NormalizeOptionalRef(branch, nameof(branch));
        Revision = NormalizeOptionalRef(revision, nameof(revision));
        Subpath = NormalizeOptionalSubpath(subpath);
        Status = GitRepositoryStatus.Active;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string RepositoryUrl { get; private set; } = string.Empty;

    public string? Branch { get; private set; }

    public string? Revision { get; private set; }

    public string? Subpath { get; private set; }

    public GitRepositoryStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static GitRepository Create(
        Guid customerId,
        string name,
        string repositoryUrl,
        string? branch,
        string? revision,
        string? subpath,
        DateTimeOffset createdAt)
    {
        return new GitRepository(Guid.NewGuid(), customerId, name, repositoryUrl, branch, revision, subpath, createdAt);
    }

    public void Update(
        string name,
        string repositoryUrl,
        string? branch,
        string? revision,
        string? subpath,
        DateTimeOffset updatedAt)
    {
        Name = NormalizeName(name);
        RepositoryUrl = NormalizeRepositoryUrl(repositoryUrl);
        Branch = NormalizeOptionalRef(branch, nameof(branch));
        Revision = NormalizeOptionalRef(revision, nameof(revision));
        Subpath = NormalizeOptionalSubpath(subpath);
        UpdatedAt = updatedAt;
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (Status == GitRepositoryStatus.Archived)
        {
            return;
        }

        Status = GitRepositoryStatus.Archived;
        ArchivedAt = archivedAt;
        UpdatedAt = archivedAt;
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmed = name.Trim();
        if (trimmed.Length > 200)
        {
            throw new ArgumentException("Git repository name must be at most 200 characters.", nameof(name));
        }

        return trimmed;
    }

    private static string NormalizeRepositoryUrl(string repositoryUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryUrl);
        var trimmed = repositoryUrl.Trim();
        if (trimmed.Length > 1000)
        {
            throw new ArgumentException("Git repository URL must be at most 1000 characters.", nameof(repositoryUrl));
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps
                || uri.Scheme == Uri.UriSchemeSsh))
        {
            return trimmed;
        }

        if (ScpLikeGitUrlRegex().IsMatch(trimmed))
        {
            return trimmed;
        }

        throw new ArgumentException("Git repository URL is invalid.", nameof(repositoryUrl));
    }

    private static string? NormalizeOptionalRef(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 200
            || trimmed.StartsWith("-", StringComparison.Ordinal)
            || trimmed.Contains("..", StringComparison.Ordinal)
            || trimmed.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("Git reference is invalid.", name);
        }

        return trimmed;
    }

    private static string? NormalizeOptionalSubpath(string? subpath)
    {
        if (string.IsNullOrWhiteSpace(subpath))
        {
            return null;
        }

        var normalized = subpath.Trim().Replace('\\', '/');
        if (normalized.Length > 500
            || normalized.StartsWith("/", StringComparison.Ordinal)
            || normalized.EndsWith("/", StringComparison.Ordinal)
            || Path.IsPathRooted(normalized)
            || normalized.Split('/').Any(part => string.IsNullOrWhiteSpace(part) || part == "." || part == ".."))
        {
            throw new ArgumentException("Git repository subpath is invalid.", nameof(subpath));
        }

        return normalized;
    }

    [GeneratedRegex("^[A-Za-z0-9_.-]+@[^:]+:[^\\s]+$")]
    private static partial Regex ScpLikeGitUrlRegex();
}
