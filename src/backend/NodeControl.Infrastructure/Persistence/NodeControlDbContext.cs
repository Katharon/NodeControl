using Microsoft.EntityFrameworkCore;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.Users;
using NodeControl.Domain.VariableSets;

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

    public DbSet<Playbook> Playbooks => Set<Playbook>();

    public DbSet<VariableSet> VariableSets => Set<VariableSet>();

    public DbSet<Job> Jobs => Set<Job>();

    public DbSet<JobSchedule> JobSchedules => Set<JobSchedule>();

    public DbSet<JobRun> JobRuns => Set<JobRun>();

    public DbSet<JobRunLogEntry> JobRunLogEntries => Set<JobRunLogEntry>();

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

    public async Task<IReadOnlyList<Playbook>> ListActivePlaybooksAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return await Playbooks
            .Where(playbook => playbook.CustomerId == customerId && playbook.Status == PlaybookStatus.Active)
            .OrderBy(playbook => playbook.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Playbook?> FindPlaybookAsync(
        Guid customerId,
        Guid playbookId,
        CancellationToken cancellationToken)
    {
        return await Playbooks.FirstOrDefaultAsync(
            playbook => playbook.CustomerId == customerId && playbook.Id == playbookId,
            cancellationToken);
    }

    public async Task<Playbook?> FindPlaybookBySlugAsync(
        Guid customerId,
        string slug,
        CancellationToken cancellationToken)
    {
        return await Playbooks.FirstOrDefaultAsync(
            playbook => playbook.CustomerId == customerId && playbook.Slug == slug,
            cancellationToken);
    }

    public async Task<IReadOnlyList<VariableSet>> ListActiveVariableSetsAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return await VariableSets
            .Where(variableSet => variableSet.CustomerId == customerId && variableSet.Status == VariableSetStatus.Active)
            .OrderBy(variableSet => variableSet.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<VariableSet?> FindVariableSetAsync(
        Guid customerId,
        Guid variableSetId,
        CancellationToken cancellationToken)
    {
        return await VariableSets.FirstOrDefaultAsync(
            variableSet => variableSet.CustomerId == customerId && variableSet.Id == variableSetId,
            cancellationToken);
    }

    public async Task<VariableSet?> FindVariableSetBySlugAsync(
        Guid customerId,
        string slug,
        CancellationToken cancellationToken)
    {
        return await VariableSets.FirstOrDefaultAsync(
            variableSet => variableSet.CustomerId == customerId && variableSet.Slug == slug,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Job>> ListActiveJobsAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return await Jobs
            .Where(job => job.CustomerId == customerId && job.Status == JobStatus.Active)
            .OrderBy(job => job.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Job?> FindJobAsync(
        Guid customerId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        return await Jobs.FirstOrDefaultAsync(
            job => job.CustomerId == customerId && job.Id == jobId,
            cancellationToken);
    }

    public async Task<Job?> FindJobBySlugAsync(
        Guid customerId,
        string slug,
        CancellationToken cancellationToken)
    {
        return await Jobs.FirstOrDefaultAsync(
            job => job.CustomerId == customerId && job.Slug == slug,
            cancellationToken);
    }

    public async Task<IReadOnlyList<JobSchedule>> ListJobSchedulesAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return await JobSchedules
            .Where(schedule => schedule.CustomerId == customerId
                && schedule.Status != JobScheduleStatus.Archived)
            .OrderBy(schedule => schedule.Name)
            .ThenBy(schedule => schedule.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<JobSchedule?> FindJobScheduleAsync(
        Guid customerId,
        Guid jobScheduleId,
        CancellationToken cancellationToken)
    {
        return await JobSchedules.FirstOrDefaultAsync(
            schedule => schedule.CustomerId == customerId && schedule.Id == jobScheduleId,
            cancellationToken);
    }

    public async Task<JobSchedule?> FindJobScheduleBySlugAsync(
        Guid customerId,
        string slug,
        CancellationToken cancellationToken)
    {
        return await JobSchedules.FirstOrDefaultAsync(
            schedule => schedule.CustomerId == customerId
                && schedule.Slug == slug,
            cancellationToken);
    }

    public async Task<IReadOnlyList<JobSchedule>> ListDueActiveJobSchedulesAsync(
        DateTimeOffset nowUtc,
        int limit,
        CancellationToken cancellationToken)
    {
        return await JobSchedules
            .Where(schedule => schedule.Status == JobScheduleStatus.Active
                && schedule.NextRunAtUtc != null
                && schedule.NextRunAtUtc <= nowUtc)
            .OrderBy(schedule => schedule.NextRunAtUtc)
            .ThenBy(schedule => schedule.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JobRun>> ListJobRunsAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return await JobRuns
            .Where(jobRun => jobRun.CustomerId == customerId)
            .OrderByDescending(jobRun => jobRun.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<JobRun?> FindJobRunAsync(
        Guid customerId,
        Guid jobRunId,
        CancellationToken cancellationToken)
    {
        return await JobRuns.FirstOrDefaultAsync(
            jobRun => jobRun.CustomerId == customerId && jobRun.Id == jobRunId,
            cancellationToken);
    }

    public async Task<JobRun?> FindOldestQueuedJobRunAsync(CancellationToken cancellationToken)
    {
        return await JobRuns
            .Where(jobRun => jobRun.Status == JobRunStatus.Queued)
            .OrderBy(jobRun => jobRun.QueuedAt)
            .ThenBy(jobRun => jobRun.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<JobRunStatus?> GetJobRunStatusAsync(Guid jobRunId, CancellationToken cancellationToken)
    {
        return await JobRuns
            .AsNoTracking()
            .Where(jobRun => jobRun.Id == jobRunId)
            .Select(jobRun => (JobRunStatus?)jobRun.Status)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsJobRunCancellationRequestedAsync(Guid jobRunId, CancellationToken cancellationToken)
    {
        return await JobRuns
            .AsNoTracking()
            .AnyAsync(
                jobRun => jobRun.Id == jobRunId
                    && (jobRun.Status == JobRunStatus.Cancelling
                        || (jobRun.CancellationRequestedAtUtc != null
                            && (jobRun.Status == JobRunStatus.Running || jobRun.Status == JobRunStatus.Cancelling))),
                cancellationToken);
    }

    public async Task<long> GetNextJobRunLogSequenceAsync(Guid jobRunId, CancellationToken cancellationToken)
    {
        var currentMax = await JobRunLogEntries
            .Where(entry => entry.JobRunId == jobRunId)
            .Select(entry => (long?)entry.Sequence)
            .MaxAsync(cancellationToken);
        return (currentMax ?? 0) + 1;
    }

    public async Task<IReadOnlyList<JobRunLogEntry>> ListJobRunLogEntriesAsync(
        Guid jobRunId,
        long? afterSequence,
        int limit,
        CancellationToken cancellationToken)
    {
        return await JobRunLogEntries
            .Where(entry => entry.JobRunId == jobRunId
                && (afterSequence == null || entry.Sequence > afterSequence))
            .OrderBy(entry => entry.Sequence)
            .Take(limit)
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

    public void AddJobSchedule(JobSchedule jobSchedule)
    {
        JobSchedules.Add(jobSchedule);
    }

    public void AddJobRun(JobRun jobRun)
    {
        JobRuns.Add(jobRun);
    }

    public void AddJobRunLogEntry(JobRunLogEntry jobRunLogEntry)
    {
        JobRunLogEntries.Add(jobRunLogEntry);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NodeControlDbContext).Assembly);
    }
}
