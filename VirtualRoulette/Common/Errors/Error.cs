namespace VirtualRoulette.Common.Errors;

public class Error(string code, string? message, ErrorType errorType)
    : IEquatable<Error>
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    public string Code { get; } = code ?? throw new ArgumentNullException(nameof(code));
    public string Message { get; } = message ?? string.Empty;
    public ErrorType ErrorType { get; } = errorType;

    public static implicit operator string(Error error) => error.Code;

    public static bool operator ==(Error? a, Error? b)
    {
        if (a is null && b is null)
            return true;
        if (a is null || b is null)
            return false;
        return a.Equals(b);
    }

    public static bool operator !=(Error? a, Error? b) => !(a == b);

    public bool Equals(Error? other)
    {
        if (other is null)
            return false;
        return Code == other.Code && Message == other.Message && ErrorType == other.ErrorType;
    }

    public override bool Equals(object? obj)
    {
        return obj is Error other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Code, Message, ErrorType);
    }

    public override string ToString() => Code;
}

public class ErrorPayload
{
    public Dictionary<string, object> Data { get; set; } = new();
}
