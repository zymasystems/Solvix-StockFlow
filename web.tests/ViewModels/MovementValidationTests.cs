using System.ComponentModel.DataAnnotations;
using Solvix.Web.ViewModels.Sales;
using Solvix.Web.ViewModels.Stock;
namespace Solvix.Web.Tests.ViewModels;
public sealed class MovementValidationTests
{
    private static List<ValidationResult> Errors(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
    [Theory]
    [InlineData("Sale")]
    [InlineData("Return")]
    [InlineData("Transfer")]
    public void Sale_WhenValid_Passes(string type)
    {
        Assert.Empty(Errors(new SaleCreateViewModel
        { ProductId = "product-001", Type = type, Quantity = 1, RequestId = Guid.NewGuid() }));
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Sale_WhenQuantityNotPositive_ReportsQuantityError(int quantity)
    {
        var errors = Errors(new SaleCreateViewModel
        { ProductId = "product-001", Type = "Sale", Quantity = quantity, RequestId = Guid.NewGuid() });
        Assert.Contains(errors, x => x.MemberNames.Contains("Quantity"));
    }
    [Fact]
    public void Sale_WhenRequestIdEmpty_ReportsRequestIdError()
    {
        var errors = Errors(new SaleCreateViewModel
        { ProductId = "product-001", Quantity = 1, RequestId = Guid.Empty });
        Assert.Contains(errors, x => x.MemberNames.Contains("RequestId"));
    }
    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    public void Stock_WhenQuantityMissingOrNegative_ReportsQuantityError(int? quantity)
    {
        var errors = Errors(new StockAdjustmentCreateViewModel
        { ProductId = "product-001", Type = "Set", Quantity = quantity, Reason = "Count", RequestId = Guid.NewGuid() });
        Assert.Contains(errors, x => x.MemberNames.Contains("Quantity"));
    }
    [Theory]
    [InlineData("Add")]
    [InlineData("Subtract")]
    public void Stock_WhenAddOrSubtractZero_ReportsQuantityError(string type)
    {
        var errors = Errors(new StockAdjustmentCreateViewModel
        { ProductId = "product-001", Type = type, Quantity = 0, Reason = "Count", RequestId = Guid.NewGuid() });
        Assert.Contains(errors, x => x.MemberNames.Contains("Quantity"));
    }
    [Fact]
    public void Stock_WhenSetZero_Passes()
    {
        Assert.Empty(Errors(new StockAdjustmentCreateViewModel
        { ProductId = "product-001", Type = "Set", Quantity = 0, Reason = "Physical count", RequestId = Guid.NewGuid() }));
    }
    [Fact]
    public void Stock_WhenRequestIdMissing_ReportsRequestIdError()
    {
        var errors = Errors(new StockAdjustmentCreateViewModel
        { ProductId = "product-001", Type = "Add", Quantity = 1, Reason = "Delivery", RequestId = null });
        Assert.Contains(errors, x => x.MemberNames.Contains("RequestId"));
    }
}
