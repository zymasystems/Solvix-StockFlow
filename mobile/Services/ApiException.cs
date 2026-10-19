namespace Solvix.Sims.Services;

/// <summary>
/// Error returned by the Solvix web API, including the HTTP status code and the ProblemDetails detail.
/// </summary>
public sealed class ApiException : Exception
{
    /// <summary>HTTP status code returned by the API, when available.</summary>
    public int? StatusCode { get; }

    /// <summary>Detail from the API's ProblemDetails response, when available.</summary>
    public string? Detail { get; }

    public ApiException(string message, int? statusCode = null, string? detail = null)
        : base(message)
    {
        StatusCode = statusCode;
        Detail = detail;
    }
}
