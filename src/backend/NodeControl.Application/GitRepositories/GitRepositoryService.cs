using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.GitRepositories;

namespace NodeControl.Application.GitRepositories;

public sealed class GitRepositoryService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService,
    IClock clock)
{
    public async Task<CustomerServiceResult<IReadOnlyList<GitRepositoryDto>>> ListAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewPlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<IReadOnlyList<GitRepositoryDto>>.FromAuthorization(authorization);
        }

        var repositories = await dbContext.ListActiveGitRepositoriesAsync(customerId, cancellationToken);
        return CustomerServiceResult<IReadOnlyList<GitRepositoryDto>>.Ok(repositories.Select(Map).ToArray());
    }

    public async Task<CustomerServiceResult<GitRepositoryDto>> GetAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid gitRepositoryId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewPlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<GitRepositoryDto>.FromAuthorization(authorization);
        }

        var repository = await dbContext.FindGitRepositoryAsync(customerId, gitRepositoryId, cancellationToken);
        return repository is null
            ? CustomerServiceResult<GitRepositoryDto>.NotFound()
            : CustomerServiceResult<GitRepositoryDto>.Ok(Map(repository));
    }

    public async Task<CustomerServiceResult<GitRepositoryDto>> CreateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CreateGitRepositoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManagePlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<GitRepositoryDto>.FromAuthorization(authorization);
        }

        try
        {
            var repository = GitRepository.Create(
                customerId,
                request.Name,
                request.RepositoryUrl,
                request.Branch,
                request.Revision,
                request.Subpath,
                clock.UtcNow);

            dbContext.AddGitRepository(repository);
            await dbContext.SaveChangesAsync(cancellationToken);

            return CustomerServiceResult<GitRepositoryDto>.Ok(Map(repository));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<GitRepositoryDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<GitRepositoryDto>> UpdateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid gitRepositoryId,
        UpdateGitRepositoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManagePlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<GitRepositoryDto>.FromAuthorization(authorization);
        }

        var repository = await dbContext.FindGitRepositoryAsync(customerId, gitRepositoryId, cancellationToken);
        if (repository is null)
        {
            return CustomerServiceResult<GitRepositoryDto>.NotFound();
        }

        if (repository.Status == GitRepositoryStatus.Archived)
        {
            return CustomerServiceResult<GitRepositoryDto>.BadRequest();
        }

        try
        {
            repository.Update(
                request.Name,
                request.RepositoryUrl,
                request.Branch,
                request.Revision,
                request.Subpath,
                clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);

            return CustomerServiceResult<GitRepositoryDto>.Ok(Map(repository));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<GitRepositoryDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<GitRepositoryDto>> ArchiveAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid gitRepositoryId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManagePlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<GitRepositoryDto>.FromAuthorization(authorization);
        }

        var repository = await dbContext.FindGitRepositoryAsync(customerId, gitRepositoryId, cancellationToken);
        if (repository is null)
        {
            return CustomerServiceResult<GitRepositoryDto>.NotFound();
        }

        repository.Archive(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CustomerServiceResult<GitRepositoryDto>.Ok(Map(repository));
    }

    private async Task<CustomerAuthorizationResult> AuthorizeAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        return await authorizationService.AuthorizeAsync(currentUser, customerId, permission, cancellationToken);
    }

    private static GitRepositoryDto Map(GitRepository repository)
    {
        return new GitRepositoryDto(
            repository.Id,
            repository.CustomerId,
            repository.Name,
            repository.RepositoryUrl,
            repository.Branch,
            repository.Revision,
            repository.Subpath,
            repository.Status,
            repository.CreatedAt,
            repository.UpdatedAt,
            repository.ArchivedAt);
    }
}
