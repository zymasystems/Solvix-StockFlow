using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Solvix.Web.Connected;
// Legacy global endpoints are not discoverable; company routes are the only inventory write path.
public sealed class RetiredControllers : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        var keep = new[]
        {
            "HomeController",
            "AccountController",
            "ProfileApiController",
            "CompaniesApiController",
            "WorkspaceController"
        };
        foreach (var controller in feature.Controllers.Where(c => !keep.Contains(c.Name)).ToArray())
            feature.Controllers.Remove(controller);
    }
}
