using Favi_BE.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Favi_BE.Interfaces.Repositories
{
    public interface IPostRepository : IGenericRepository<Post>
    {
        Task<IEnumerable<Post>> GetPostsByProfileIdAsync(Guid profileId, int skip, int take);
        Task<IEnumerable<Post>> GetPostsWithMediaAsync(int skip, int take);
        Task<Post?> GetPostWithDetailsAsync(Guid postId);

        Task<Post?> GetPostWithAllAsync(Guid postId);

        Task<IEnumerable<Post>> GetFeedByFollowingsAsync(Guid profileId, int skip, int take);
        // Lấy posts theo tag
        Task<IEnumerable<Post>> GetPostsByTagIdAsync(Guid tagId, int skip, int take);

        // Lấy posts trong 1 collection
        Task<IEnumerable<Post>> GetPostsByCollectionIdAsync(Guid collectionId, int skip, int take);

        // Lấy newest posts (Explore)
        Task<IEnumerable<Post>> GetLatestPostsAsync(int skip, int take);
    }
}