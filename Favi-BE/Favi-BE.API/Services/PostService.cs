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
        private readonly IPrivacyGuard _privacy;
        public PostService(IUnitOfWork uow, ICloudinaryService cloudinary, IPrivacyGuard privacy)
        {
            _uow = uow;
            _cloudinary = cloudinary;
            _privacy = privacy;
        }

        public Task<Post?> GetEntityAsync(Guid id) => _uow.Posts.GetByIdAsync(id);

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
                    m.PostId ?? Guid.Empty,
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

            var (posts, total) = await _uow.Posts.GetFeedPagedAsync(currentUserId, skip, pageSize);

            // Map
            var items = new List<PostResponse>(total);
            foreach (var p in posts)
            {
                items.Add(await MapPostToResponseAsync(p, currentUserId));
            }
            
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
            /*            await _uow.CompleteAsync();*/

            await _uow.CompleteAsync();

            var orphanTags = await _uow.Tags.GetTagsWithNoPostsAsync();
            foreach (var tag in orphanTags)
            {
                _uow.Tags.Remove(tag);
            }
            await _uow.CompleteAsync();

            return true;
        }

        // --------------------------------------------------------------------
        // Media
        // --------------------------------------------------------------------
        // Services/PostService.cs
        public async Task<IEnumerable<PostMediaResponse>> UploadMediaAsync(
            Guid postId,
            IEnumerable<IFormFile> files,
            Guid requesterId)
        {
            // 0) Kiểm tra post & quyền
            var post = await _uow.Posts.GetByIdAsync(postId);
            if (post is null || post.ProfileId != requesterId)
                return Enumerable.Empty<PostMediaResponse>();

            // 1) Chốt danh sách file & lọc ảnh
            var fileList = (files ?? Enumerable.Empty<IFormFile>()).ToList();
            if (fileList.Count == 0) return Enumerable.Empty<PostMediaResponse>();

            // Chỉ nhận image/*, có dữ liệu
            var allowed = fileList
                .Where(f => f?.Length > 0
                         && !string.IsNullOrWhiteSpace(f.ContentType)
                         && f.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (allowed.Count == 0) return Enumerable.Empty<PostMediaResponse>();

            // 2) Tính position bắt đầu = max(position) hiện có + 1
            var existing = await _uow.PostMedia.GetByPostIdAsync(postId);
            var maxPos = existing.Any() ? existing.Max(m => m.Position) : -1;
            var startPos = maxPos + 1;

            var createdMedias = new List<PostMedia>();
            var responses = new List<PostMediaResponse>();

            // 3) Upload tuần tự (đơn giản, ổn định). Có thể tối ưu song song sau.
            for (int i = 0; i < allowed.Count; i++)
            {
                var file = allowed[i];

                // Upload Cloudinary (trả null nếu fail)
                var uploaded = await _cloudinary.TryUploadAsync(file);
                if (uploaded is null) continue; // bỏ qua file hỏng

                var media = new PostMedia
                {
                    Id = Guid.NewGuid(),
                    PostId = postId,
                    Url = uploaded.Url,
                    ThumbnailUrl = uploaded.ThumbnailUrl,
                    Position = startPos + i,   // luôn tăng liên tục
                    PublicId = uploaded.PublicId,
                    Width = uploaded.Width,
                    Height = uploaded.Height,
                    Format = uploaded.Format
                };

                createdMedias.Add(media);

                responses.Add(new PostMediaResponse(
                    media.Id, media.PostId ?? Guid.Empty, media.Url,
                    media.PublicId, media.Width, media.Height,
                    media.Format, media.Position, media.ThumbnailUrl
                ));
            }

            // 4) Không có file nào hợp lệ/đăng thành công → thôi
            if (createdMedias.Count == 0)
                return Enumerable.Empty<PostMediaResponse>();

            // 5) Lưu DB một lần
            await _uow.PostMedia.AddRangeAsync(createdMedias);
            post.UpdatedAt = DateTime.UtcNow;
            _uow.Posts.Update(post);
            await _uow.CompleteAsync();

            // 6) Trả về theo Position tăng dần
            return responses.OrderBy(r => r.Position);
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
            if (post is null) return Enumerable.Empty<TagDto>();  
            if (post.ProfileId != requesterId) return Enumerable.Empty<TagDto>(); 

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
            if (post is null) return null;

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
                    m.PostId ?? Guid.Empty,
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

        public async Task<PagedResult<PostResponse>> GetByProfileAsync(Guid profileId, Guid? viewerId, int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;
            var posts = await _uow.Posts.GetPostsByProfileIdAsync(profileId, skip, pageSize);
            var total = await _uow.Posts.CountAsync(p => p.ProfileId == profileId);

            var result = new List<PostResponse>();
            foreach (var post in posts)
            {
                if (await _privacy.CanViewPostAsync(post, viewerId))
                    result.Add(await MapPostToResponseAsync(post, viewerId));
            }

            return new PagedResult<PostResponse>(result, page, pageSize, total);
        }

        // TODO: Implement these methods
        public Task<PagedResult<PostResponse>> GetExploreAsync(Guid userId, int page, int pageSize)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<PostResponse>> GetLatestAsync(int page, int pageSize)
        {
            throw new NotImplementedException();
        }
    }
}
