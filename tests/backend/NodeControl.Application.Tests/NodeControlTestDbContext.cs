using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.Users;
using NodeControl.Domain.VariableSets;

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

    public List<Playbook> Playbooks { get; } = [];

    public List<VariableSet> VariableSets { get; } = [];

    public List<Job> Jobs { get; } = [];

    public List<JobRun> JobRuns { get; } = [];

    public List<JobRunStatus[]> SavedJobRunStatuses { get; } = [];

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

    public Task<IReadOnlyList<Playbook>> ListActivePlaybooksAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Playbook>>(
            Playbooks
                .Where(playbook => playbook.CustomerId == customerId && playbook.Status == PlaybookStatus.Active)
                .ToArray());
    }

    public Task<Playbook?> FindPlaybookAsync(Guid customerId, Guid playbookId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Playbooks.FirstOrDefault(playbook =>
            playbook.CustomerId == customerId && playbook.Id == playbookId));
    }

    public Task<Playbook?> FindPlaybookBySlugAsync(Guid customerId, string slug, CancellationToken cancellationToken)
    {
        return Task.FromResult(Playbooks.FirstOrDefault(playbook =>
            playbook.CustomerId == customerId && playbook.Slug == slug));
    }

    public Task<IReadOnlyList<VariableSet>> ListActiveVariableSetsAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<VariableSet>>(
            VariableSets
                .Where(variableSet => variableSet.CustomerId == customerId && variableSet.Status == VariableSetStatus.Active)
                .ToArray());
    }

    public Task<VariableSet?> FindVariableSetAsync(Guid customerId, Guid variableSetId, CancellationToken cancellationToken)
    {
        return Task.FromResult(VariableSets.FirstOrDefault(variableSet =>
            variableSet.CustomerId == customerId && variableSet.Id == variableSetId));
    }

    public Task<VariableSet?> FindVariableSetBySlugAsync(Guid customerId, string slug, CancellationToken cancellationToken)
    {
        return Task.FromResult(VariableSets.FirstOrDefault(variableSet =>
            variableSet.CustomerId == customerId && variableSet.Slug == slug));
    }

    public Task<IReadOnlyList<Job>> ListActiveJobsAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Job>>(
            Jobs
                .Where(job => job.CustomerId == customerId && job.Status == JobStatus.Active)
                .ToArray());
    }

    public Task<Job?> FindJobAsync(Guid customerId, Guid jobId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Jobs.FirstOrDefault(job =>
            job.CustomerId == customerId && job.Id == jobId));
    }

    public Task<Job?> FindJobBySlugAsync(Guid customerId, string slug, CancellationToken cancellationToken)
    {
        return Task.FromResult(Jobs.FirstOrDefault(job =>
            job.CustomerId == customerId && job.Slug == slug));
    }

    public Task<IReadOnlyList<JobRun>> ListJobRunsAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<JobRun>>(
            JobRuns
                .Where(jobRun => jobRun.CustomerId == customerId)
                .OrderByDescending(jobRun => jobRun.CreatedAt)
                .ToArray());
    }

    public Task<JobRun?> FindJobRunAsync(Guid customerId, Guid jobRunId, CancellationToken cancellationToken)
    {
        return Task.FromResult(JobRuns.FirstOrDefault(jobRun =>
            jobRun.CustomerId == customerId && jobRun.Id == jobRunId));
    }

    public Task<JobRun?> FindOldestQueuedJobRunAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(JobRuns
            .Where(jobRun => jobRun.Status == JobRunStatus.Queued)
            .OrderBy(jobRun => jobRun.QueuedAt)
            .ThenBy(jobRun => jobRun.CreatedAt)
            .FirstOrDefault());
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

    public void AddPlaybook(Playbook playbook)
    {
        Playbooks.Add(playbook);
    }

    public void AddVariableSet(VariableSet variableSet)
    {
        VariableSets.Add(variableSet);
    }

    public void AddJob(Job job)
    {
        Jobs.Add(job);
    }

    public void AddJobRun(JobRun jobRun)
    {
        JobRuns.Add(jobRun);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SavedJobRunStatuses.Add(JobRuns.Select(jobRun => jobRun.Status).ToArray());
        return Task.FromResult(1);
    }
}
