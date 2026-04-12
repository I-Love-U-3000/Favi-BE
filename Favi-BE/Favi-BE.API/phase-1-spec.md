1) Feed strategy, Redis, consistency
1.1 Feed hiện tại là fan-out write hay fan-out read?
Fan-out on read.
•	GetFeedAsync(Guid, int, int) gọi GetFeedPagedAsync(currentUserId, skip:0, take:500) rồi mới tính score khi request đến (PostService).
•	GetFeedPagedAsync(Guid, int, int) query trực tiếp từ bảng Posts + Follows tại thời điểm đọc (PostRepository).
•	Không có bước materialize timeline theo user khi write post (không có “home_timeline table/queue push”).
=> Mỗi lần đọc feed là assemble + rank lại.
---
1.2 Redis đang dùng cho task nào? TTL?
Hiện tại trong code:
•	Có Redis config trong appsettings*.json (Redis:ConnectionString).
•	Có RedisHealthCheck để ping Redis trong health check.
•	Không thấy đăng ký cache runtime kiểu AddStackExchangeRedisCache, IDistributedCache, ConnectionMultiplexer dùng cho business feed.
•	Không thấy TTL policy cho feed/cache key (vì chưa thấy layer cache đang chạy cho feed).
=> Kết luận thực tế: Redis đang ở mức observability/healthcheck, chưa dùng làm cache nghiệp vụ feed.
---
1.3 Consistency hiện tại trong app
•	DB PostgreSQL + EF Core SaveChangesAsync → strong consistency trong từng transaction mặc định của lần save.
•	Feed read trực tiếp DB → không có cache stale layer cho feed (nên không có eventual do cache).
•	Tuy nhiên có các điểm eventual/không đồng bộ:
•	Vector index: index post chạy fire-and-forget (Task.Run).
•	NSFW check: cập nhật sau create/update.
•	Create post chưa bọc explicit transaction end-to-end với external side effects (upload media) → có rủi ro partial state ở external system.
---
2) API cần test + mẫu request/response + auth
Dưới đây là checklist chính cho api/posts:
A. Feed / Discovery
1.	GET /api/posts/feed — Auth
•	Req: ?page=1&pageSize=20
•	Res: PagedResult<PostResponse>
2.	GET /api/posts/feed-with-reposts — Auth
•	Req: ?page=1&pageSize=20
•	Res: PagedResult<FeedItemDto>
3.	GET /api/posts/guest-feed — No Auth
•	Req: ?page=1&pageSize=20
•	Res: PagedResult<PostResponse>
4.	GET /api/posts/explore — Auth
•	Req: ?page=1&pageSize=20
•	Res: PagedResult<PostResponse>
5.	GET /api/posts/latest — No Auth
•	Req: ?page=1&pageSize=20
•	Res: PagedResult<PostResponse>
6.	GET /api/posts/{id}/related — No Auth (viewer optional)
•	Req: ?page=1&pageSize=20
•	Res: PagedResult<PostResponse>
7.	GET /api/posts/tag/{tagId} — No Auth (viewer optional)
•	Req: ?page=1&pageSize=20
•	Res: PagedResult<PostResponse>
8.	GET /api/posts/profile/{profileId} — No Auth (privacy check)
•	Req: ?page=1&pageSize=20
•	Res: PagedResult<PostResponse>
---
B. Post lifecycle
9.	GET /api/posts/{id} — No Auth (privacy check)
•	Res success: PostResponse
•	Res fail: { code: "POST_NOT_FOUND" | "POST_FORBIDDEN", message: "..." }
10.	POST /api/posts — Auth
•	Req: multipart/form-data (CreatePostRequest + mediaFiles)
•	Res: 201 Created + PostResponse
•	Error: EMPTY_POST, MEDIA_UPLOAD_FAILED, POST_CREATION_FAILED
11.	PUT /api/posts/{id} — Auth
•	Req body: UpdatePostRequest
•	Res: { message: "Đã cập nhật bài viết." } hoặc 403
12.	DELETE /api/posts/{id} — Auth
13.	POST /api/posts/{id}/restore — Auth
14.	DELETE /api/posts/{id}/permanent — Auth
15.	POST /api/posts/{id}/archive — Auth
16.	POST /api/posts/{id}/unarchive — Auth
17.	GET /api/posts/recycle-bin — Auth
18.	GET /api/posts/archived — Auth
---
C. Media / Reaction / Share
19.	POST /api/posts/{id}/media — Auth
•	Req: multipart/form-data files
•	Res: IEnumerable<PostMediaResponse>
20.	POST /api/posts/{id}/reactions?type=Like — Auth
•	Res set: { type: "Like", message: "Reaction đã được cập nhật." }
•	Res unset: { removed: true, message: "Reaction đã được gỡ." }
21.	GET /api/posts/{id}/reactors — Auth
•	Res: IEnumerable<PostReactorResponse>
22.	POST /api/posts/{id}/share — Auth
•	Req body: CreateRepostRequest
•	Res: RepostResponse
23.	DELETE /api/posts/{id}/share — Auth
24.	GET /api/posts/profile/{profileId}/shares — No Auth (viewer optional)
25.	GET /api/posts/shares/{repostId} — No Auth (viewer optional)
---
3) Feed query lấy post như thế nào, từ ai? Pagination gì? Sort gì?
3.1 Lấy post từ ai?
•	Personal feed (/feed): từ chính user + các followee (GetFeedPagedAsync(Guid, int, int)).
•	Feed with reposts: post từ user+followee và repost từ user+followee (GetFeedRepostsAsync(Guid, int, int)).
•	Guest feed: lấy global latest candidates (không theo graph), rồi privacy filter.
•	Explore: global latest candidates cho auth user, rồi privacy + trending filter.
3.2 Pagination hiện tại
Offset pagination (Skip/Take) ở service/repository (Page, PageSize).
3.3 Sort hiện tại
•	/feed: sort theo TrendingScore desc (engagement + decay + velocity).
•	/guest-feed: TrendingScore desc.
•	/explore: TrendingScore desc.
•	/latest: CreatedAt desc.
•	/feed-with-reposts: CreatedAt desc (trộn post/repost theo timestamp).

4) Các table/entity quan trọng sẽ bị chạm khi test feed/post/upload
Scope dưới đây tập trung vào luồng bạn đang load test (feed, guest-feed, Reaction, share, upload media).
Profiles
•	PK: Id
•	Cột chính: Username, DisplayName, Role, PrivacyLevel, FollowPrivacyLevel, IsBanned, LastActiveAt, CreatedAt
•	FK: không trực tiếp trong bảng này
•	Index: theo snapshot hiện tại không thấy unique index cho Username (điểm cần lưu ý)
Follows
•	PK (composite): (FollowerId, FolloweeId)
•	Cột chính: CreatedAt
•	FK:
•	FollowerId -> Profiles(Id)
•	FolloweeId -> Profiles(Id)
•	Index: IX_Follows_FolloweeId
Posts
•	PK: Id
•	Cột chính: ProfileId, Caption, Privacy, CreatedAt, UpdatedAt, DeletedDayExpiredAt, IsArchived, IsNSFW, Location*
•	FK: ProfileId -> Profiles(Id)
•	Index: IX_Posts_ProfileId
•	⚠️ Feed sort nhiều theo thời gian/score nhưng chưa thấy index chuyên cho CreatedAt hoặc partial index cho DeletedDayExpiredAt/IsArchived.
PostMedias
•	PK: Id
•	Cột chính: PostId?, ProfileId?, Url, ThumbnailUrl, Position, PublicId, Width, Height, Format, IsAvatar, IsPoster
•	FK: PostId -> Posts(Id) (nullable)
•	Index: IX_PostMedias_PostId
Comments
•	PK: Id
•	Cột chính: PostId, RepostId?, ProfileId, Content, MediaUrl, ParentCommentId?, CreatedAt, UpdatedAt
•	FK:
•	PostId -> Posts(Id)
•	RepostId -> Reposts(Id)
•	ProfileId -> Profiles(Id)
•	ParentCommentId -> Comments(Id)
•	Index: IX_Comments_PostId, IX_Comments_ProfileId, IX_Comments_ParentCommentId, IX_Comments_RepostId
Reactions
•	PK: Id
•	Cột chính: PostId?, CommentId?, CollectionId?, RepostId?, ProfileId, Type, CreatedAt
•	FK:
•	PostId -> Posts(Id)
•	CommentId -> Comments(Id)
•	CollectionId -> Collections(Id)
•	RepostId -> Reposts(Id)
•	ProfileId -> Profiles(Id)
•	Index:
•	unique (PostId, ProfileId)
•	unique (CommentId, ProfileId)
•	unique (CollectionId, ProfileId)
•	IX_Reactions_RepostId
•	IX_Reactions_ProfileId
Tags
•	PK: Id
•	Cột chính: Name
•	Index: chưa thấy unique index Name ở snapshot
PostTags
•	PK (composite): (PostId, TagId)
•	FK:
•	PostId -> Posts(Id)
•	TagId -> Tags(Id)
•	Index: IX_PostTags_TagId
Reposts
•	PK: Id
•	Cột chính: ProfileId, OriginalPostId, Caption, CreatedAt, UpdatedAt
•	FK:
•	ProfileId -> Profiles(Id)
•	OriginalPostId -> Posts(Id)
•	Index:
•	unique (ProfileId, OriginalPostId) (1 user share 1 post tối đa 1 lần)
•	(ProfileId, CreatedAt)
•	OriginalPostId
Notifications (bị chạm khi reaction/comment/follow)
•	PK: Id
•	Cột chính: Type, RecipientProfileId, ActorProfileId, TargetPostId?, TargetCommentId?, IsRead, CreatedAt
•	FK: recipient/actor/target post/target comment
•	Index: (RecipientProfileId, CreatedAt) + index cho ActorProfileId, TargetPostId, TargetCommentId
---
5) Cache usage (Redis)
Hiện tại
•	Có config Redis (Redis:ConnectionString) + RedisHealthCheck.
•	Không thấy cache business cho feed (AddStackExchangeRedisCache, IDistributedCache dùng trong feed path).
•	Không có TTL feed cache đang chạy.
Nghĩa là gì cho testing
•	Kết quả benchmark hiện tại = baseline no-cache.
•	Rất phù hợp để đo “raw bottleneck” (DB + app CPU + N+1).
Khi implement cache sau này (để so sánh)
Đo lại cùng kịch bản:
•	p50/p95/p99 latency
•	throughput (RPS)
•	DB query count / CPU
•	cache hit ratio
•	consistency lag sau create/reaction/share (đặc biệt trang đầu feed)
---
6) Image upload flow + xử lý backend thêm gì?
Flow hiện tại
1.	API nhận multipart/form-data (Create([FromForm] CreatePostRequest, [FromForm] List<IFormFile>?) hoặc /{id}/media).
2.	Chỉ nhận file có ContentType bắt đầu image/*.
3.	Upload lên Cloudinary qua TryUploadAsync(IFormFile, CancellationToken, string?).
4.	Lưu metadata vào PostMedias (Url, PublicId, Width, Height, Format, Position, ThumbnailUrl).
5.	Cập nhật UpdatedAt.
Tối ưu ảnh hiện có
•	Có Cloudinary transformation:
•	Quality("auto")
•	FetchFormat("auto")
•	Tức là có tối ưu chất lượng/định dạng cơ bản theo Cloudinary.
•	Chưa thấy resize/crop/limit dimension/watermark custom ở backend.
Webhook / background task / vector index
•	Không thấy webhook từ Cloudinary.
•	Vector index: có gọi IndexPostAsync(Post, CancellationToken) theo kiểu fire-and-forget (Task.Run) khi tạo post.
•	Upload media vào post hiện có (UploadMediaAsync(Guid, IEnumerable<IFormFile>, Guid)) chỉ re-check NSFW, không thấy re-index vector ngay tại flow này.
•	NSFW check gọi API đồng bộ sau save; lỗi thì swallow/log.
“1 post nhiều ảnh đúng không?”
•	Đúng. Quan hệ Post (1) - (N) PostMedias.
•	Controller create nhận List<IFormFile>? mediaFiles, và upload media endpoint cũng nhận list files.
7) Vector search in current system
What is it used for?
•	Semantic search (text intent search over posts).
•	Related-post fallback (when tag-based related results are insufficient).
Is it used for recommendation/feed ranking?
•	Not directly in main feed (/api/posts/feed, /guest-feed, /explore currently rank via trending score).
•	It is used in:
•	POST /api/search/semantic (auth required)
•	GET /api/posts/{id}/related (semantic fallback path)
Which APIs/services use vector?
•	Query side:
•	SemanticSearch(SemanticSearchRequest) -> SemanticSearchAsync(SemanticSearchRequest, Guid) -> SearchAsync(SearchRequest)
•	GetRelated(Guid, [FromQuery] int, [FromQuery] int) -> SemanticSearchAsync(SemanticSearchRequest, Guid) (fallback strategy)
•	Index side:
•	On post creation, backend calls VectorIndexService.IndexPostAsync(post) in fire-and-forget mode.
---
8) Current bottleneck
You are right: not measured yet => no proven bottleneck yet.
But from static code, likely hotspots are:
1.	Feed computes score per request over candidate pool (take=500) + per-post reaction fetch.
2.	SemanticSearchAsync(SemanticSearchRequest, Guid) fetches all posts from DB then filters in memory (expensive at scale).
3.	Offset pagination on dynamic feeds (Skip/Take) can degrade and produce unstable pages under heavy writes.
4.	Extra per-item mapping/privacy checks can create N+1 behavior.
So: currently this is risk-based diagnosis, not benchmark-confirmed.
---
9) Docker resource spec readiness (for scale + k6)
Current compose status
•	Services exist (favi-api, postgres, vector-index-api, qdrant, redis).
•	No explicit CPU/RAM limits/reservations in docker-compose.yml.
•	Single instance layout (no replicas/load balancer).
Implication
•	For performance testing, results are noisy because containers can compete for host resources unpredictably.
•	Not yet prepared for meaningful horizontal-scaling experiments.
Horizontal scaling readiness
•	API is JWT-based (good for stateless auth), but:
•	SignalR scaling would need backplane strategy.
•	No orchestration/replica policy in current compose.
•	So: good for functional + baseline load, not true scale-out validation.
---
10) Goal (optimize everything, no deployment)
Use this practical goal stack:
1.	Establish baseline (no-cache) with k6 + DB metrics.
2.	Remove algorithmic hotspots (feed scoring/read path, semantic search DB fetch pattern).
3.	Add cache selectively and compare before/after.
4.	Stabilize pagination for feed consistency under writes.
5.	Re-test with same dataset/scenario and report deltas (p50/p95/p99, RPS, error rate, CPU, memory, DB QPS).
---
11) Real constraints
Cloudinary 20 credits
•	Keep media-heavy test minimal.
•	Reuse existing media URLs where possible.
•	Focus load on read/feed/search/reaction paths, not mass upload loops.
“Lecturer expects what?”
From your project objectives doc, likely expected outcomes are:
•	Functional coverage of core social features.
•	Performance targets (examples listed there): fast API/search response, concurrent-user handling, pagination for large lists.
•	Demonstrable architecture rationale (privacy, auth, moderation, observability, scalability direction).
So for presentation/report: show measured before/after optimization evidence, not just code changes.
