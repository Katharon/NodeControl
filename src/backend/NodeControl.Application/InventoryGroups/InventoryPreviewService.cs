using System.Text;
using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Domain.Authorization;

namespace NodeControl.Application.InventoryGroups;

public sealed class InventoryPreviewService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService)
{
    public async Task<CustomerServiceResult<InventoryPreviewDto>> GetPreviewAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid inventoryGroupId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            currentUser,
            customerId,
            Permission.ViewNodes,
            cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<InventoryPreviewDto>.FromAuthorization(authorization);
        }

        var group = await dbContext.FindInventoryGroupAsync(customerId, inventoryGroupId, cancellationToken);
        if (group is null || group.IsArchived)
        {
            return CustomerServiceResult<InventoryPreviewDto>.NotFound();
        }

        var managedNodes = await dbContext.ListActiveManagedNodesForInventoryGroupAsync(
            inventoryGroupId,
            cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("all:");
        builder.AppendLine("  children:");
        builder.AppendLine($"    {group.Name}:");
        builder.AppendLine("      hosts:");

        foreach (var managedNode in managedNodes.OrderBy(managedNode => managedNode.Name))
        {
            builder.AppendLine($"        {managedNode.Name}:");
            foreach (var (name, value) in ManagedNodeInventoryVariables.Build(managedNode))
            {
                builder.AppendLine($"          {name}: {value}");
            }
        }

        return CustomerServiceResult<InventoryPreviewDto>.Ok(new InventoryPreviewDto(
            group.Id,
            group.Name,
            "yaml",
            builder.ToString()));
    }
}
