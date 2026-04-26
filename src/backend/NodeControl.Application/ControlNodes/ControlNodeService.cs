using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.Nodes;

namespace NodeControl.Application.ControlNodes;

public sealed class ControlNodeService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService,
    IClock clock)
{
    public async Task<CustomerServiceResult<IReadOnlyList<ControlNodeDto>>> ListAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewNodes, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<IReadOnlyList<ControlNodeDto>>.FromAuthorization(authorization);
        }

        var controlNodes = await dbContext.ListActiveControlNodesAsync(customerId, cancellationToken);
        return CustomerServiceResult<IReadOnlyList<ControlNodeDto>>.Ok(controlNodes.Select(Map).ToArray());
    }

    public async Task<CustomerServiceResult<ControlNodeDto>> GetAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid controlNodeId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewNodes, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<ControlNodeDto>.FromAuthorization(authorization);
        }

        var controlNode = await dbContext.FindControlNodeAsync(customerId, controlNodeId, cancellationToken);
        return controlNode is null
            ? CustomerServiceResult<ControlNodeDto>.NotFound()
            : CustomerServiceResult<ControlNodeDto>.Ok(Map(controlNode));
    }

    public async Task<CustomerServiceResult<ControlNodeDto>> CreateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CreateControlNodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageNodes, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<ControlNodeDto>.FromAuthorization(authorization);
        }

        if (await dbContext.FindControlNodeByNameAsync(customerId, request.Name.Trim(), cancellationToken) is not null)
        {
            return CustomerServiceResult<ControlNodeDto>.Conflict();
        }

        try
        {
            var controlNode = ControlNode.Create(
                customerId,
                request.Name,
                request.Hostname,
                request.SshPort,
                request.Description,
                clock.UtcNow);

            dbContext.AddControlNode(controlNode);
            await dbContext.SaveChangesAsync(cancellationToken);

            return CustomerServiceResult<ControlNodeDto>.Ok(Map(controlNode));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<ControlNodeDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<ControlNodeDto>> UpdateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid controlNodeId,
        UpdateControlNodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageNodes, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<ControlNodeDto>.FromAuthorization(authorization);
        }

        var controlNode = await dbContext.FindControlNodeAsync(customerId, controlNodeId, cancellationToken);
        if (controlNode is null)
        {
            return CustomerServiceResult<ControlNodeDto>.NotFound();
        }

        var existing = await dbContext.FindControlNodeByNameAsync(customerId, request.Name.Trim(), cancellationToken);
        if (existing is not null && existing.Id != controlNodeId)
        {
            return CustomerServiceResult<ControlNodeDto>.Conflict();
        }

        try
        {
            controlNode.Update(request.Name, request.Hostname, request.SshPort, request.Description, clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);

            return CustomerServiceResult<ControlNodeDto>.Ok(Map(controlNode));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<ControlNodeDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<ControlNodeDto>> ArchiveAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid controlNodeId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageNodes, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<ControlNodeDto>.FromAuthorization(authorization);
        }

        var controlNode = await dbContext.FindControlNodeAsync(customerId, controlNodeId, cancellationToken);
        if (controlNode is null)
        {
            return CustomerServiceResult<ControlNodeDto>.NotFound();
        }

        controlNode.Archive(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CustomerServiceResult<ControlNodeDto>.Ok(Map(controlNode));
    }

    private async Task<CustomerAuthorizationResult> AuthorizeAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        return await authorizationService.AuthorizeAsync(currentUser, customerId, permission, cancellationToken);
    }

    private static ControlNodeDto Map(ControlNode controlNode)
    {
        return new ControlNodeDto(
            controlNode.Id,
            controlNode.CustomerId,
            controlNode.Name,
            controlNode.Hostname,
            controlNode.SshPort,
            controlNode.Description,
            controlNode.Status,
            controlNode.CreatedAt,
            controlNode.UpdatedAt,
            controlNode.ArchivedAt);
    }
}
