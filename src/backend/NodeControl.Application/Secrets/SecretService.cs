using System.Text.Json;
using NodeControl.Application.Audit;
using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Security;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Domain.Audit;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.Secrets;

namespace NodeControl.Application.Secrets;

public sealed class SecretService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService,
    ISecretProtector secretProtector,
    IClock clock,
    IAuditLogWriter auditLogWriter)
{
    private const int MaxSecretValueLength = 100000;

    public async Task<CustomerServiceResult<IReadOnlyList<SecretDto>>> ListAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewSecrets, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<IReadOnlyList<SecretDto>>.FromAuthorization(authorization);
        }

        var secrets = await dbContext.ListActiveSecretsAsync(customerId, cancellationToken);
        return CustomerServiceResult<IReadOnlyList<SecretDto>>.Ok(secrets.Select(Map).ToArray());
    }

    public async Task<CustomerServiceResult<SecretDto>> GetAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid secretId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewSecrets, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<SecretDto>.FromAuthorization(authorization);
        }

        var secret = await dbContext.FindSecretAsync(customerId, secretId, cancellationToken);
        return secret is null
            ? CustomerServiceResult<SecretDto>.NotFound()
            : CustomerServiceResult<SecretDto>.Ok(Map(secret));
    }

    public async Task<CustomerServiceResult<SecretDto>> CreateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CreateSecretRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageSecrets, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<SecretDto>.FromAuthorization(authorization);
        }

        if (!TryParseSecretKind(request.Kind, out var kind) || !IsValidSecretValue(request.Value))
        {
            return CustomerServiceResult<SecretDto>.BadRequest();
        }

        if (await dbContext.FindSecretBySlugAsync(customerId, request.Slug.Trim(), cancellationToken) is not null)
        {
            return CustomerServiceResult<SecretDto>.Conflict();
        }

        try
        {
            var secret = Secret.Create(
                customerId,
                request.Name,
                request.Slug,
                request.Description,
                kind,
                secretProtector.Protect(request.Value),
                clock.UtcNow);

            dbContext.AddSecret(secret);
            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteSecretAuditAsync(currentUser, secret, "secret.created", $"Secret '{secret.Name}' was created.", cancellationToken);

            return CustomerServiceResult<SecretDto>.Ok(Map(secret));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<SecretDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<SecretDto>> UpdateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid secretId,
        UpdateSecretRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageSecrets, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<SecretDto>.FromAuthorization(authorization);
        }

        if (!TryParseSecretKind(request.Kind, out var kind))
        {
            return CustomerServiceResult<SecretDto>.BadRequest();
        }

        var secret = await dbContext.FindSecretAsync(customerId, secretId, cancellationToken);
        if (secret is null)
        {
            return CustomerServiceResult<SecretDto>.NotFound();
        }

        if (secret.Status == SecretStatus.Archived)
        {
            return CustomerServiceResult<SecretDto>.BadRequest();
        }

        var existing = await dbContext.FindSecretBySlugAsync(customerId, request.Slug.Trim(), cancellationToken);
        if (existing is not null && existing.Id != secretId)
        {
            return CustomerServiceResult<SecretDto>.Conflict();
        }

        try
        {
            secret.UpdateMetadata(
                request.Name,
                request.Slug,
                request.Description,
                kind,
                clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteSecretAuditAsync(currentUser, secret, "secret.updated", $"Secret '{secret.Name}' was updated.", cancellationToken);

            return CustomerServiceResult<SecretDto>.Ok(Map(secret));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<SecretDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<SecretDto>> RotateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid secretId,
        RotateSecretRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageSecrets, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<SecretDto>.FromAuthorization(authorization);
        }

        if (!IsValidSecretValue(request.Value))
        {
            return CustomerServiceResult<SecretDto>.BadRequest();
        }

        var secret = await dbContext.FindSecretAsync(customerId, secretId, cancellationToken);
        if (secret is null)
        {
            return CustomerServiceResult<SecretDto>.NotFound();
        }

        if (secret.Status == SecretStatus.Archived)
        {
            return CustomerServiceResult<SecretDto>.BadRequest();
        }

        try
        {
            secret.Rotate(secretProtector.Protect(request.Value), clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteSecretAuditAsync(currentUser, secret, "secret.rotated", $"Secret '{secret.Name}' was rotated.", cancellationToken);

            return CustomerServiceResult<SecretDto>.Ok(Map(secret));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<SecretDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<SecretDto>> ArchiveAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid secretId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageSecrets, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<SecretDto>.FromAuthorization(authorization);
        }

        var secret = await dbContext.FindSecretAsync(customerId, secretId, cancellationToken);
        if (secret is null)
        {
            return CustomerServiceResult<SecretDto>.NotFound();
        }

        secret.Archive(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        await WriteSecretAuditAsync(currentUser, secret, "secret.archived", $"Secret '{secret.Name}' was archived.", cancellationToken);

        return CustomerServiceResult<SecretDto>.Ok(Map(secret));
    }

    private async Task WriteSecretAuditAsync(
        CurrentUserDto currentUser,
        Secret secret,
        string action,
        string message,
        CancellationToken cancellationToken)
    {
        await auditLogWriter.WriteAsync(new AuditLogWriteRequest(
            secret.CustomerId,
            currentUser.Id,
            currentUser.DisplayName,
            AuditActorType.User,
            action,
            "Secret",
            secret.Id,
            secret.Name,
            AuditOutcome.Succeeded,
            message,
            JsonSerializer.Serialize(new
            {
                secretId = secret.Id,
                slug = secret.Slug,
                kind = secret.Kind.ToString(),
                status = secret.Status.ToString(),
                hasValue = !string.IsNullOrWhiteSpace(secret.ProtectedValue)
            })),
            cancellationToken);
    }

    private async Task<CustomerAuthorizationResult> AuthorizeAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        return await authorizationService.AuthorizeAsync(currentUser, customerId, permission, cancellationToken);
    }

    private static bool TryParseSecretKind(string kind, out SecretKind parsed)
    {
        return Enum.TryParse(kind, ignoreCase: true, out parsed)
            && Enum.IsDefined(parsed);
    }

    private static bool IsValidSecretValue(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Length <= MaxSecretValueLength;
    }

    private static SecretDto Map(Secret secret)
    {
        return new SecretDto(
            secret.Id,
            secret.CustomerId,
            secret.Name,
            secret.Slug,
            secret.Description,
            secret.Kind.ToString(),
            secret.Status.ToString(),
            !string.IsNullOrWhiteSpace(secret.ProtectedValue),
            secret.LastRotatedAtUtc,
            secret.CreatedAt,
            secret.UpdatedAt,
            secret.ArchivedAt);
    }
}
