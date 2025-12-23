using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;

namespace Favi_BE.Interfaces.Services
{
    public interface IVectorIndexService
    {
        /// <summary>
        /// Index a post in the vector database for semantic search.
        /// Returns true if successful, false otherwise.
        /// Does not throw exceptions - handles errors gracefully.
        /// </summary>
        Task<bool> IndexPostAsync(Post post, CancellationToken ct = default);

        /// <summary>
        /// Search for posts using semantic/vector search.
        /// Returns list of search results with scores, or empty list on error.
        /// </summary>
        Task<List<VectorSearchResultItem>> SearchAsync(
            Guid userId,
            string query,
            int k = 100,
            CancellationToken ct = default
        );

        /// <summary>
        /// Check if the vector index service is enabled and available.
        /// </summary>
        bool IsEnabled();
    }
}
