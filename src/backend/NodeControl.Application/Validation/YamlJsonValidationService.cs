using System.Text.Json;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace NodeControl.Application.Validation;

public sealed class YamlJsonValidationService
{
    private readonly IDeserializer yamlDeserializer = new DeserializerBuilder().Build();

    public ValidationResult ValidateYaml(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return ValidationResult.Invalid("Content is required.", ["Content is required."]);
        }

        try
        {
            yamlDeserializer.Deserialize<object>(content);
            return ValidationResult.Valid("YAML syntax is valid.");
        }
        catch (YamlException exception)
        {
            return ValidationResult.Invalid("YAML syntax is invalid.", [exception.Message]);
        }
    }

    public ValidationResult ValidateJson(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return ValidationResult.Invalid("Content is required.", ["Content is required."]);
        }

        try
        {
            using var _ = JsonDocument.Parse(content);
            return ValidationResult.Valid("JSON syntax is valid.");
        }
        catch (JsonException exception)
        {
            return ValidationResult.Invalid("JSON syntax is invalid.", [exception.Message]);
        }
    }
}

public sealed record ValidationResult(bool IsValid, string Message, IReadOnlyCollection<string> Errors)
{
    public static ValidationResult Valid(string message)
    {
        return new ValidationResult(true, message, []);
    }

    public static ValidationResult Invalid(string message, IReadOnlyCollection<string> errors)
    {
        return new ValidationResult(false, message, errors);
    }
}
