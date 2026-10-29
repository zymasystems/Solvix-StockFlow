using System.Text;

namespace Solvix.Web.Connected;

public sealed class CompanyNameClaim
{
    public string CompanyId { get; set; } = "";
}

public sealed class CompanyCreationRequest
{
    public bool Created { get; set; }
    public string NameKey { get; set; } = "";
    public string CompanyId { get; set; } = "";
}

public static class CompanyNames
{
    public static string Normalize(string name) => string.Join(" ", name.Normalize(NormalizationForm.FormKC)
        .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
    public static string ClaimPath(string name) => "companyNames/" + CompanyService.Key(Normalize(name));
}
