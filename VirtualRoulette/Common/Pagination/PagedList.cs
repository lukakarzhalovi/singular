namespace VirtualRoulette.Common.Pagination;

public class PagedList<T>(List<T> items, long totalCount)
{
    public long TotalCount { get; } = totalCount;
    public List<T> Result { get; } = items;
}
