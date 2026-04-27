using NodeControl.Application.Audit;
using NodeControl.Domain.Audit;
using NodeControl.Domain.Users;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Inventories;
using NodeControl.Domain.Jobs;
using NodeControl.Domain.Nodes;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.Secrets;
using NodeControl.Domain.Templates;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Application.Abstractions.Persistence;

public interface INodeControlDbContext
{
    Task<ExternalIdentity?> FindExternalIdentityAsync(
        string provider,
        string subject,
        CancellationToken cancellationToken);

    Task<User?> FindUserAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> SearchUsersAsync(string? query, int limit, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

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

    Task<IReadOnlyList<Template>> ListActiveTemplatesAsync(Guid customerId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<Template?> FindTemplateAsync(Guid customerId, Guid templateId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<Template?> FindTemplateBySlugAsync(Guid customerId, string slug, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<IReadOnlyList<Secret>> ListActiveSecretsAsync(Guid customerId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<Secret?> FindSecretAsync(Guid customerId, Guid secretId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<Secret?> FindSecretBySlugAsync(Guid customerId, string slug, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<IReadOnlyList<Job>> ListActiveJobsAsync(Guid customerId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<Job?> FindJobAsync(Guid customerId, Guid jobId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<Job?> FindJobBySlugAsync(Guid customerId, string slug, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<IReadOnlyList<JobSchedule>> ListJobSchedulesAsync(Guid customerId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<JobSchedule?> FindJobScheduleAsync(Guid customerId, Guid jobScheduleId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<JobSchedule?> FindJobScheduleBySlugAsync(Guid customerId, string slug, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<IReadOnlyList<JobSchedule>> ListDueActiveJobSchedulesAsync(
        DateTimeOffset nowUtc,
        int limit,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<IReadOnlyList<JobRun>> ListJobRunsAsync(Guid customerId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<JobRun?> FindJobRunAsync(Guid customerId, Guid jobRunId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<JobRun?> FindOldestQueuedJobRunAsync(CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<JobRunStatus?> GetJobRunStatusAsync(Guid jobRunId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<bool> IsJobRunCancellationRequestedAsync(Guid jobRunId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<long> GetNextJobRunLogSequenceAsync(Guid jobRunId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<IReadOnlyList<JobRunLogEntry>> ListJobRunLogEntriesAsync(
        Guid jobRunId,
        long? afterSequence,
        int limit,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<IReadOnlyList<AuditLogEntry>> ListAuditLogEntriesAsync(
        Guid customerId,
        AuditLogQuery query,
        int limit,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task<AuditLogEntry?> FindAuditLogEntryAsync(
        Guid customerId,
        Guid auditLogEntryId,
        CancellationToken cancellationToken)
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

    void AddTemplate(Template template)
    {
        throw new NotSupportedException();
    }

    void AddSecret(Secret secret)
    {
        throw new NotSupportedException();
    }

    void AddJob(Job job)
    {
        throw new NotSupportedException();
    }

    void AddJobSchedule(JobSchedule jobSchedule)
    {
        throw new NotSupportedException();
    }

    void AddJobRun(JobRun jobRun)
    {
        throw new NotSupportedException();
    }

    void AddJobRunLogEntry(JobRunLogEntry jobRunLogEntry)
    {
        throw new NotSupportedException();
    }

    void AddAuditLogEntry(AuditLogEntry auditLogEntry)
    {
        throw new NotSupportedException();
    }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
