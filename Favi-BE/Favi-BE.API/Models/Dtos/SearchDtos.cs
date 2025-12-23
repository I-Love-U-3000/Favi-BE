namespace Favi_BE.Models.Dtos
{
    public record SearchRequest(
        string Query,
        int Page,
        int PageSize
    );

    public record SearchResult(
        IEnumerable<SearchPostDto> Posts,
        IEnumerable<SearchTagDto> Tags
    );

    public record SearchPostDto(
        Guid Id,
        string Caption,
        string ThumbnailUrl
    );

    public record SearchTagDto(
        Guid Id,
        string Name,
        int PostCount
    );

    // Semantic search request from client
    public record SemanticSearchRequest(
        string Query,
        int? Page = 1,
        int? PageSize = 20,
        int? K = 100
    );
}
