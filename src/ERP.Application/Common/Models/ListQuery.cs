namespace ERP.Application.Common.Models;

public class ListQuery
{
    private const int MaxPageSize = 200;

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }

    public int NormalizedPageNumber => PageNumber <= 0 ? 1 : PageNumber;
    public int NormalizedPageSize => PageSize <= 0 ? 20 : Math.Min(PageSize, MaxPageSize);
    public bool SortDescending => string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
}
