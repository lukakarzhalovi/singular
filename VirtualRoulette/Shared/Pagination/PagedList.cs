namespace VirtualRoulette.Shared.Pagination;

public class PagedList<T>
{
    public required List<T> Items { get; set; }
    public required long TotalCount { get; set; }
}
