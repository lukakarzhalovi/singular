#nullable enable
namespace VirtualRoulette.Core.DTOs.Responses;

public record ApiServiceResponse
{
    public string? Message { get; set; }
    public int StatusCode { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}

public record ApiServiceResponse<T> : ApiServiceResponse
{
    public T? Data { get; set; }
}