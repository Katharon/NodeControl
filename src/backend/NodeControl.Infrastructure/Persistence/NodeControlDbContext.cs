using Microsoft.EntityFrameworkCore;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Users;

namespace NodeControl.Infrastructure.Persistence;

public sealed class NodeControlDbContext(DbContextOptions<NodeControlDbContext> options)
    : DbContext(options), INodeControlDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<ExternalIdentity> ExternalIdentities => Set<ExternalIdentity>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<CustomerMembership> CustomerMemberships => Set<CustomerMembership>();

    public DbSet<ControlNode> ControlNodes => Set<ControlNode>();

    public DbSet<ManagedNode> ManagedNodes => Set<ManagedNode>();

    public DbSet<InventoryGroup> InventoryGroups => Set<InventoryGroup>();

    public DbSet<InventoryGroupNode> InventoryGroupNodes => Set<InventoryGroupNode>();

    public async Task<ExternalIdentity?> FindExternalIdentityAsync(
        string provider,
        string subject,
        CancellationToken cancellationToken)
    {
        return await ExternalIdentities
            .Include(externalIdentity => externalIdentity.User)
            .FirstOrDefaultAsync(
                externalIdentity => externalIdentity.Provider == provider
                    && externalIdentity.Subject == subject,
                cancellationToken);
    }

    public async Task<User?> FindUserAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> ListActiveCustomersAsync(CancellationToken cancellationToken)
    {
        return await Customers
            .Where(customer => customer.Status == CustomerStatus.Active)
            .OrderBy(customer => customer.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> ListActiveCustomersForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await CustomerMemberships
            .Where(membership => membership.UserId == userId
                && membership.IsActive
                && membership.Customer.Status == CustomerStatus.Active)
            .Select(membership => membership.Customer)
            .OrderBy(customer => customer.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Customer?> FindCustomerAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Customers.FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerMembership>> ListCustomerMembershipsAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return await CustomerMemberships
            .Include(membership => membership.User)
            .Where(membership => membership.CustomerId == customerId)
            .OrderBy(membership => membership.User.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerMembership?> FindCustomerMembershipAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await CustomerMemberships
            .Include(membership => membership.Customer)
            .Include(membership => membership.User)
            .FirstOrDefaultAsync(membership => membership.Id == id, cancellationToken);
    }

    public async Task<CustomerMembership?> FindCustomerMembershipForUserAsync(
        Guid customerId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await CustomerMemberships
            .Include(membership => membership.Customer)
            .Include(membership => membership.User)
            .FirstOrDefaultAsync(
                membership => membership.CustomerId == customerId && membership.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ControlNode>> ListActiveControlNodesAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return await ControlNodes
            .Where(controlNode => controlNode.CustomerId == customerId
                && controlNode.Status == ControlNodeStatus.Active)
            .OrderBy(controlNode => controlNode.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ControlNode?> FindControlNodeAsync(
        Guid customerId,
        Guid controlNodeId,
        CancellationToken cancellationToken)
    {
        return await ControlNodes.FirstOrDefaultAsync(
            controlNode => controlNode.CustomerId == customerId && controlNode.Id == controlNodeId,
            cancellationToken);
    }

    public async Task<ControlNode?> FindControlNodeByNameAsync(
        Guid customerId,
        string name,
        CancellationToken cancellationToken)
    {
        return await ControlNodes.FirstOrDefaultAsync(
            controlNode => controlNode.CustomerId == customerId && controlNode.Name == name,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ManagedNode>> ListActiveManagedNodesAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return await ManagedNodes
            .Where(managedNode => managedNode.CustomerId == customerId
                && managedNode.Status == ManagedNodeStatus.Active)
            .OrderBy(managedNode => managedNode.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ManagedNode?> FindManagedNodeAsync(
        Guid customerId,
        Guid managedNodeId,
        CancellationToken cancellationToken)
    {
        return await ManagedNodes.FirstOrDefaultAsync(
            managedNode => managedNode.CustomerId == customerId && managedNode.Id == managedNodeId,
            cancellationToken);
    }

    public async Task<ManagedNode?> FindManagedNodeByNameAsync(
        Guid customerId,
        string name,
        CancellationToken cancellationToken)
    {
        return await ManagedNodes.FirstOrDefaultAsync(
            managedNode => managedNode.CustomerId == customerId && managedNode.Name == name,
            cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryGroup>> ListActiveInventoryGroupsAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return await InventoryGroups
            .Where(group => group.CustomerId == customerId && group.ArchivedAt == null)
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<InventoryGroup?> FindInventoryGroupAsync(
        Guid customerId,
        Guid inventoryGroupId,
        CancellationToken cancellationToken)
    {
        return await InventoryGroups.FirstOrDefaultAsync(
            group => group.CustomerId == customerId && group.Id == inventoryGroupId,
            cancellationToken);
    }

    public async Task<InventoryGroup?> FindInventoryGroupByNameAsync(
        Guid customerId,
        string name,
        CancellationToken cancellationToken)
    {
        return await InventoryGroups.FirstOrDefaultAsync(
            group => group.CustomerId == customerId && group.Name == name,
            cancellationToken);
    }

    public async Task<InventoryGroupNode?> FindInventoryGroupNodeAsync(
        Guid inventoryGroupId,
        Guid managedNodeId,
        CancellationToken cancellationToken)
    {
        return await InventoryGroupNodes.FirstOrDefaultAsync(
            link => link.InventoryGroupId == inventoryGroupId && link.ManagedNodeId == managedNodeId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ManagedNode>> ListActiveManagedNodesForInventoryGroupAsync(
        Guid inventoryGroupId,
        CancellationToken cancellationToken)
    {
        return await InventoryGroupNodes
            .Where(link => link.InventoryGroupId == inventoryGroupId)
            .Join(
                ManagedNodes,
                link => link.ManagedNodeId,
                managedNode => managedNode.Id,
                (_, managedNode) => managedNode)
            .Where(managedNode => managedNode.Status == ManagedNodeStatus.Active)
            .OrderBy(managedNode => managedNode.Name)
            .ToListAsync(cancellationToken);
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NodeControlDbContext).Assembly);
    }
}
