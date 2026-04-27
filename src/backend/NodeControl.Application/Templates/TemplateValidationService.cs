using NodeControl.Domain.Templates;

namespace NodeControl.Application.Templates;

public sealed class TemplateValidationService
{
    private const int MaxContentLength = 200000;

    public TemplateValidationResultDto Validate(TemplateType templateType, string content, string? language)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(content))
        {
            errors.Add("Content is required.");
        }
        else if (content.Length > MaxContentLength)
        {
            errors.Add($"Content must be at most {MaxContentLength} characters.");
        }

        if (!string.IsNullOrWhiteSpace(language) && language.Trim().Length > 100)
        {
            errors.Add("Language must be at most 100 characters.");
        }

        if (templateType == TemplateType.Jinja2 && !string.IsNullOrEmpty(content))
        {
            AddDelimiterErrors(content, "{{", "}}", errors);
            AddDelimiterErrors(content, "{%", "%}", errors);
        }

        if (templateType == TemplateType.ShellScript
            && !string.IsNullOrWhiteSpace(content)
            && !content.TrimStart().StartsWith("#!", StringComparison.Ordinal))
        {
            warnings.Add("Shell script templates usually start with a shebang.");
        }

        if (ContainsSecretLikeWord(content))
        {
            warnings.Add("Content contains secret-like words. Avoid storing secrets in templates.");
        }

        return new TemplateValidationResultDto(errors.Count == 0, errors, warnings);
    }

    private static void AddDelimiterErrors(
        string content,
        string openDelimiter,
        string closeDelimiter,
        ICollection<string> errors)
    {
        var position = 0;
        while (position < content.Length)
        {
            var open = content.IndexOf(openDelimiter, position, StringComparison.Ordinal);
            var close = content.IndexOf(closeDelimiter, position, StringComparison.Ordinal);

            if (close >= 0 && (open < 0 || close < open))
            {
                errors.Add($"Found closing delimiter '{closeDelimiter}' before opening delimiter '{openDelimiter}'.");
                position = close + closeDelimiter.Length;
                continue;
            }

            if (open < 0)
            {
                break;
            }

            close = content.IndexOf(closeDelimiter, open + openDelimiter.Length, StringComparison.Ordinal);
            if (close < 0)
            {
                errors.Add($"Unclosed template delimiter '{openDelimiter}'.");
                break;
            }

            position = close + closeDelimiter.Length;
        }
    }

    private static bool ContainsSecretLikeWord(string content)
    {
        return content.Contains("password", StringComparison.OrdinalIgnoreCase)
            || content.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || content.Contains("token", StringComparison.OrdinalIgnoreCase)
            || content.Contains("private_key", StringComparison.OrdinalIgnoreCase);
    }
}
