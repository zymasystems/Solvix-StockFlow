namespace Solvix.Sims.Models;

public enum MovementKind { Inbound, Outbound, Adjustment, QualityCheck }

public class StockMovement
{
	public string CompanyId { get; set; } = "";
    public string Warehouse { get; set; } = "";
	public string? Sku { get; init; }
	public string Title { get; init; } = "";
	public string Subtitle { get; init; } = "";
	public long Delta { get; init; }
	public MovementKind Kind { get; init; }
	public DateTime At { get; init; } = DateTime.Now;

	public string Ago
	{
		get
		{
			var span = DateTime.Now - At;
			if (span.TotalMinutes < 1) return "just now";
			if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
			if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
			if (span.TotalDays < 2) return "Yesterday";
			return $"{(int)span.TotalDays}d ago";
		}
	}

	public string QtyText => Delta > 0 ? $"+{Delta}" : Delta.ToString();

	// Visuals used by the feed rows (Tailwind classes live in .cs files too, the build scans them).
	public string Icon => Kind switch
	{
		MovementKind.Inbound => "move_to_inbox",
		MovementKind.Outbound => "outbox",
		MovementKind.Adjustment => "difference",
		_ => "task_alt"
	};

	public string TileClass => Kind switch
	{
		MovementKind.Inbound => "bg-surface-container text-primary",
		MovementKind.Outbound => "bg-surface-container-high text-secondary",
		MovementKind.Adjustment => "bg-amber-50 text-amber-700",
		_ => "bg-primary/10 text-primary"
	};

	public string QtyClass => Kind switch
	{
		MovementKind.Inbound => "text-primary",
		MovementKind.Outbound => "text-secondary",
		MovementKind.Adjustment => "text-amber-800",
		_ => "text-primary"
	};

	public string DotClass => Kind switch
	{
		MovementKind.Inbound => "bg-tertiary",
		MovementKind.Outbound => "bg-secondary",
		_ => "bg-primary"
	};
}
