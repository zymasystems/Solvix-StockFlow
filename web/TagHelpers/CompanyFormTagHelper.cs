using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Solvix.Web.Connected;
using Solvix.Web.ViewModels.Sales;

namespace Solvix.Web.TagHelpers;

// Keep the uploaded Razor markup intact. Add only the context required to validate writes.
[HtmlTargetElement("form", Attributes = "method")]
public sealed class CompanyFormTagHelper(CompanyMvcContext company) : TagHelper
{
    [HtmlAttributeNotBound, ViewContext]
    public ViewContext ViewContext { get; set; } = default!;
    public override int Order => 1000;
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!string.Equals(output.Attributes["method"]?.Value?.ToString(), "post", StringComparison.OrdinalIgnoreCase)
            || output.Attributes.ContainsName("data-logout")
            || ViewContext.ViewData["CompanyContextId"] is null) return;
        foreach (var (name, key) in new[] { ("CompanyContextId", "CompanyContextId"), ("WarehouseContextId", "WarehouseContextId"), ("CompanyOperationId", "CompanyOperationId") })
        {
            var hidden = new TagBuilder("input") { TagRenderMode = TagRenderMode.SelfClosing };
            hidden.Attributes["type"] = "hidden";
            hidden.Attributes["name"] = name;
            hidden.Attributes["value"] = ViewContext.ViewData[key]?.ToString() ?? "";
            output.PostContent.AppendHtml(hidden);
        }
        // A destination is required for the original form's Transfer option to use the shared workflow.
        if (ViewContext.ViewData.Model is SaleCreateViewModel sale && company.State is { } state)
        {
            var field = new TagBuilder("div"); field.AddCssClass("field");
            var label = new TagBuilder("label"); label.Attributes["for"] = "DestinationWarehouseId";
            label.InnerHtml.Append("Destination warehouse (transfers only)"); field.InnerHtml.AppendHtml(label);
            var select = new TagBuilder("select"); select.Attributes["name"] = "DestinationWarehouseId"; select.Attributes["id"] = "DestinationWarehouseId";
            var prompt = new TagBuilder("option"); prompt.Attributes["value"] = ""; prompt.InnerHtml.Append("Choose a destination for transfers"); select.InnerHtml.AppendHtml(prompt);
            foreach (var warehouse in state.Warehouses.Where(w => w.Id != company.WarehouseId))
            {
                var option = new TagBuilder("option"); option.Attributes["value"] = warehouse.Id;
                if (warehouse.Id == sale.DestinationWarehouseId) option.Attributes["selected"] = "selected";
                option.InnerHtml.Append(warehouse.Name); select.InnerHtml.AppendHtml(option);
            }
            field.InnerHtml.AppendHtml(select); output.PreContent.AppendHtml(field);
        }
    }
}
