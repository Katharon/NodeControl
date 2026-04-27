using System.Text.RegularExpressions;

namespace NodeControl.Application.Secrets;

public sealed partial class SecretReferenceParser
{
    public IReadOnlyList<string> ParseDistinctSlugs(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        return SecretReferenceRegex()
            .Matches(content)
            .Select(match => match.Groups["slug"].Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    [GeneratedRegex(@"secret://(?<slug>[a-z0-9][a-z0-9-]{1,99})(?![A-Za-z0-9_-])")]
    private static partial Regex SecretReferenceRegex();
}
