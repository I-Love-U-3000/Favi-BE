namespace Favi_BE.Models.Dtos
{
    public record PagedResult<T>(
        IEnumerable<T> Items,
        int Page,
        int PageSize,
        int TotalCount
    );
}
