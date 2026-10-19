namespace Solvix.Web.Contracts.Responses;

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();

    public string? NextCursor { get; set; }

    public bool HasMore => !string.IsNullOrEmpty(NextCursor);
}