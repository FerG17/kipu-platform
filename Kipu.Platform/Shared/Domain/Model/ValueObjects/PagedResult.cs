namespace Kipu.Platform.Shared.Domain.Model.ValueObjects;

/// <summary>One page of a collection query, plus the total count needed to render pagination controls.</summary>
public record PagedResult<T>(IReadOnlyCollection<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
