using Solvix.Sims.Services;
namespace Solvix.Sims.Services.Authentication;
public static class AuthMessages
{
    public static string Describe(Exception error) => error switch
    {
        AuthException known => known.Message,
        ApiException api => api.Detail ?? api.Message,
        HttpRequestException => "The service could not be reached. Check your connection and HTTPS backend address.",
        OperationCanceledException => "The request timed out or was cancelled. Try again.",
        _ => "Sign-in could not be completed. Check the app configuration or contact your administrator."
    };
}
