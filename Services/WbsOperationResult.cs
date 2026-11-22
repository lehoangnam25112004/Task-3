namespace ConstructionProjectManagement.Services;

public enum WbsFailureKind
{
    None,
    Forbidden,
    NotFound,
    Validation
}

public sealed record WbsOperationResult<T>(
    WbsFailureKind Failure,
    T? Value = default,
    string? Error = null,
    string? Warning = null)
{
    public bool Succeeded => Failure == WbsFailureKind.None;

    public static WbsOperationResult<T> Success(T value, string? warning = null) =>
        new(WbsFailureKind.None, value, Warning: warning);

    public static WbsOperationResult<T> Forbidden() =>
        new(WbsFailureKind.Forbidden);

    public static WbsOperationResult<T> NotFound() =>
        new(WbsFailureKind.NotFound);

    public static WbsOperationResult<T> Invalid(string error) =>
        new(WbsFailureKind.Validation, Error: error);
}
