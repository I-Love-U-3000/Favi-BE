namespace Favi_BE.Models.Dtos
{
    public record PaginationResult<T>(
        IReadOnlyList<T> Data,
        int Page,
        int Size,
        bool HasPrevious,
        bool HasNext,
        int TotalCount = 0
    )
    {
        public static PaginationResult<T> Create(IReadOnlyList<T> data, int page, int size, int totalCount)
        {
            var hasPrevious = page > 1;
            var hasNext = data.Count > 0 && (long)page * size < totalCount;
            return new PaginationResult<T>(data, page, size, hasPrevious, hasNext, totalCount);
        }
    }

    public record PagedResult<T>(
        IEnumerable<T> Items,
        int Page,
        int PageSize,
        int TotalCount
    );
}

