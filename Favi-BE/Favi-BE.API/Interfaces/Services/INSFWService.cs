using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;

namespace Favi_BE.Interfaces.Services
{
    public interface INSFWService
    {
        bool IsEnabled();

        /// <summary>
        /// Check if a post contains NSFW content
        /// </summary>
        Task<bool> CheckPostAsync(Post post, CancellationToken ct = default);
    }
}
