using System.Text.RegularExpressions;

namespace NodeControl.Application.JobRuns;

public static class JobRunLogRedactor
{
    private const string Redacted = "[REDACTED]";

    private static readonly Regex BearerTokenPattern = new(
        @"\bBearer\s+[A-Za-z0-9._~+/=-]{8,}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex SecretReferenceValuePattern = new(
        @"\b(secret://[a-z0-9][a-z0-9-]{1,99})(\s*[:=]\s*)([^\s,;]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex SensitiveKeyValuePattern = new(
        @"\b(password|passwd|pwd|token|api[_-]?key|access[_-]?token|client[_-]?secret|secret)(\s*[:=]\s*)(?!//)(""[^""]*""|'[^']*'|[^\s,;]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string? Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var redacted = BearerTokenPattern.Replace(value, $"Bearer {Redacted}");
        redacted = SecretReferenceValuePattern.Replace(redacted, match => $"{match.Groups[1].Value}{match.Groups[2].Value}{Redacted}");
        redacted = SensitiveKeyValuePattern.Replace(redacted, match =>
        {
            var sensitiveValue = match.Groups[3].Value;
            if (sensitiveValue.StartsWith("secret://", StringComparison.OrdinalIgnoreCase)
                || sensitiveValue.StartsWith("\"secret://", StringComparison.OrdinalIgnoreCase)
                || sensitiveValue.StartsWith("'secret://", StringComparison.OrdinalIgnoreCase))
            {
                return match.Value;
            }

            return $"{match.Groups[1].Value}{match.Groups[2].Value}{Redacted}";
        });

        return redacted;
    }
}
