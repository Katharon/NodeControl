using NodeControl.Domain.Users;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Application.Abstractions.Persistence;

public interface INodeControlDbContext
{
    Task<ExternalIdentity?> FindExternalIdentityAsync(
        string provider,
        string subject,
        CancellationToken cancellationToken);

    Task<User?> FindUserAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Customer>> ListActiveCustomersAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Customer>> ListActiveCustomersForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<Customer?> FindCustomerAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CustomerMembership>> ListCustomerMembershipsAsync(
        Guid customerId,
        CancellationToken cancellationToken);

    Task<CustomerMembership?> FindCustomerMembershipAsync(Guid id, CancellationToken cancellationToken);

    Task<CustomerMembership?> FindCustomerMembershipForUserAsync(
        Guid customerId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ControlNode>> ListActiveControlNodesAsync(Guid customerId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<ControlNode?> FindControlNodeAsync(Guid customerId, Guid controlNodeId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<ControlNode?> FindControlNodeByNameAsync(Guid customerId, string name, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<IReadOnlyList<ManagedNode>> ListActiveManagedNodesAsync(Guid customerId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<ManagedNode?> FindManagedNodeAsync(Guid customerId, Guid managedNodeId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<ManagedNode?> FindManagedNodeByNameAsync(Guid customerId, string name, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<IReadOnlyList<InventoryGroup>> ListActiveInventoryGroupsAsync(Guid customerId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<InventoryGroup?> FindInventoryGroupAsync(Guid customerId, Guid inventoryGroupId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<InventoryGroup?> FindInventoryGroupByNameAsync(Guid customerId, string name, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<InventoryGroupNode?> FindInventoryGroupNodeAsync(
        Guid inventoryGroupId,
        Guid managedNodeId,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<IReadOnlyList<ManagedNode>> ListActiveManagedNodesForInventoryGroupAsync(
        Guid inventoryGroupId,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<IReadOnlyList<Playbook>> ListActivePlaybooksAsync(Guid customerId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<Playbook?> FindPlaybookAsync(Guid customerId, Guid playbookId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<Playbook?> FindPlaybookBySlugAsync(Guid customerId, string slug, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<IReadOnlyList<VariableSet>> ListActiveVariableSetsAsync(Guid customerId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<VariableSet?> FindVariableSetAsync(Guid customerId, Guid variableSetId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<VariableSet?> FindVariableSetBySlugAsync(Guid customerId, string slug, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    void AddUser(User user);

    void AddExternalIdentity(ExternalIdentity externalIdentity);

    void AddCustomer(Customer customer);

    void AddCustomerMembership(CustomerMembership customerMembership);

    void AddControlNode(ControlNode controlNode)
    {
        throw new NotSupportedException();
    }

    void AddManagedNode(ManagedNode managedNode)
    {
        throw new NotSupportedException();
    }

    void AddInventoryGroup(InventoryGroup inventoryGroup)
    {
        throw new NotSupportedException();
    }

    void AddInventoryGroupNode(InventoryGroupNode inventoryGroupNode)
    {
        throw new NotSupportedException();
    }

    void RemoveInventoryGroupNode(InventoryGroupNode inventoryGroupNode)
    {
        throw new NotSupportedException();
    }

    void AddPlaybook(Playbook playbook)
    {
        throw new NotSupportedException();
    }

    void AddVariableSet(VariableSet variableSet)
    {
        throw new NotSupportedException();
    }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
