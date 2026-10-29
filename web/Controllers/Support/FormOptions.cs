using Microsoft.AspNetCore.Mvc.Rendering;
using Solvix.Web.Services;
using Solvix.Web.Services.Interfaces;
namespace Solvix.Web.Controllers.Support;
internal static class FormOptions
{
    public static List<SelectListItem> Categories(string? selected = null)
    {
        var names = new List<string>{"Electronics","Peripherals","Accessories","Audio"};
        if(!string.IsNullOrWhiteSpace(selected) && !names.Contains(selected)) names.Add(selected);
        return names.Select(x=>new SelectListItem(x,x,x==selected)).ToList();
    }
    public static async Task<List<SelectListItem>> Suppliers(ISupplierService suppliers, string? selected = null)
    {
        var result = new List<SelectListItem>(); string? cursor = null;
        do
        {
            var page = await suppliers.ListAsync(100, cursor);
            result.AddRange(page.Items.Select(x => new SelectListItem(x.Name + (string.IsNullOrEmpty(x.Code) ? "" : " (" + x.Code + ")"), x.Id, x.Id == selected)));
            cursor = page.NextCursor;
            if (result.Count > 10000 || (result.Count == 10000 && cursor is not null))
                throw new ServiceException(422, "Supplier directory too large for a dropdown.");
        } while (cursor is not null);
        return result.OrderBy(x => x.Text).ToList();
    }
    // Small-catalog starter: fail explicitly instead of silently omitting products.
    public static async Task<List<SelectListItem>> Products(IProductService products,string? selected=null)
    {
        var result = new List<SelectListItem>(); string? cursor = null;
        do
        {
            var page = await products.ListAsync(100,cursor);
            result.AddRange(page.Items.Select(x=>new SelectListItem($"{x.Name} ({x.Sku}) — stock {x.Stock}",x.Id,x.Id==selected)));
            cursor = page.NextCursor;
            if(result.Count>10000 || (result.Count==10000 && cursor is not null))
                throw new ServiceException(422,"Catalog too large for a dropdown. Implement a paginated product search.");
        } while(cursor is not null);
        return result.OrderBy(x=>x.Text).ToList();
    }
}
