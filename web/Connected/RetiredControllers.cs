using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Solvix.Web.Connected;
// Keep the original MVC pages, now backed by company services. Legacy global APIs stay retired.
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
            "WorkspaceController",
            "CompaniesController",
            "DashboardController", "ProductsController", "SuppliersController",
            "SalesController", "StockController", "ReportsController", "UsersController"
        };
        foreach (var controller in feature.Controllers.Where(c => !keep.Contains(c.Name)).ToArray())
            feature.Controllers.Remove(controller);
    }
}
