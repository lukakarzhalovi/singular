using VirtualRoulette.Shared.Errors;
using ErrorPayload = VirtualRoulette.Shared.Errors.ErrorPayload;

namespace VirtualRoulette.Shared.Result;

public class Result
{
    protected Result(bool isSuccess, Error error, ErrorPayload? errorPayload)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException();
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException();
        
        IsSuccess = isSuccess;
        Errors = [error];
        ErrorPayload = errorPayload;
    }

    protected Result(bool isSuccess, Error[] errors, ErrorPayload? errorPayload)
    {
        IsSuccess = isSuccess;
        Errors = errors;
        ErrorPayload = errorPayload;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public ErrorPayload? ErrorPayload { get; }
    public Error[] Errors { get; }
    
    public Error FirstError => Errors.Length > 0 ? Errors[0] : Error.None;

    public static Result Success() => new Result(true, Error.None, null);

    public static Result<TValue> Success<TValue>(TValue? value)
    {
        return new Result<TValue>(value, true, Error.None);
    }

    public static Result Failure(Error error, ErrorPayload? errorPayload = null)
    {
        return new Result(false, error, errorPayload);
    }

    public static Result Failure(string message)
    {
        return new Result(false, new Error("Error", message, ErrorType.BadRequest), null);
    }

    public static Result Failure(Error[] errors, ErrorPayload? errorPayload = null)
    {
        return new Result(false, errors, errorPayload);
    }

    public static Result<TValue> Failure<TValue>(Error error)
    {
        return new Result<TValue>(default!, null, false, error);
    }

    public static Result<TValue> Failure<TValue>(Error[] errors, ErrorPayload? errorPayload = null)
    {
        return new Result<TValue>(default!, errorPayload, false, errors);
    }

    public static Result<TValue> Failure<TValue>(Error error, ErrorPayload? errorPayload = null)
    {
        return new Result<TValue>(default!, errorPayload, false, error);
    }

    public static Result<TValue> Failure<TValue>(string message)
    {
        return new Result<TValue>(default!, null, false, new Error("Error", message, ErrorType.BadRequest));
    }
}

public class Result<T> : Result
{
    public T Value { get; private set; }

    internal Result(T value, bool isSuccess, Error error) 
        : base(isSuccess, error, null)
    {
        Value = value;
    }

    internal Result(T value, ErrorPayload? errorPayload, bool isSuccess, Error error)
        : base(isSuccess, error, errorPayload)
    {
        Value = value;
    }

    internal Result(T value, ErrorPayload? errorPayload, bool isSuccess, Error[] errors)
        : base(isSuccess, errors, errorPayload)
    {
        Value = value;
    }

    public static implicit operator Result<T>(T value) => Success(value);
}
