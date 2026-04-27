using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Application.Validation;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Application.VariableSets;

public sealed class VariableSetService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService,
    YamlJsonValidationService validationService,
    IClock clock)
{
    public async Task<CustomerServiceResult<IReadOnlyList<VariableSetDto>>> ListAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewPlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<IReadOnlyList<VariableSetDto>>.FromAuthorization(authorization);
        }

        var variableSets = await dbContext.ListActiveVariableSetsAsync(customerId, cancellationToken);
        return CustomerServiceResult<IReadOnlyList<VariableSetDto>>.Ok(variableSets.Select(Map).ToArray());
    }

    public async Task<CustomerServiceResult<VariableSetDto>> GetAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid variableSetId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewPlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<VariableSetDto>.FromAuthorization(authorization);
        }

        var variableSet = await dbContext.FindVariableSetAsync(customerId, variableSetId, cancellationToken);
        return variableSet is null
            ? CustomerServiceResult<VariableSetDto>.NotFound()
            : CustomerServiceResult<VariableSetDto>.Ok(Map(variableSet));
    }

    public async Task<CustomerServiceResult<VariableSetDto>> CreateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CreateVariableSetRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManagePlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<VariableSetDto>.FromAuthorization(authorization);
        }

        if (!ValidateContent(request.Format, request.Content).IsValid)
        {
            return CustomerServiceResult<VariableSetDto>.BadRequest();
        }

        if (await dbContext.FindVariableSetBySlugAsync(customerId, request.Slug.Trim(), cancellationToken) is not null)
        {
            return CustomerServiceResult<VariableSetDto>.Conflict();
        }

        try
        {
            var variableSet = VariableSet.Create(
                customerId,
                request.Name,
                request.Slug,
                request.Description,
                request.Format,
                request.Content,
                request.ContainsSensitiveValues,
                clock.UtcNow);

            dbContext.AddVariableSet(variableSet);
            await dbContext.SaveChangesAsync(cancellationToken);

            return CustomerServiceResult<VariableSetDto>.Ok(Map(variableSet));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<VariableSetDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<VariableSetDto>> UpdateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid variableSetId,
        UpdateVariableSetRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManagePlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<VariableSetDto>.FromAuthorization(authorization);
        }

        if (!ValidateContent(request.Format, request.Content).IsValid)
        {
            return CustomerServiceResult<VariableSetDto>.BadRequest();
        }

        var variableSet = await dbContext.FindVariableSetAsync(customerId, variableSetId, cancellationToken);
        if (variableSet is null)
        {
            return CustomerServiceResult<VariableSetDto>.NotFound();
        }

        var existing = await dbContext.FindVariableSetBySlugAsync(customerId, request.Slug.Trim(), cancellationToken);
        if (existing is not null && existing.Id != variableSetId)
        {
            return CustomerServiceResult<VariableSetDto>.Conflict();
        }

        try
        {
            variableSet.Update(
                request.Name,
                request.Slug,
                request.Description,
                request.Format,
                request.Content,
                request.ContainsSensitiveValues,
                clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);

            return CustomerServiceResult<VariableSetDto>.Ok(Map(variableSet));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<VariableSetDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<VariableSetDto>> ArchiveAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid variableSetId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManagePlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<VariableSetDto>.FromAuthorization(authorization);
        }

        var variableSet = await dbContext.FindVariableSetAsync(customerId, variableSetId, cancellationToken);
        if (variableSet is null)
        {
            return CustomerServiceResult<VariableSetDto>.NotFound();
        }

        variableSet.Archive(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CustomerServiceResult<VariableSetDto>.Ok(Map(variableSet));
    }

    public async Task<CustomerServiceResult<VariableSetValidationResultDto>> ValidateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid variableSetId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewPlaybooks, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<VariableSetValidationResultDto>.FromAuthorization(authorization);
        }

        var variableSet = await dbContext.FindVariableSetAsync(customerId, variableSetId, cancellationToken);
        if (variableSet is null)
        {
            return CustomerServiceResult<VariableSetValidationResultDto>.NotFound();
        }

        var result = ValidateContent(variableSet.Format, variableSet.Content);
        return CustomerServiceResult<VariableSetValidationResultDto>.Ok(
            new VariableSetValidationResultDto(result.IsValid, result.Message, result.Errors));
    }

    private ValidationResult ValidateContent(VariableSetFormat format, string content)
    {
        return format switch
        {
            VariableSetFormat.Yaml => validationService.ValidateYaml(content),
            VariableSetFormat.Json => validationService.ValidateJson(content),
            _ => ValidationResult.Invalid("Unsupported variable set format.", ["Unsupported variable set format."])
        };
    }

    private async Task<CustomerAuthorizationResult> AuthorizeAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        return await authorizationService.AuthorizeAsync(currentUser, customerId, permission, cancellationToken);
    }

    private static VariableSetDto Map(VariableSet variableSet)
    {
        return new VariableSetDto(
            variableSet.Id,
            variableSet.CustomerId,
            variableSet.Name,
            variableSet.Slug,
            variableSet.Description,
            variableSet.Format,
            variableSet.Content,
            variableSet.ContainsSensitiveValues,
            variableSet.Status,
            variableSet.CreatedAt,
            variableSet.UpdatedAt,
            variableSet.ArchivedAt);
    }
}
