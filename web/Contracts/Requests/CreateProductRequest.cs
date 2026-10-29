using System.ComponentModel.DataAnnotations;

namespace Solvix.Web.Contracts.Requests;

public class CreateProductRequest
{
    [StringLength(128)]
    public string WarehouseId { get; set; } = "";
    [StringLength(128)]
    public string? SupplierId { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [Range(
    typeof(decimal),
    "0.01",
    "999999999.99",
    ParseLimitsInInvariantCulture = true,
    ConvertValueInInvariantCulture = true)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int OpeningStock { get; set; }

    [Range(0, int.MaxValue)]
    public int MinStock { get; set; }
}