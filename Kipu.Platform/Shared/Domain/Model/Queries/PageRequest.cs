namespace Kipu.Platform.Shared.Domain.Model.Queries;

/// <summary>
///     Clamps page/pageSize once at the API boundary so every collection
///     endpoint shares the same defaults and hard cap (X4 S3) — before this,
///     a GET with no paging returned the entire table, unbounded.
/// </summary>
public record PageRequest(int Page, int PageSize)
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    public static PageRequest Create(int? page, int? pageSize)
    {
        var normalizedPage = page is > 0 ? page.Value : 1;
        var normalizedPageSize = pageSize switch
        {
            null or <= 0 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize.Value
        };
        return new PageRequest(normalizedPage, normalizedPageSize);
    }

    public int Skip => (Page - 1) * PageSize;
}
