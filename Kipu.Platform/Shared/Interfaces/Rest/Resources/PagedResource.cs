namespace Kipu.Platform.Shared.Interfaces.Rest.Resources;

public record PagedResource<T>(IEnumerable<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);
