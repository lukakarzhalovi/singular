using VirtualRoulette.Common.Errors;
using VirtualRoulette.Models.DTOs;

namespace VirtualRoulette.Common;

public static class ResultExtensions
{
    public static ApiServiceResponse ToApiResponse(this Result result, int? successStatusCode = null)
    {
        if (result.IsSuccess)
        {
            return new ApiServiceResponse
            {
                Message = "Success",
                StatusCode = successStatusCode ?? StatusCodes.Status200OK
            };
        }

        var firstError = result.Errors.Length > 0 ? result.Errors[0] : Error.None;
        var validationErrors = result.Errors
            .Where(e => e.ErrorType == ErrorType.Validation)
            .GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray());

        return new ApiServiceResponse
        {
            Message = firstError.Message,
            StatusCode = GetStatusCodeFromErrorType(firstError.ErrorType),
            ValidationErrors = validationErrors.Any() ? validationErrors : null
        };
    }

    public static ApiServiceResponse<T> ToApiResponse<T>(this Result<T> result, int? successStatusCode = null)
    {
        if (result.IsSuccess)
        {
            return new ApiServiceResponse<T>
            {
                Data = result.Value,
                Message = "Success",
                StatusCode = successStatusCode ?? StatusCodes.Status200OK
            };
        }

        var baseResponse = result.ToApiResponse(successStatusCode);
        return new ApiServiceResponse<T>(default!, baseResponse);
    }

    private static int GetStatusCodeFromErrorType(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.BadRequest => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.InternalServerError => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}