namespace NodeControl.Application.VariableSets;

public sealed record VariableSetValidationResultDto(
    bool IsValid,
    string Message,
    IReadOnlyCollection<string> Errors);
