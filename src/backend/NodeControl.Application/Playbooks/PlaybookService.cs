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

        if (!validationService.ValidateYaml(request.InlineContent).IsValid)
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
                clock.UtcNow);

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

        if (!validationService.ValidateYaml(request.InlineContent).IsValid)
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
                clock.UtcNow);
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

        var result = validationService.ValidateYaml(playbook.InlineContent);
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
        return sourceType == PlaybookSourceType.InlineYaml;
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
            playbook.ArchivedAt);
    }
}
