using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Nodes;

namespace NodeControl.Application.InventoryGroups;

public sealed class InventoryGroupService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService,
    IClock clock)
{
    public async Task<CustomerServiceResult<IReadOnlyList<InventoryGroupDto>>> ListAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewNodes, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<IReadOnlyList<InventoryGroupDto>>.FromAuthorization(authorization);
        }

        var groups = await dbContext.ListActiveInventoryGroupsAsync(customerId, cancellationToken);
        var dtos = new List<InventoryGroupDto>(groups.Count);
        foreach (var group in groups)
        {
            dtos.Add(await MapAsync(group, cancellationToken));
        }

        return CustomerServiceResult<IReadOnlyList<InventoryGroupDto>>.Ok(dtos);
    }

    public async Task<CustomerServiceResult<InventoryGroupDto>> GetAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid inventoryGroupId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewNodes, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<InventoryGroupDto>.FromAuthorization(authorization);
        }

        var group = await dbContext.FindInventoryGroupAsync(customerId, inventoryGroupId, cancellationToken);
        return group is null
            ? CustomerServiceResult<InventoryGroupDto>.NotFound()
            : CustomerServiceResult<InventoryGroupDto>.Ok(await MapAsync(group, cancellationToken));
    }

    public async Task<CustomerServiceResult<InventoryGroupDto>> CreateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CreateInventoryGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageNodes, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<InventoryGroupDto>.FromAuthorization(authorization);
        }

        if (await dbContext.FindInventoryGroupByNameAsync(customerId, request.Name.Trim(), cancellationToken) is not null)
        {
            return CustomerServiceResult<InventoryGroupDto>.Conflict();
        }

        try
        {
            var group = InventoryGroup.Create(customerId, request.Name, request.Description, clock.UtcNow);
            dbContext.AddInventoryGroup(group);
            await dbContext.SaveChangesAsync(cancellationToken);

            return CustomerServiceResult<InventoryGroupDto>.Ok(await MapAsync(group, cancellationToken));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<InventoryGroupDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<InventoryGroupDto>> UpdateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid inventoryGroupId,
        UpdateInventoryGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageNodes, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<InventoryGroupDto>.FromAuthorization(authorization);
        }

        var group = await dbContext.FindInventoryGroupAsync(customerId, inventoryGroupId, cancellationToken);
        if (group is null)
        {
            return CustomerServiceResult<InventoryGroupDto>.NotFound();
        }

        var existing = await dbContext.FindInventoryGroupByNameAsync(customerId, request.Name.Trim(), cancellationToken);
        if (existing is not null && existing.Id != inventoryGroupId)
        {
            return CustomerServiceResult<InventoryGroupDto>.Conflict();
        }

        try
        {
            group.Update(request.Name, request.Description, clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);

            return CustomerServiceResult<InventoryGroupDto>.Ok(await MapAsync(group, cancellationToken));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<InventoryGroupDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<InventoryGroupDto>> ArchiveAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid inventoryGroupId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageNodes, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<InventoryGroupDto>.FromAuthorization(authorization);
        }

        var group = await dbContext.FindInventoryGroupAsync(customerId, inventoryGroupId, cancellationToken);
        if (group is null)
        {
            return CustomerServiceResult<InventoryGroupDto>.NotFound();
        }

        group.Archive(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CustomerServiceResult<InventoryGroupDto>.Ok(await MapAsync(group, cancellationToken));
    }

    public async Task<CustomerServiceResult<InventoryGroupDto>> AddManagedNodeAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid inventoryGroupId,
        AddManagedNodeToInventoryGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageNodes, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<InventoryGroupDto>.FromAuthorization(authorization);
        }

        var group = await dbContext.FindInventoryGroupAsync(customerId, inventoryGroupId, cancellationToken);
        var managedNode = await dbContext.FindManagedNodeAsync(customerId, request.ManagedNodeId, cancellationToken);
        if (group is null || managedNode is null)
        {
            return CustomerServiceResult<InventoryGroupDto>.NotFound();
        }

        if (group.IsArchived || managedNode.Status != ManagedNodeStatus.Active)
        {
            return CustomerServiceResult<InventoryGroupDto>.NotFound();
        }

        if (await dbContext.FindInventoryGroupNodeAsync(inventoryGroupId, request.ManagedNodeId, cancellationToken) is null)
        {
            dbContext.AddInventoryGroupNode(InventoryGroupNode.Create(group, managedNode, clock.UtcNow));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return CustomerServiceResult<InventoryGroupDto>.Ok(await MapAsync(group, cancellationToken));
    }

    public async Task<CustomerServiceResult<InventoryGroupDto>> RemoveManagedNodeAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid inventoryGroupId,
        Guid managedNodeId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageNodes, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<InventoryGroupDto>.FromAuthorization(authorization);
        }

        var group = await dbContext.FindInventoryGroupAsync(customerId, inventoryGroupId, cancellationToken);
        if (group is null)
        {
            return CustomerServiceResult<InventoryGroupDto>.NotFound();
        }

        if (await dbContext.FindManagedNodeAsync(customerId, managedNodeId, cancellationToken) is null)
        {
            return CustomerServiceResult<InventoryGroupDto>.NotFound();
        }

        var link = await dbContext.FindInventoryGroupNodeAsync(inventoryGroupId, managedNodeId, cancellationToken);
        if (link is not null)
        {
            dbContext.RemoveInventoryGroupNode(link);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return CustomerServiceResult<InventoryGroupDto>.Ok(await MapAsync(group, cancellationToken));
    }

    private async Task<CustomerAuthorizationResult> AuthorizeAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        return await authorizationService.AuthorizeAsync(currentUser, customerId, permission, cancellationToken);
    }

    private async Task<InventoryGroupDto> MapAsync(InventoryGroup group, CancellationToken cancellationToken)
    {
        var managedNodes = await dbContext.ListActiveManagedNodesForInventoryGroupAsync(group.Id, cancellationToken);
        return new InventoryGroupDto(
            group.Id,
            group.CustomerId,
            group.Name,
            group.Description,
            group.CreatedAt,
            group.UpdatedAt,
            group.ArchivedAt,
            managedNodes.Select(managedNode => managedNode.Id).ToArray());
    }
}
