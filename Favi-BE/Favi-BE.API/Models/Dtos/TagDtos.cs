namespace Favi_BE.Models.Dtos
{
    public record TagResponse(
        Guid Id,
        string Name,
        int PostCount
    );
}
