using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.Nodes;

namespace NodeControl.Application.ManagedNodes;

public sealed class ManagedNodeService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService,
    IClock clock)
{
    public async Task<CustomerServiceResult<IReadOnlyList<ManagedNodeDto>>> ListAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            currentUser,
            customerId,
            Permission.ViewNodes,
            cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<IReadOnlyList<ManagedNodeDto>>.FromAuthorization(authorization);
        }

        var managedNodes = await dbContext.ListActiveManagedNodesAsync(customerId, cancellationToken);
        return CustomerServiceResult<IReadOnlyList<ManagedNodeDto>>.Ok(managedNodes.Select(Map).ToArray());
    }

    public async Task<CustomerServiceResult<ManagedNodeDto>> GetAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid managedNodeId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            currentUser,
            customerId,
            Permission.ViewNodes,
            cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<ManagedNodeDto>.FromAuthorization(authorization);
        }

        var managedNode = await dbContext.FindManagedNodeAsync(customerId, managedNodeId, cancellationToken);
        return managedNode is null
            ? CustomerServiceResult<ManagedNodeDto>.NotFound()
            : CustomerServiceResult<ManagedNodeDto>.Ok(Map(managedNode));
    }

    public async Task<CustomerServiceResult<ManagedNodeDto>> CreateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CreateManagedNodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            currentUser,
            customerId,
            Permission.ManageNodes,
            cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<ManagedNodeDto>.FromAuthorization(authorization);
        }

        if (await dbContext.FindManagedNodeByNameAsync(customerId, request.Name.Trim(), cancellationToken) is not null)
        {
            return CustomerServiceResult<ManagedNodeDto>.Conflict();
        }

        try
        {
            var managedNode = ManagedNode.Create(
                customerId,
                request.Name,
                request.Hostname,
                request.SshPort,
                request.OperatingSystem,
                request.Environment,
                request.Description,
                clock.UtcNow);

            dbContext.AddManagedNode(managedNode);
            await dbContext.SaveChangesAsync(cancellationToken);

            return CustomerServiceResult<ManagedNodeDto>.Ok(Map(managedNode));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<ManagedNodeDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<ManagedNodeDto>> UpdateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid managedNodeId,
        UpdateManagedNodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            currentUser,
            customerId,
            Permission.ManageNodes,
            cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<ManagedNodeDto>.FromAuthorization(authorization);
        }

        var managedNode = await dbContext.FindManagedNodeAsync(customerId, managedNodeId, cancellationToken);
        if (managedNode is null)
        {
            return CustomerServiceResult<ManagedNodeDto>.NotFound();
        }

        var existing = await dbContext.FindManagedNodeByNameAsync(customerId, request.Name.Trim(), cancellationToken);
        if (existing is not null && existing.Id != managedNodeId)
        {
            return CustomerServiceResult<ManagedNodeDto>.Conflict();
        }

        try
        {
            managedNode.Update(
                request.Name,
                request.Hostname,
                request.SshPort,
                request.OperatingSystem,
                request.Environment,
                request.Description,
                clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);

            return CustomerServiceResult<ManagedNodeDto>.Ok(Map(managedNode));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<ManagedNodeDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<ManagedNodeDto>> ArchiveAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid managedNodeId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            currentUser,
            customerId,
            Permission.ManageNodes,
            cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<ManagedNodeDto>.FromAuthorization(authorization);
        }

        var managedNode = await dbContext.FindManagedNodeAsync(customerId, managedNodeId, cancellationToken);
        if (managedNode is null)
        {
            return CustomerServiceResult<ManagedNodeDto>.NotFound();
        }

        managedNode.Archive(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CustomerServiceResult<ManagedNodeDto>.Ok(Map(managedNode));
    }

    private static ManagedNodeDto Map(ManagedNode managedNode)
    {
        return new ManagedNodeDto(
            managedNode.Id,
            managedNode.CustomerId,
            managedNode.Name,
            managedNode.Hostname,
            managedNode.SshPort,
            managedNode.OperatingSystem,
            managedNode.Environment,
            managedNode.Description,
            managedNode.Status,
            managedNode.CreatedAt,
            managedNode.UpdatedAt,
            managedNode.ArchivedAt);
    }
}
