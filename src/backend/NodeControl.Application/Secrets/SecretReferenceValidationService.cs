using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Domain.Secrets;

namespace NodeControl.Application.Secrets;

public sealed class SecretReferenceValidationService(
    INodeControlDbContext dbContext,
    SecretReferenceParser parser)
{
    public async Task<SecretReferenceValidationResultDto> ValidateAsync(
        Guid customerId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var slugs = parser.ParseDistinctSlugs(content);
        if (slugs.Count == 0)
        {
            return new SecretReferenceValidationResultDto(true, [], [], []);
        }

        var references = new List<SecretReferenceDto>(slugs.Count);
        var errors = new List<string>();

        foreach (var slug in slugs)
        {
            var secret = await dbContext.FindSecretBySlugAsync(customerId, slug, cancellationToken);
            if (secret is null)
            {
                references.Add(new SecretReferenceDto(slug, false, null, null));
                errors.Add($"Secret reference 'secret://{slug}' does not exist.");
                continue;
            }

            references.Add(new SecretReferenceDto(slug, true, secret.Id, secret.Status.ToString()));
            if (secret.Status != SecretStatus.Active)
            {
                errors.Add($"Secret reference 'secret://{slug}' is {secret.Status}.");
            }
        }

        return new SecretReferenceValidationResultDto(errors.Count == 0, references, errors, []);
    }
}
