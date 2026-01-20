namespace VirtualRoulette.Common.Errors;

public static class DomainError
{
    public static class User
    {
        public static readonly Error NotFound = new(
            "DomainErrors.User.NotFound",
            "DomainErrors_Combinations_RequiredPoint",
            ErrorType.BadRequest
        );
    }
    
    public static class DbError
    {
        public static Error Error(string repositoryName, string message)
        {
            return new Error(
                "DbError.Error",
                $"Database error in {repositoryName}: {message}",
                ErrorType.InternalServerError
            );
        }
    }
    
    public static class InMemoryCache
    {
        public static Error Error(string cacheName, string message)
        {
            return new Error(
                "InMemoryCache.Error",
                $"InMemoryCache error in {cacheName}: {message}",
                ErrorType.InternalServerError
            );
        }
    }
    
    public static class PasswordHasher
    {
        public static readonly Error InvalidPassword = new(
            "DomainErrors.PasswordHasher.InvalidPassword",
            "Password cannot be null or empty.",
            ErrorType.BadRequest
        );
        
        public static readonly Error HashError = new(
            "DomainErrors.PasswordHasher.HashError",
            "Password cannot be null or empty.",
            ErrorType.BadRequest
        );
        
        public static readonly Error VerifyError = new(
            "DomainErrors.PasswordHasher.VerifyError",
            "PasswordHasher.VerifyError",
            ErrorType.BadRequest
        );
    }
    
    public static class Bet
    {
        public static readonly Error InvalidBet = new(
            "DomainErrors.Bet.InvalidBet",
            "The bet string is invalid or incorrectly formatted.",
            ErrorType.BadRequest
        );
        
        public static readonly Error InsufficientBalance = new(
            "DomainErrors.Bet.InsufficientBalance",
            "User does not have sufficient balance to place this bet.",
            ErrorType.BadRequest
        );
        
        public static readonly Error NotFound = new(
            "DomainErrors.Bet.NotFound",
            "Bet not found.",
            ErrorType.NotFound
        );
    }
}