namespace BudgetPipeline;

public sealed record Error(string Code, string Message, string? Context = null)
{
    public override string ToString() =>
        Context is null ? $"[{Code}] {Message}" : $"[{Code}] {Message} ({Context})";
}

public static class ErrorCodes
{
    public const string MissingRequest = "MISSING_REQUEST";
    public const string InvalidId = "INVALID_ID";
    public const string InvalidAmount = "INVALID_AMOUNT";
    public const string UnknownCategory = "UNKNOWN_CATEGORY";
    public const string BudgetExceeded = "BUDGET_EXCEEDED";
    public const string InvalidState = "INVALID_STATE";
    public const string FileNotFound = "FILE_NOT_FOUND";
    public const string FileAccess = "FILE_ACCESS_ERROR";
    public const string MalformedContent = "MALFORMED_CONTENT";
    public const string NativeIntegration = "NATIVE_INTEGRATION_ERROR";
}

public static class Errors
{
    public static Error MissingRequest() =>
        new(ErrorCodes.MissingRequest, "Request is missing.");

    public static Error InvalidId(int id) =>
        new(ErrorCodes.InvalidId, "Id must be positive.", $"id: {id}");

    public static Error InvalidAmount(int id, decimal amount) =>
        new(ErrorCodes.InvalidAmount, "Amount must be greater than zero.",
            $"request {id}, amount: {Format.Money(amount)}");

    public static Error UnknownCategory(string category) =>
        new(ErrorCodes.UnknownCategory, "Category does not exist.", $"category: '{category}'");

    public static Error BudgetExceeded(string category, decimal remaining, decimal amount) =>
        new(ErrorCodes.BudgetExceeded, "Amount exceeds the remaining budget.",
            $"category: {category}, remaining {Format.Money(remaining)}, requested {Format.Money(amount)}");

    public static Error InvalidState(string state, string operation) =>
        new(ErrorCodes.InvalidState, $"A {state} expense cannot be {operation}.");

    public static Error FileNotFound(string path) =>
        new(ErrorCodes.FileNotFound, "Data file was not found.", $"file: {path}");

    public static Error FileAccess(string path, string detail) =>
        new(ErrorCodes.FileAccess, $"Data file could not be read: {detail}", $"file: {path}");

    public static Error MalformedContent(string path, string detail) =>
        new(ErrorCodes.MalformedContent, $"Data file content is malformed: {detail}", $"file: {path}");

    public static Error NativeIntegration(string detail) =>
        new(ErrorCodes.NativeIntegration, "Native budget calculation failed.", detail);
}
