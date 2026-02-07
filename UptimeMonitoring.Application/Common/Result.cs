namespace UptimeMonitoring.Application.Common;

public class Result<T>
{
    public T? Value { get; }
    public Error? Error { get; }
    public bool IsSuccess => Error == null;
    public bool IsFailure => !IsSuccess;

    private Result(T value)
    {
        Value = value;
    }

    private Result(Error error)
    {
        Error = error;
    }

    public static Result<T> Success(T value) => new Result<T>(value);
    public static Result<T> Failure(Error error) => new Result<T>(error);
}

public class Result
{
    public Error? Error { get; }
    public bool IsSuccess => Error == null;
    public bool IsFailure => !IsSuccess;

    protected Result(Error? error)
    {
        Error = error;
    }

    public static Result Success() => new(null);
    public static Result Failure(Error error) => new(error);
}

public class Error
{
    public string Code { get; }
    public string Message { get; }

    public Error(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public static Error NotFound(string message = "Resource not found") => new("NotFound", message);
    public static Error Conflict(string message = "Conflict occurred") => new("Conflict", message);
    public static Error Unauthorized(string message = "Unauthorized") => new("Unauthorized", message);
    public static Error Validation(string message = "Validation failed") => new("Validation", message);
    public static Error Unexpected(string message = "An unexpected error occurred") => new("Unexpected", message);
}
