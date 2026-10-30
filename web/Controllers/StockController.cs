using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solvix.Web.Authorization;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services.Interfaces;

using Solvix.Web.Controllers.Support;
using Solvix.Web.Services;
using Solvix.Web.ViewModels.Stock;
namespace Solvix.Web.Controllers;
[Authorize(Policy="CompanyIdentity")]
[AutoValidateAntiforgeryToken]
[ServiceFilter(typeof(Solvix.Web.Connected.CompanyPageFilter))]
public sealed class StockController : Controller
{
    private readonly IStockService stock;private readonly IProductService products;
    private readonly Solvix.Web.Connected.CompanyMvcContext context;
    public StockController(IStockService stock,IProductService products,Solvix.Web.Connected.CompanyMvcContext context){this.stock=stock;this.products=products;this.context=context;}

    [HttpGet("Stock/Approvals"), HttpGet("Audits"), HttpGet("Receiving"), HttpGet("Receiving/Approvals"), HttpGet("InboundArrivals")]
    public async Task<IActionResult> Approvals() => View(await context.RequireAsync());

    [HttpPost]
    public async Task<IActionResult> SubmitReceipt(string productId, int quantity, string? reference, string? notes, Guid requestId)
    {
        try
        {
            if (requestId == Guid.Empty || quantity <= 0 || (reference?.Length ?? 0) > 200 || (notes?.Length ?? 0) > 1000)
                throw new ServiceException(400, "Choose a product, a positive quantity and a valid delivery reference.");
            await context.ExecuteAsync("receipt.create", new() { RequestId = requestId, ProductId = productId ?? "", WarehouseId = context.RequireWarehouse(), Quantity = quantity, Reference = reference ?? "", Notes = notes ?? "" });
            TempData["Success"] = "Delivery submitted. Confirm arrival and verify the item before administrator approval.";
        }
        catch(ServiceException e) when(e.StatusCode is 400 or 403 or 404 or 409) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Approvals));
    }

    [HttpPost, Authorize(Policy="CompanyAdmin")]
    public Task<IActionResult> ReviewAudit(string id, bool approve, Guid requestId) => Review("audit." + (approve ? "approve" : "reject"), id, requestId);

    [HttpPost]
    public Task<IActionResult> ConfirmArrival(string id, Guid requestId) => Review("receipt.arrive", id, requestId);

    [HttpPost]
    public Task<IActionResult> VerifyReceipt(string id, string barcode, Guid requestId) => Review("receipt.verify", id, requestId, barcode);

    [HttpPost, Authorize(Policy="CompanyAdmin")]
    public Task<IActionResult> ReviewReceipt(string id, bool approve, Guid requestId) => Review("receipt." + (approve ? "approve" : "reject"), id, requestId);

    private async Task<IActionResult> Review(string command, string id, Guid requestId, string barcode = "")
    {
        try
        {
            if (requestId == Guid.Empty) throw new ServiceException(400, "Reload the approval form before submitting.");
            await context.ExecuteAsync(command, new() { Id = id ?? "", RequestId = requestId, Barcode = barcode ?? "" });
            TempData["Success"] = command switch
            {
                "receipt.arrive" => "Arrival confirmed. Verify the item before approval.",
                "receipt.verify" => "Item verified. An administrator can now approve this receipt.",
                "receipt.approve" => "Receipt approved and stock added.",
                "audit.approve" => "Adjustment approved and stock updated.",
                _ => "Request rejected. Stock has not changed."
            };
        }
        catch(ServiceException e) when(e.StatusCode is 400 or 403 or 404 or 409)
        { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Approvals));
    }
    [HttpGet]
    public async Task<IActionResult> Index(int pageSize=50,string? cursor=null)=>await History(pageSize,cursor);
    [HttpGet]
    public async Task<IActionResult> History(int pageSize=50,string? cursor=null)
    {
        var page=await stock.ListAsync(pageSize,cursor);ViewData["NextCursor"]=page.NextCursor;ViewData["PageSize"]=pageSize;
        return View("History",new StockHistoryViewModel{Adjustments=page.Items.Select(ViewMapping.Adjustment).ToList()});
    }
    [HttpGet]
    public async Task<IActionResult> Adjust()=>View(new StockAdjustmentCreateViewModel{
        RequestId=Guid.NewGuid(),ProductOptions=await FormOptions.Products(products)});
    [HttpPost]
    public async Task<IActionResult> Adjust(StockAdjustmentCreateViewModel model)
    {
        if(ModelState.IsValid)try{
            await stock.AdjustAsync(new AdjustStockRequest{ProductId=model.ProductId,Type=model.Type,
                Quantity=model.Quantity,Reason=model.Reason,RequestId=model.RequestId});
            TempData["Success"] = "Adjustment submitted for approval. Stock has not changed. Review it under Stock approvals.";
            return RedirectToAction(nameof(History));
        }catch(ServiceException e)when(e.StatusCode is 400 or 409){ModelState.AddModelError("",e.Message);}
        model.ProductOptions=await FormOptions.Products(products,model.ProductId);return View(model);
    }
}
