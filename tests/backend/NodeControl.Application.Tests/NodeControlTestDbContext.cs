using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Users;

namespace NodeControl.Application.Tests;

public sealed class NodeControlTestDbContext : INodeControlDbContext
{
    public List<User> Users { get; } = [];

    public List<ExternalIdentity> ExternalIdentities { get; } = [];

    public List<Customer> Customers { get; } = [];

    public List<CustomerMembership> CustomerMemberships { get; } = [];

    public List<ControlNode> ControlNodes { get; } = [];

    public List<ManagedNode> ManagedNodes { get; } = [];

    public List<InventoryGroup> InventoryGroups { get; } = [];

    public List<InventoryGroupNode> InventoryGroupNodes { get; } = [];

    public Task<ExternalIdentity?> FindExternalIdentityAsync(
        string provider,
        string subject,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ExternalIdentities.FirstOrDefault(identity =>
            identity.Provider == provider && identity.Subject == subject));
    }

    public Task<User?> FindUserAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Users.FirstOrDefault(user => user.Id == id));
    }

    public Task<IReadOnlyList<Customer>> ListActiveCustomersAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Customer>>(
            Customers.Where(customer => customer.Status == CustomerStatus.Active).ToArray());
    }

    public Task<IReadOnlyList<Customer>> ListActiveCustomersForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Customer>>(
            CustomerMemberships
                .Where(membership => membership.UserId == userId
                    && membership.IsActive
                    && membership.Customer.Status == CustomerStatus.Active)
                .Select(membership => membership.Customer)
                .ToArray());
    }

    public Task<Customer?> FindCustomerAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Customers.FirstOrDefault(customer => customer.Id == id));
    }

    public Task<IReadOnlyList<CustomerMembership>> ListCustomerMembershipsAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<CustomerMembership>>(
            CustomerMemberships.Where(membership => membership.CustomerId == customerId).ToArray());
    }

    public Task<CustomerMembership?> FindCustomerMembershipAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(CustomerMemberships.FirstOrDefault(membership => membership.Id == id));
    }

    public Task<CustomerMembership?> FindCustomerMembershipForUserAsync(
        Guid customerId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(CustomerMemberships.FirstOrDefault(membership =>
            membership.CustomerId == customerId && membership.UserId == userId));
    }

    public Task<IReadOnlyList<ControlNode>> ListActiveControlNodesAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<ControlNode>>(
            ControlNodes
                .Where(controlNode => controlNode.CustomerId == customerId
                    && controlNode.Status == ControlNodeStatus.Active)
                .ToArray());
    }

    public Task<ControlNode?> FindControlNodeAsync(
        Guid customerId,
        Guid controlNodeId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ControlNodes.FirstOrDefault(controlNode =>
            controlNode.CustomerId == customerId && controlNode.Id == controlNodeId));
    }

    public Task<ControlNode?> FindControlNodeByNameAsync(
        Guid customerId,
        string name,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ControlNodes.FirstOrDefault(controlNode =>
            controlNode.CustomerId == customerId && controlNode.Name == name));
    }

    public Task<IReadOnlyList<ManagedNode>> ListActiveManagedNodesAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<ManagedNode>>(
            ManagedNodes
                .Where(managedNode => managedNode.CustomerId == customerId
                    && managedNode.Status == ManagedNodeStatus.Active)
                .ToArray());
    }

    public Task<ManagedNode?> FindManagedNodeAsync(
        Guid customerId,
        Guid managedNodeId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ManagedNodes.FirstOrDefault(managedNode =>
            managedNode.CustomerId == customerId && managedNode.Id == managedNodeId));
    }

    public Task<ManagedNode?> FindManagedNodeByNameAsync(
        Guid customerId,
        string name,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ManagedNodes.FirstOrDefault(managedNode =>
            managedNode.CustomerId == customerId && managedNode.Name == name));
    }

    public Task<IReadOnlyList<InventoryGroup>> ListActiveInventoryGroupsAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<InventoryGroup>>(
            InventoryGroups
                .Where(group => group.CustomerId == customerId && !group.IsArchived)
                .ToArray());
    }

    public Task<InventoryGroup?> FindInventoryGroupAsync(
        Guid customerId,
        Guid inventoryGroupId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(InventoryGroups.FirstOrDefault(group =>
            group.CustomerId == customerId && group.Id == inventoryGroupId));
    }

    public Task<InventoryGroup?> FindInventoryGroupByNameAsync(
        Guid customerId,
        string name,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(InventoryGroups.FirstOrDefault(group =>
            group.CustomerId == customerId && group.Name == name));
    }

    public Task<InventoryGroupNode?> FindInventoryGroupNodeAsync(
        Guid inventoryGroupId,
        Guid managedNodeId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(InventoryGroupNodes.FirstOrDefault(link =>
            link.InventoryGroupId == inventoryGroupId && link.ManagedNodeId == managedNodeId));
    }

    public Task<IReadOnlyList<ManagedNode>> ListActiveManagedNodesForInventoryGroupAsync(
        Guid inventoryGroupId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<ManagedNode>>(
            InventoryGroupNodes
                .Where(link => link.InventoryGroupId == inventoryGroupId)
                .Join(
                    ManagedNodes,
                    link => link.ManagedNodeId,
                    managedNode => managedNode.Id,
                    (_, managedNode) => managedNode)
                .Where(managedNode => managedNode.Status == ManagedNodeStatus.Active)
                .ToArray());
    }

    public void AddUser(User user)
    {
        Users.Add(user);
    }

    public void AddExternalIdentity(ExternalIdentity externalIdentity)
    {
        ExternalIdentities.Add(externalIdentity);
    }

    public void AddCustomer(Customer customer)
    {
        Customers.Add(customer);
    }

    public void AddCustomerMembership(CustomerMembership customerMembership)
    {
        CustomerMemberships.Add(customerMembership);
    }

    public void AddControlNode(ControlNode controlNode)
    {
        ControlNodes.Add(controlNode);
    }

    public void AddManagedNode(ManagedNode managedNode)
    {
        ManagedNodes.Add(managedNode);
    }

    public void AddInventoryGroup(InventoryGroup inventoryGroup)
    {
        InventoryGroups.Add(inventoryGroup);
    }

    public void AddInventoryGroupNode(InventoryGroupNode inventoryGroupNode)
    {
        InventoryGroupNodes.Add(inventoryGroupNode);
    }

    public void RemoveInventoryGroupNode(InventoryGroupNode inventoryGroupNode)
    {
        InventoryGroupNodes.Remove(inventoryGroupNode);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(1);
    }
}
