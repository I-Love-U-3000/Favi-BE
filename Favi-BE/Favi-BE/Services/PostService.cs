using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;

namespace Favi_BE.Services
{
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICloudinaryService _cloudinary;

        public PostService(IUnitOfWork uow, ICloudinaryService cloudinary)
        {
            _uow = uow;
            _cloudinary = cloudinary;
        }

        // --------------------------------------------------------------------
        // Queries
        // --------------------------------------------------------------------
        public async Task<PostResponse?> GetByIdAsync(Guid id, Guid? currentUserId)
        {
            var post = await _uow.Posts.GetPostWithAllAsync(id);
            if (post is null) return null;

            // Lấy tags từ repo Tags (hoặc post.PostTags)
            var tags = post.PostTags?.Select(pt => new TagDto(pt.Tag.Id, pt.Tag.Name))
                       ?? Enumerable.Empty<TagDto>();

            // Medias
            var medias = (post.PostMedias ?? new List<PostMedia>())
                .OrderBy(m => m.Position)
                .Select(m => new PostMediaResponse(
                    m.Id,
                    m.PostId,
                    m.Url,
                    PublicId: string.Empty,     // entity hiện không có, để rỗng
                    Width: 0,
                    Height: 0,
                    Format: string.Empty,
                    Position: m.Position,
                    ThumbnailUrl: m.ThumbnailUrl
                )).ToList();

            // Reaction summary
            var summary = await BuildReactionSummaryAsync(post.Id, currentUserId);

            return new PostResponse(
                Id: post.Id,
                AuthorProfileId: post.ProfileId,
                Caption: post.Caption,
                CreatedAt: post.CreatedAt,
                UpdatedAt: post.UpdatedAt,
                PrivacyLevel: post.Privacy,
                Medias: medias,
                Tags: tags,
                Reactions: summary
            );
        }

        public async Task<PagedResult<PostResponse>> GetFeedAsync(Guid currentUserId, int page, int pageSize)
        {
            var skip = Math.Max(0, (page - 1) * pageSize);

            // Feed theo followings (đã chuẩn hoá IPostRepository)
            var posts = await _uow.Posts.GetFeedByFollowingsAsync(currentUserId, skip, pageSize);

            // Map
            var items = new List<PostResponse>(posts.Count());
            foreach (var p in posts)
            {
                items.Add(await MapPostToResponseAsync(p, currentUserId));
            }

            // TODO: nếu muốn có TotalCount thật, thêm IPostRepository.CountFeedByFollowingsAsync
            var total = -1;
            return new PagedResult<PostResponse>(items, page, pageSize, total);
        }

        // --------------------------------------------------------------------
        // Commands: Create/Update/Delete
        // --------------------------------------------------------------------
        public async Task<PostResponse> CreateAsync(Guid authorId, string? caption, IEnumerable<string>? tags)
        {
            var now = DateTime.UtcNow;
            var post = new Post
            {
                Id = Guid.NewGuid(),
                ProfileId = authorId,
                Caption = caption?.Trim(),
                Privacy = PrivacyLevel.Public, // mặc định, có thể đổi theo nhu cầu
                CreatedAt = now,
                UpdatedAt = now
            };

            await _uow.Posts.AddAsync(post);

            // Tags: normalize + tạo liên kết
            if (tags != null && tags.Any())
            {
                var createdTags = await _uow.Tags.GetOrCreateTagsAsync(tags);
                foreach (var t in createdTags)
                {
                    await _uow.PostTags.AddTagToPostAsync(post.Id, t.Id);
                }
            }

            await _uow.CompleteAsync();
            // Trả về bản đầy đủ
            var full = await _uow.Posts.GetPostWithAllAsync(post.Id);
            return await MapPostToResponseAsync(full ?? post, authorId);
        }

        public async Task<bool> UpdateAsync(Guid postId, Guid requesterId, string? caption)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) return false;
            if (post.ProfileId != requesterId) return false;

            if (caption != null)
                post.Caption = caption.Trim();

            post.UpdatedAt = DateTime.UtcNow;

            _uow.Posts.Update(post);
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid postId, Guid requesterId)
        {
            // Lấy post + kiểm tra quyền
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) return false;
            if (post.ProfileId != requesterId) return false;

            // Xoá media trong Cloudinary nếu có (entity không lưu PublicId => bỏ qua cloud delete)
            var medias = await _uow.PostMedia.GetByPostIdAsync(postId);
            _uow.PostMedia.RemoveRange(medias);

            // Xoá post (các join như PostTags, Reactions, Comments nên cascade trong DbContext)
            _uow.Posts.Remove(post);
            await _uow.CompleteAsync();
            return true;
        }

        // --------------------------------------------------------------------
        // Media
        // --------------------------------------------------------------------
        public async Task<IEnumerable<PostMediaResponse>> UploadMediaAsync(Guid postId, IEnumerable<IFormFile> files, Guid requesterId)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) throw new InvalidOperationException("Post not found");
            if (post.ProfileId != requesterId) throw new UnauthorizedAccessException("Not owner");

            var existing = await _uow.PostMedia.GetByPostIdAsync(postId);
            var nextPos = existing.Any() ? existing.Max(m => m.Position) + 1 : 0;

            var outputs = new List<PostMediaResponse>();
            foreach (var f in files)
            {
                var up = await _cloudinary.UploadAsync(f);

                var media = new PostMedia
                {
                    Id = Guid.NewGuid(),
                    PostId = postId,
                    Url = up.Url,
                    ThumbnailUrl = up.ThumbnailUrl,
                    Position = nextPos++
                };

                await _uow.PostMedia.AddAsync(media);

                // Map DTO (đầy đủ thông tin Cloudinary ra ngoài)
                outputs.Add(new PostMediaResponse(
                    Id: media.Id,
                    PostId: media.PostId,
                    Url: media.Url,
                    PublicId: up.PublicId,
                    Width: up.Width,
                    Height: up.Height,
                    Format: up.Format,
                    Position: media.Position,
                    ThumbnailUrl: media.ThumbnailUrl
                ));
            }

            await _uow.CompleteAsync();
            return outputs;
        }

        public async Task<bool> RemoveMediaAsync(Guid postId, Guid mediaId, Guid requesterId)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) return false;
            if (post.ProfileId != requesterId) return false;

            var medias = await _uow.PostMedia.GetByPostIdAsync(postId);
            var media = medias.FirstOrDefault(m => m.Id == mediaId);
            if (media is null) return false;

            // Nếu bạn lưu PublicId ở nơi khác, có thể gọi _cloudinary.DeleteAsync(publicId) tại đây.
            _uow.PostMedia.Remove(media);

            // Re-order position (giữ thứ tự đẹp)
            var remain = medias.Where(m => m.Id != mediaId).OrderBy(m => m.Position).ToList();
            for (int i = 0; i < remain.Count; i++)
            {
                if (remain[i].Position != i)
                {
                    remain[i].Position = i;
                    _uow.PostMedia.Update(remain[i]);
                }
            }

            await _uow.CompleteAsync();
            return true;
        }

        // --------------------------------------------------------------------
        // Tags
        // --------------------------------------------------------------------
        public async Task<IEnumerable<TagDto>> AddTagsAsync(Guid postId, IEnumerable<string> tags, Guid requesterId)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) throw new InvalidOperationException("Post not found");
            if (post.ProfileId != requesterId) throw new UnauthorizedAccessException("Not owner");

            var created = await _uow.Tags.GetOrCreateTagsAsync(tags);
            foreach (var t in created)
                await _uow.PostTags.AddTagToPostAsync(postId, t.Id);

            await _uow.CompleteAsync();

            var result = await _uow.Tags.GetTagsByPostIdAsync(postId);
            return result.Select(t => new TagDto(t.Id, t.Name));
        }

        public async Task<bool> RemoveTagAsync(Guid postId, Guid tagId, Guid requesterId)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) return false;
            if (post.ProfileId != requesterId) return false;

            await _uow.PostTags.RemoveTagFromPostAsync(postId, tagId);
            await _uow.CompleteAsync();
            return true;
        }

        // --------------------------------------------------------------------
        // Reactions
        // --------------------------------------------------------------------
        public async Task<ReactionSummaryDto> GetReactionsAsync(Guid postId, Guid? currentUserId)
        {
            return await BuildReactionSummaryAsync(postId, currentUserId);
        }

        public async Task<ReactionType?> ToggleReactionAsync(Guid postId, Guid userId, ReactionType type)
        {
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null) throw new InvalidOperationException("Post not found");

            var existing = await _uow.Reactions.GetProfileReactionOnPostAsync(userId, postId);

            // Chưa có → thêm
            if (existing is null)
            {
                await _uow.Reactions.AddAsync(new Reaction
                {
                    PostId = postId,
                    ProfileId = userId,
                    Type = type,
                    CreatedAt = DateTime.UtcNow
                });
                await _uow.CompleteAsync();
                return type;
            }

            // Cùng loại → gỡ
            if (existing.Type == type)
            {
                _uow.Reactions.Remove(existing);
                await _uow.CompleteAsync();
                return null;
            }

            // Khác loại → đổi
            existing.Type = type;
            _uow.Reactions.Update(existing);
            await _uow.CompleteAsync();
            return type;
        }

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------
        private async Task<PostResponse> MapPostToResponseAsync(Post post, Guid? currentUserId)
        {
            // Bảo đảm include đủ (nếu repo trả chưa đủ)
            if (post.PostMedias is null)
                post = await _uow.Posts.GetPostWithAllAsync(post.Id) ?? post;

            var medias = (post.PostMedias ?? new List<PostMedia>())
                .OrderBy(m => m.Position)
                .Select(m => new PostMediaResponse(
                    m.Id,
                    m.PostId,
                    m.Url,
                    PublicId: string.Empty, // entity không có
                    Width: 0,
                    Height: 0,
                    Format: string.Empty,
                    Position: m.Position,
                    ThumbnailUrl: m.ThumbnailUrl
                ));

            var tags = (post.PostTags ?? new List<PostTag>())
                .Select(pt => new TagDto(pt.Tag.Id, pt.Tag.Name));

            var summary = await BuildReactionSummaryAsync(post.Id, currentUserId);

            return new PostResponse(
                Id: post.Id,
                AuthorProfileId: post.ProfileId,
                Caption: post.Caption,
                CreatedAt: post.CreatedAt,
                UpdatedAt: post.UpdatedAt,
                PrivacyLevel: post.Privacy,
                Medias: medias,
                Tags: tags,
                Reactions: summary
            );
        }

        private async Task<ReactionSummaryDto> BuildReactionSummaryAsync(Guid postId, Guid? currentUserId)
        {
            var reactions = await _uow.Reactions.GetReactionsByPostIdAsync(postId);

            var byType = reactions
                .GroupBy(r => r.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            var total = byType.Values.Sum();

            ReactionType? mine = null;
            if (currentUserId.HasValue)
            {
                var my = reactions.FirstOrDefault(r => r.ProfileId == currentUserId.Value);
                if (my != null) mine = my.Type;
            }

            return new ReactionSummaryDto(total, byType, mine);
        }
    }
}
