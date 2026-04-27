using System.Text.RegularExpressions;

namespace NodeControl.Domain.Playbooks;

internal static partial class DefinitionValidation
{
    public static void ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > 200)
        {
            throw new ArgumentException("Name must be at most 200 characters.", nameof(name));
        }
    }

    public static void ValidateSlug(string slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        if (!SlugRegex().IsMatch(slug.Trim()))
        {
            throw new ArgumentException("Slug is invalid.", nameof(slug));
        }
    }

    public static string? NormalizeOptional(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{name} must be at most {maxLength} characters.", name);
        }

        return trimmed;
    }

    public static void ValidateContent(string content, int maxLength, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        if (content.Length > maxLength)
        {
            throw new ArgumentException($"{name} must be at most {maxLength} characters.", name);
        }
    }

    public static string ValidateEntryFilePath(string? entryFilePath)
    {
        var value = string.IsNullOrWhiteSpace(entryFilePath) ? "site.yml" : entryFilePath.Trim();
        if (value.Length > 500
            || Path.IsPathRooted(value)
            || value.Split('/', '\\').Any(part => part == ".."))
        {
            throw new ArgumentException("Entry file path is invalid.", nameof(entryFilePath));
        }

        return value;
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,99}$")]
    private static partial Regex SlugRegex();
}
