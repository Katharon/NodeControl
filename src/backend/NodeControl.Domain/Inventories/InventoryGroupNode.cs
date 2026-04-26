namespace NodeControl.Domain.Inventories;

public sealed class InventoryGroupNode
{
    private InventoryGroupNode()
    {
    }

    private InventoryGroupNode(Guid inventoryGroupId, Guid managedNodeId, DateTimeOffset createdAt)
    {
        InventoryGroupId = inventoryGroupId;
        ManagedNodeId = managedNodeId;
        CreatedAt = createdAt;
    }

    public Guid InventoryGroupId { get; private set; }

    public Guid ManagedNodeId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static InventoryGroupNode Create(
        InventoryGroup inventoryGroup,
        Nodes.ManagedNode managedNode,
        DateTimeOffset createdAt)
    {
        if (inventoryGroup.CustomerId != managedNode.CustomerId)
        {
            throw new InvalidOperationException("Inventory group and managed node must belong to the same customer.");
        }

        return new InventoryGroupNode(inventoryGroup.Id, managedNode.Id, createdAt);
    }
}
