#nullable enable
namespace VirtualRoulette.Models.DTOs;

public class ApiServiceResponse
{
    public string? Message { get; set; }
    public int StatusCode { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}

public class ApiServiceResponse<T> : ApiServiceResponse
{
    public ApiServiceResponse()
    {
    }

    public ApiServiceResponse(T data, ApiServiceResponse response)
    {
        Message = response.Message;
        StatusCode = response.StatusCode;
        ValidationErrors = response.ValidationErrors;
        Data = data;
    }

    public T? Data { get; set; }
}

public class SuccessApiServiceResponse<T> : ApiServiceResponse<T>
{
    public SuccessApiServiceResponse(T data)
    {
        Data = data;
        Message = "Success";
        StatusCode = 200;
    }
}
