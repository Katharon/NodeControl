using System.Text.Json;
using NodeControl.Application.Audit;
using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Customers;
using NodeControl.Domain.Audit;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.Templates;

namespace NodeControl.Application.Templates;

public sealed class TemplateService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService,
    TemplateValidationService validationService,
    IClock clock,
    IAuditLogWriter auditLogWriter)
{
    public async Task<CustomerServiceResult<IReadOnlyList<TemplateDto>>> ListAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewTemplates, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<IReadOnlyList<TemplateDto>>.FromAuthorization(authorization);
        }

        var templates = await dbContext.ListActiveTemplatesAsync(customerId, cancellationToken);
        return CustomerServiceResult<IReadOnlyList<TemplateDto>>.Ok(templates.Select(Map).ToArray());
    }

    public async Task<CustomerServiceResult<TemplateDto>> GetAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ViewTemplates, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<TemplateDto>.FromAuthorization(authorization);
        }

        var template = await dbContext.FindTemplateAsync(customerId, templateId, cancellationToken);
        return template is null
            ? CustomerServiceResult<TemplateDto>.NotFound()
            : CustomerServiceResult<TemplateDto>.Ok(Map(template));
    }

    public async Task<CustomerServiceResult<TemplateDto>> CreateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CreateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageTemplates, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<TemplateDto>.FromAuthorization(authorization);
        }

        if (!TryParseTemplateType(request.TemplateType, out var templateType))
        {
            return CustomerServiceResult<TemplateDto>.BadRequest();
        }

        if (!validationService.Validate(templateType, request.Content, request.Language).IsValid)
        {
            return CustomerServiceResult<TemplateDto>.BadRequest();
        }

        if (await dbContext.FindTemplateBySlugAsync(customerId, request.Slug.Trim(), cancellationToken) is not null)
        {
            return CustomerServiceResult<TemplateDto>.Conflict();
        }

        try
        {
            var template = Template.Create(
                customerId,
                request.Name,
                request.Slug,
                request.Description,
                templateType,
                request.Content,
                request.Language,
                clock.UtcNow);

            dbContext.AddTemplate(template);
            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteTemplateAuditAsync(currentUser, template, "template.created", $"Template '{template.Name}' was created.", cancellationToken);

            return CustomerServiceResult<TemplateDto>.Ok(Map(template));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<TemplateDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<TemplateDto>> UpdateAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid templateId,
        UpdateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageTemplates, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<TemplateDto>.FromAuthorization(authorization);
        }

        if (!TryParseTemplateType(request.TemplateType, out var templateType))
        {
            return CustomerServiceResult<TemplateDto>.BadRequest();
        }

        if (!validationService.Validate(templateType, request.Content, request.Language).IsValid)
        {
            return CustomerServiceResult<TemplateDto>.BadRequest();
        }

        var template = await dbContext.FindTemplateAsync(customerId, templateId, cancellationToken);
        if (template is null)
        {
            return CustomerServiceResult<TemplateDto>.NotFound();
        }

        if (template.Status == TemplateStatus.Archived)
        {
            return CustomerServiceResult<TemplateDto>.BadRequest();
        }

        var existing = await dbContext.FindTemplateBySlugAsync(customerId, request.Slug.Trim(), cancellationToken);
        if (existing is not null && existing.Id != templateId)
        {
            return CustomerServiceResult<TemplateDto>.Conflict();
        }

        try
        {
            template.Update(
                request.Name,
                request.Slug,
                request.Description,
                templateType,
                request.Content,
                request.Language,
                clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteTemplateAuditAsync(currentUser, template, "template.updated", $"Template '{template.Name}' was updated.", cancellationToken);

            return CustomerServiceResult<TemplateDto>.Ok(Map(template));
        }
        catch (ArgumentException)
        {
            return CustomerServiceResult<TemplateDto>.BadRequest();
        }
    }

    public async Task<CustomerServiceResult<TemplateDto>> ArchiveAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageTemplates, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<TemplateDto>.FromAuthorization(authorization);
        }

        var template = await dbContext.FindTemplateAsync(customerId, templateId, cancellationToken);
        if (template is null)
        {
            return CustomerServiceResult<TemplateDto>.NotFound();
        }

        template.Archive(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        await WriteTemplateAuditAsync(currentUser, template, "template.archived", $"Template '{template.Name}' was archived.", cancellationToken);

        return CustomerServiceResult<TemplateDto>.Ok(Map(template));
    }

    public async Task<CustomerServiceResult<TemplateValidationResultDto>> ValidateRequestAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        ValidateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageTemplates, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<TemplateValidationResultDto>.FromAuthorization(authorization);
        }

        if (!TryParseTemplateType(request.TemplateType, out var templateType))
        {
            return CustomerServiceResult<TemplateValidationResultDto>.BadRequest();
        }

        return CustomerServiceResult<TemplateValidationResultDto>.Ok(
            validationService.Validate(templateType, request.Content, request.Language));
    }

    public async Task<CustomerServiceResult<TemplateValidationResultDto>> ValidateExistingAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeAsync(currentUser, customerId, Permission.ManageTemplates, cancellationToken);
        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<TemplateValidationResultDto>.FromAuthorization(authorization);
        }

        var template = await dbContext.FindTemplateAsync(customerId, templateId, cancellationToken);
        if (template is null)
        {
            return CustomerServiceResult<TemplateValidationResultDto>.NotFound();
        }

        return CustomerServiceResult<TemplateValidationResultDto>.Ok(
            validationService.Validate(template.TemplateType, template.Content, template.Language));
    }

    private async Task WriteTemplateAuditAsync(
        CurrentUserDto currentUser,
        Template template,
        string action,
        string message,
        CancellationToken cancellationToken)
    {
        await auditLogWriter.WriteAsync(new AuditLogWriteRequest(
            template.CustomerId,
            currentUser.Id,
            currentUser.DisplayName,
            AuditActorType.User,
            action,
            "Template",
            template.Id,
            template.Name,
            AuditOutcome.Succeeded,
            message,
            JsonSerializer.Serialize(new
            {
                templateId = template.Id,
                slug = template.Slug,
                templateType = template.TemplateType.ToString(),
                language = template.Language,
                contentLength = template.Content.Length
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

    private static bool TryParseTemplateType(string templateType, out TemplateType parsed)
    {
        return Enum.TryParse(templateType, ignoreCase: true, out parsed)
            && Enum.IsDefined(parsed);
    }

    private static TemplateDto Map(Template template)
    {
        return new TemplateDto(
            template.Id,
            template.CustomerId,
            template.Name,
            template.Slug,
            template.Description,
            template.TemplateType.ToString(),
            template.Content,
            template.Language,
            template.Status.ToString(),
            template.CreatedAt,
            template.UpdatedAt,
            template.ArchivedAt);
    }
}
