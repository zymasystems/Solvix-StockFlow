namespace Solvix.Sims.Services.Authentication;

public sealed class AuthException : Exception
{
    public string Code { get; }
    public AuthException(string code, string message) : base(message) => Code = code;
}
