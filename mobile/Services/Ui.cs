using Solvix.Sims.Models;

namespace Solvix.Sims.Services;

/// <summary>Small helpers that map data to the Stitch design's Tailwind classes.</summary>
public static class Ui
{
	public static string StatusLabel(StockStatus s) => s switch
	{
		StockStatus.OutOfStock => "Out of Stock",
		StockStatus.Low => "Low Stock",
		_ => "Optimal"
	};

	public static string StatusBadge(StockStatus s) => s switch
	{
		StockStatus.OutOfStock => "bg-[#fee2e2] text-[#b91c1c]",
		StockStatus.Low => "bg-[#fef3c7] text-[#b45309]",
		_ => "bg-[#ecfdf5] text-[#047857]"
	};

	public static string StatusDot(StockStatus s) => s switch
	{
		StockStatus.OutOfStock => "bg-[#ef4444]",
		StockStatus.Low => "bg-[#f59e0b]",
		_ => "bg-[#10b981]"
	};

	public static string Money(decimal v) => v.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-ZA")); // en-ZA => Rand (R) currency symbol
}
