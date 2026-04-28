using System.Text.Json;
using NodeControl.Application.Audit;
using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Domain.Audit;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.Nodes;

namespace NodeControl.Application.HostConnectionChecks;

public sealed class HostConnectionCheckService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService,
    IClock clock,
    IAuditLogWriter auditLogWriter)
{
    private const int DefaultListLimit = 100;
    private const int MaxListLimit = 200;
    private const int LatestSummaryLimit = 500;

    public async Task<CustomerServiceResult<IReadOnlyList<HostConnectionCheckDto>>> ListAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        HostConnectionTargetType? targetType = null,
        Guid? controlNodeId = null,
        Guid? managedNodeId = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewNodes, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<IReadOnlyList<HostConnectionCheckDto>>.FromAuthorization(authorization);
        }

        var checks = await dbContext.ListHostConnectionChecksAsync(
            customerId,
            targetType,
            controlNodeId,
            managedNodeId,
            Math.Clamp(limit ?? DefaultListLimit, 1, MaxListLimit),
            cancellationToken);

        return CustomerServiceResult<IReadOnlyList<HostConnectionCheckDto>>.Ok(checks.Select(Map).ToArray());
    }

    public async Task<CustomerServiceResult<HostConnectionCheckDto>> GetAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid checkId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewNodes, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<HostConnectionCheckDto>.FromAuthorization(authorization);
        }

        var check = await dbContext.FindHostConnectionCheckAsync(customerId, checkId, cancellationToken);
        return check is null
            ? CustomerServiceResult<HostConnectionCheckDto>.NotFound()
            : CustomerServiceResult<HostConnectionCheckDto>.Ok(Map(check));
    }

    public async Task<CustomerServiceResult<HostHealthSummaryDto>> GetHostHealthSummaryAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewNodes, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<HostHealthSummaryDto>.FromAuthorization(authorization);
        }

        var controlNodes = await dbContext.ListActiveControlNodesAsync(customerId, cancellationToken);
        var managedNodes = await dbContext.ListActiveManagedNodesAsync(customerId, cancellationToken);
        var latestChecks = await dbContext.ListLatestHostConnectionChecksAsync(
            customerId,
            LatestSummaryLimit,
            cancellationToken);
        var latestByTarget = latestChecks
            .GroupBy(check => new TargetKey(check.TargetType, check.ControlNodeId, check.ManagedNodeId))
            .ToDictionary(group => group.Key, group => Map(group.First()));

        var targets = controlNodes
            .Select(controlNode => new HostHealthTargetDto(
                HostConnectionTargetType.ControlNode,
                controlNode.Id,
                controlNode.Name,
                controlNode.Hostname,
                controlNode.SshPort,
                latestByTarget.GetValueOrDefault(new TargetKey(HostConnectionTargetType.ControlNode, controlNode.Id, null))))
            .Concat(managedNodes.Select(managedNode => new HostHealthTargetDto(
                HostConnectionTargetType.ManagedNode,
                managedNode.Id,
                managedNode.Name,
                managedNode.Hostname,
                managedNode.SshPort,
                latestByTarget.GetValueOrDefault(new TargetKey(HostConnectionTargetType.ManagedNode, null, managedNode.Id)))))
            .OrderBy(target => target.TargetType)
            .ThenBy(target => target.Name)
            .ToArray();

        return CustomerServiceResult<HostHealthSummaryDto>.Ok(new HostHealthSummaryDto(targets));
    }

    public async Task<CustomerServiceResult<HostConnectionCheckDto>> QueueControlNodeCheckAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid controlNodeId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageNodes, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<HostConnectionCheckDto>.FromAuthorization(authorization);
        }

        var controlNode = await dbContext.FindControlNodeAsync(customerId, controlNodeId, cancellationToken);
        if (controlNode is null)
        {
            return CustomerServiceResult<HostConnectionCheckDto>.NotFound();
        }

        try
        {
            var check = HostConnectionCheck.CreateForControlNode(controlNode, currentUser.Id, clock.UtcNow);
            dbContext.AddHostConnectionCheck(check);
            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteQueuedAuditAsync(currentUser, check, controlNode.Name, cancellationToken);
            return CustomerServiceResult<HostConnectionCheckDto>.Ok(Map(check));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<HostConnectionCheckDto>.BadRequest();
        }
        catch (InvalidOperationException)
        {
            return CustomerServiceResult<HostConnectionCheckDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<HostConnectionCheckDto>> QueueManagedNodeCheckAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid managedNodeId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageNodes, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<HostConnectionCheckDto>.FromAuthorization(authorization);
        }

        var managedNode = await dbContext.FindManagedNodeAsync(customerId, managedNodeId, cancellationToken);
        if (managedNode is null)
        {
            return CustomerServiceResult<HostConnectionCheckDto>.NotFound();
        }

        try
        {
            var check = HostConnectionCheck.CreateForManagedNode(managedNode, currentUser.Id, clock.UtcNow);
            dbContext.AddHostConnectionCheck(check);
            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteQueuedAuditAsync(currentUser, check, managedNode.Name, cancellationToken);
            return CustomerServiceResult<HostConnectionCheckDto>.Ok(Map(check));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<HostConnectionCheckDto>.BadRequest();
        }
        catch (InvalidOperationException)
        {
            return CustomerServiceResult<HostConnectionCheckDto>.BadRequest();
        }
    }

    private async Task<CustomerAuthorizationResult> AuthorizeAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        return await authorizationService.AuthorizeAsync(currentUser, customerId, permission, cancellationToken);
    }

    private async Task WriteQueuedAuditAsync(
        CurrentUserDto currentUser,
        HostConnectionCheck check,
        string targetName,
        CancellationToken cancellationToken)
    {
        await auditLogWriter.WriteAsync(new AuditLogWriteRequest(
            check.CustomerId,
            currentUser.Id,
            currentUser.DisplayName,
            AuditActorType.User,
            "host_connection_check.queued",
            "HostConnectionCheck",
            check.Id,
            $"{check.TargetType} {targetName}",
            AuditOutcome.Succeeded,
            $"Connection check queued for {check.TargetType} '{targetName}'.",
            JsonSerializer.Serialize(new
            {
                checkId = check.Id,
                check.TargetType,
                check.ControlNodeId,
                check.ManagedNodeId,
                check.Hostname,
                check.Port,
                check.Status
            })),
            cancellationToken);
    }

    internal static HostConnectionCheckDto Map(HostConnectionCheck check)
    {
        return new HostConnectionCheckDto(
            check.Id,
            check.CustomerId,
            check.TargetType,
            check.ControlNodeId,
            check.ManagedNodeId,
            check.Hostname,
            check.Port,
            check.Status,
            check.RequestedByUserId,
            check.QueuedAtUtc,
            check.StartedAtUtc,
            check.FinishedAtUtc,
            check.DurationMs,
            check.ResultMessage,
            check.ErrorMessage);
    }

    private sealed record TargetKey(
        HostConnectionTargetType TargetType,
        Guid? ControlNodeId,
        Guid? ManagedNodeId);
}
