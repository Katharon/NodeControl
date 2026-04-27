namespace NodeControl.Application.Playbooks;

public sealed record PlaybookValidationResultDto(
    bool IsValid,
    string Message,
    IReadOnlyCollection<string> Errors);
