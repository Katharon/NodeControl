using System.Text.Json;
using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Application.Validation;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.Playbooks;

namespace NodeControl.Application.Playbooks;

public sealed class PlaybookService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService,
    YamlJsonValidationService validationService,
    IClock clock)
{
    private static readonly JsonSerializerOptions ArtifactJsonOptions = new(JsonSerializerDefaults.Web);
    private const int MaxArtifactFileCount = 100;
    private const int MaxArtifactFileLength = 200000;
    private const int MaxArtifactTotalLength = 1000000;

    public async Task<CustomerServiceResult<IReadOnlyList<PlaybookDto>>> ListAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewPlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<IReadOnlyList<PlaybookDto>>.FromAuthorization(authorization);
        }

        var playbooks = await dbContext.ListActivePlaybooksAsync(customerId, cancellationToken);
        return CustomerServiceResult<IReadOnlyList<PlaybookDto>>.Ok(playbooks.Select(Map).ToArray());
    }

    public async Task<CustomerServiceResult<PlaybookDto>> GetAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid playbookId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewPlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<PlaybookDto>.FromAuthorization(authorization);
        }

        var playbook = await dbContext.FindPlaybookAsync(customerId, playbookId, cancellationToken);
        return playbook is null
            ? CustomerServiceResult<PlaybookDto>.NotFound()
            : CustomerServiceResult<PlaybookDto>.Ok(Map(playbook));
    }

    public async Task<CustomerServiceResult<PlaybookDto>> CreateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CreatePlaybookRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManagePlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<PlaybookDto>.FromAuthorization(authorization);
        }

        if (!IsSupportedSourceType(request.SourceType))
        {
            return CustomerServiceResult<PlaybookDto>.BadRequest();
        }

        if (!TryPrepareContent(
            request.SourceType,
            request.InlineContent,
            request.EntryFilePath,
            request.ArtifactFiles,
            out var artifactFilesJson))
        {
            return CustomerServiceResult<PlaybookDto>.BadRequest();
        }

        if (await dbContext.FindPlaybookBySlugAsync(customerId, request.Slug.Trim(), cancellationToken) is not null)
        {
            return CustomerServiceResult<PlaybookDto>.Conflict();
        }

        try
        {
            var playbook = Playbook.Create(
                customerId,
                request.Name,
                request.Slug,
                request.Description,
                request.SourceType,
                request.InlineContent,
                request.EntryFilePath,
                clock.UtcNow,
                artifactFilesJson);

            dbContext.AddPlaybook(playbook);
            await dbContext.SaveChangesAsync(cancellationToken);

            return CustomerServiceResult<PlaybookDto>.Ok(Map(playbook));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<PlaybookDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<PlaybookDto>> UpdateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid playbookId,
        UpdatePlaybookRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManagePlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<PlaybookDto>.FromAuthorization(authorization);
        }

        if (!IsSupportedSourceType(request.SourceType))
        {
            return CustomerServiceResult<PlaybookDto>.BadRequest();
        }

        if (!TryPrepareContent(
            request.SourceType,
            request.InlineContent,
            request.EntryFilePath,
            request.ArtifactFiles,
            out var artifactFilesJson))
        {
            return CustomerServiceResult<PlaybookDto>.BadRequest();
        }

        var playbook = await dbContext.FindPlaybookAsync(customerId, playbookId, cancellationToken);
        if (playbook is null)
        {
            return CustomerServiceResult<PlaybookDto>.NotFound();
        }

        var existing = await dbContext.FindPlaybookBySlugAsync(customerId, request.Slug.Trim(), cancellationToken);
        if (existing is not null && existing.Id != playbookId)
        {
            return CustomerServiceResult<PlaybookDto>.Conflict();
        }

        try
        {
            playbook.Update(
                request.Name,
                request.Slug,
                request.Description,
                request.SourceType,
                request.InlineContent,
                request.EntryFilePath,
                clock.UtcNow,
                artifactFilesJson);
            await dbContext.SaveChangesAsync(cancellationToken);

            return CustomerServiceResult<PlaybookDto>.Ok(Map(playbook));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<PlaybookDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<PlaybookDto>> ArchiveAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid playbookId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManagePlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<PlaybookDto>.FromAuthorization(authorization);
        }

        var playbook = await dbContext.FindPlaybookAsync(customerId, playbookId, cancellationToken);
        if (playbook is null)
        {
            return CustomerServiceResult<PlaybookDto>.NotFound();
        }

        playbook.Archive(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CustomerServiceResult<PlaybookDto>.Ok(Map(playbook));
    }

    public async Task<CustomerServiceResult<PlaybookValidationResultDto>> ValidateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid playbookId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewPlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<PlaybookValidationResultDto>.FromAuthorization(authorization);
        }

        var playbook = await dbContext.FindPlaybookAsync(customerId, playbookId, cancellationToken);
        if (playbook is null)
        {
            return CustomerServiceResult<PlaybookValidationResultDto>.NotFound();
        }

        var result = ValidateStoredContent(playbook);
        return CustomerServiceResult<PlaybookValidationResultDto>.Ok(
            new PlaybookValidationResultDto(result.IsValid, result.Message, result.Errors));
    }

    private async Task<CustomerAuthorizationResult> AuthorizeAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        return await authorizationService.AuthorizeAsync(currentUser, customerId, permission, cancellationToken);
    }

    private static bool IsSupportedSourceType(PlaybookSourceType sourceType)
    {
        return sourceType is PlaybookSourceType.InlineYaml or PlaybookSourceType.ArtifactDirectory;
    }

    private bool TryPrepareContent(
        PlaybookSourceType sourceType,
        string? inlineContent,
        string? entryFilePath,
        IReadOnlyList<PlaybookArtifactFileDto>? artifactFiles,
        out string? artifactFilesJson)
    {
        artifactFilesJson = null;

        if (sourceType == PlaybookSourceType.InlineYaml)
        {
            if (artifactFiles is { Count: > 0 })
            {
                return false;
            }

            return validationService.ValidateYaml(inlineContent).IsValid;
        }

        if (sourceType != PlaybookSourceType.ArtifactDirectory
            || !string.IsNullOrWhiteSpace(inlineContent)
            || artifactFiles is null
            || artifactFiles.Count == 0
            || artifactFiles.Count > MaxArtifactFileCount)
        {
            return false;
        }

        try
        {
            var normalizedEntryFilePath = NormalizeArtifactPath(entryFilePath);
            var normalizedFiles = NormalizeArtifactFiles(artifactFiles);
            var entryFile = normalizedFiles.FirstOrDefault(file => file.Path == normalizedEntryFilePath);
            if (entryFile is null)
            {
                return false;
            }

            if (!validationService.ValidateYaml(entryFile.Content).IsValid)
            {
                return false;
            }

            artifactFilesJson = JsonSerializer.Serialize(normalizedFiles, ArtifactJsonOptions);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private ValidationResult ValidateStoredContent(Playbook playbook)
    {
        if (playbook.SourceType == PlaybookSourceType.InlineYaml)
        {
            return validationService.ValidateYaml(playbook.InlineContent);
        }

        if (playbook.SourceType == PlaybookSourceType.ArtifactDirectory)
        {
            try
            {
                var files = DeserializeArtifactFiles(playbook.ArtifactFilesJson);
                var entryFilePath = NormalizeArtifactPath(playbook.EntryFilePath);
                var entryFile = files.FirstOrDefault(file => file.Path == entryFilePath);
                return entryFile is null
                    ? ValidationResult.Invalid("Artifact entry file is missing.", ["Entry file was not found in artifact files."])
                    : validationService.ValidateYaml(entryFile.Content) with { Message = "Artifact entry file YAML syntax is valid." };
            }
            catch (ArgumentException exception)
            {
                return ValidationResult.Invalid("Artifact files are invalid.", [exception.Message]);
            }
        }

        return ValidationResult.Invalid("Playbook source type is not supported.", ["Playbook source type is not supported."]);
    }

    private static IReadOnlyList<PlaybookArtifactFileDto> NormalizeArtifactFiles(IReadOnlyList<PlaybookArtifactFileDto> artifactFiles)
    {
        var seenPaths = new HashSet<string>(StringComparer.Ordinal);
        var normalizedFiles = new List<PlaybookArtifactFileDto>(artifactFiles.Count);
        var totalLength = 0;

        foreach (var artifactFile in artifactFiles)
        {
            var path = NormalizeArtifactPath(artifactFile.Path);
            if (artifactFile.Content is null)
            {
                throw new ArgumentException("Artifact file content is required.", nameof(artifactFiles));
            }

            if (!seenPaths.Add(path))
            {
                throw new ArgumentException("Artifact file paths must be unique.", nameof(artifactFiles));
            }

            if (artifactFile.Content.Length > MaxArtifactFileLength)
            {
                throw new ArgumentException("Artifact file content is too large.", nameof(artifactFiles));
            }

            totalLength += artifactFile.Content.Length;
            if (totalLength > MaxArtifactTotalLength)
            {
                throw new ArgumentException("Artifact file content is too large.", nameof(artifactFiles));
            }

            normalizedFiles.Add(new PlaybookArtifactFileDto(path, artifactFile.Content));
        }

        return normalizedFiles;
    }

    private static string NormalizeArtifactPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Artifact file path is required.", nameof(path));
        }

        var normalized = path.Trim().Replace('\\', '/');
        if (normalized.Length > 500
            || normalized.StartsWith("/", StringComparison.Ordinal)
            || normalized.EndsWith("/", StringComparison.Ordinal)
            || Path.IsPathRooted(normalized)
            || normalized.Split('/').Any(part => string.IsNullOrWhiteSpace(part) || part == "." || part == ".."))
        {
            throw new ArgumentException("Artifact file path is invalid.", nameof(path));
        }

        return normalized;
    }

    private static IReadOnlyList<PlaybookArtifactFileDto> DeserializeArtifactFiles(string? artifactFilesJson)
    {
        if (string.IsNullOrWhiteSpace(artifactFilesJson))
        {
            return [];
        }

        var files = JsonSerializer.Deserialize<IReadOnlyList<PlaybookArtifactFileDto>>(artifactFilesJson, ArtifactJsonOptions);
        return files is null ? [] : NormalizeArtifactFiles(files);
    }

    private static PlaybookDto Map(Playbook playbook)
    {
        return new PlaybookDto(
            playbook.Id,
            playbook.CustomerId,
            playbook.Name,
            playbook.Slug,
            playbook.Description,
            playbook.SourceType,
            playbook.Status,
            playbook.InlineContent,
            playbook.EntryFilePath,
            playbook.CreatedAt,
            playbook.UpdatedAt,
            playbook.ArchivedAt,
            DeserializeArtifactFiles(playbook.ArtifactFilesJson));
    }
}
