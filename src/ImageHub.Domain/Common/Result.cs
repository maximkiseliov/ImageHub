namespace ImageHub.Domain.Common;

public abstract class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }

    public static Result Success() => Result<Unit>.Success(Unit.Value);
    public static Result Failure(Error error) => Result<Unit>.Failure(error);
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Failure result can't have a value");

    private Result(T value) : base(true, Error.None)
    {
        _value = value;
    }

    private Result(Error error) : base(false, error)
    {
        _value = default;
    }

    public static Result<T> Success(T value) => new(value);
#pragma warning disable CS0108, CS0114
    public static Result<T> Failure(Error error) => new(error);
#pragma warning restore CS0108, CS0114
}