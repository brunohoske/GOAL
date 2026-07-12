namespace Goal.Application.Common;

public enum ErrorType { Validation, NotFound, Conflict, Forbidden, Unauthorized }

public sealed record Error(ErrorType Type, string Message)
{
    public static Error NotFound(string m) => new(ErrorType.NotFound, m);
    public static Error Validation(string m) => new(ErrorType.Validation, m);
    public static Error Conflict(string m) => new(ErrorType.Conflict, m);
    public static Error Forbidden(string m) => new(ErrorType.Forbidden, m);
    public static Error Unauthorized(string m) => new(ErrorType.Unauthorized, m);
}

public class Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool ok, Error? error) { IsSuccess = ok; Error = error; }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);
    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }
    internal Result(T? value, bool ok, Error? error) : base(ok, error) => Value = value;

    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}
