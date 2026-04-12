Favi Final Benchmark Spec
1) Mục tiêu

Bạn không benchmark “production scale”. Bạn benchmark:

Baseline ổn định trước khi tối ưu.
Cùng một dataset, cùng một môi trường, cùng một kịch bản, để so trước/sau.
Phát hiện bottleneck thật của code hiện tại: feed read path, post create path, media upload path, reaction path, related/semantic fallback.

Mục tiêu đo là:

latency P50/P95/P99
error rate
throughput
DB query behavior
cache behavior hiện tại là “no business cache”
chi phí từ media upload và vector index side effects, nhưng không để nó làm nhiễu baseline chính.
2) Fixed environment contract
2.1 Docker resource lock

Source of truth: `docker-compose.resource-allocation`.

Block dưới đây chỉ là bản mirror để tham chiếu nhanh; khi benchmark phải ưu tiên giá trị trong `docker-compose.resource-allocation`:

services:
  favi-api:
    cpus: 2
    mem_limit: 2.5g

  favi-postgres:
    cpus: 2
    mem_limit: 2.5g

  vector-index-api:
    cpus: 1.5
    mem_limit: 1g

  qdrant:
    cpus: 0.75
    mem_limit: 1g

  redis:
    cpus: 0.5
    mem_limit: 512m

Ý nghĩa:

favi-api và favi-postgres là hai service cần ưu tiên nhất.
redis hiện chỉ là cực nhỏ, đủ để giữ footprint thấp.
vector-index-api và qdrant phải tồn tại, nhưng không được kéo trọng tâm benchmark chính sang vector search.
2.2 Không đổi giữa các lần chạy

Không được đổi:

Docker resource limits
image tag
env config
seed key
dataset
k6 script
runtime flags

Nếu đổi một trong các thứ trên, bạn phá baseline.

Lưu ý bắt buộc: benchmark chỉ dùng resource limit từ `docker-compose.resource-allocation` (không tự sửa tay ở file compose khác trong lúc đo).

3) Final seed scope

Đây là mức humble nhưng đủ lực.

3.1 Core counts
Users: 5,000
Posts: 10,000–12,000
Follows: 50,000–70,000 edges
Likes: 80,000–120,000
Comments: 15,000–30,000
Reposts/Shares: 1,000–2,000
Tags: 50–120
Image catalog: 1000+ URLs (đa dạng)
Run image set: tập con freeze theo `SEED_KEY`, không đổi giữa before/after
Vectorized posts: 3,000–5,000 posts, không cần toàn bộ dataset

3.1.1 Image source policy (chốt)
- Dùng `https://loremflickr.com` với công thức deterministic: `/{width}/{height}/{tag}?lock={n}`.
- `tag` = chủ đề ảnh (du lịch, đồ ăn, thể thao, ...), `lock` cố định để URL ổn định giữa các lần chạy.
- Không dùng pool 20 ảnh cố định nữa. Dùng mô hình 2 lớp:
  1) `image-catalog.json`: catalog ảnh lớn (khuyến nghị 1000+ URL) tạo 1 lần, lưu lại.
  2) `run-image-set.json`: tập con deterministic chọn từ catalog theo `SEED_KEY`, freeze cho 1 vòng benchmark.
- Dùng URL từ `run-image-set.json` để gán `PostMedias.Url` trong DB seed và cho seed vector DB.
- Không phụ thuộc Cloudinary URL cho benchmark dataset.
3.2 Why this size
Đủ lớn để feed query không còn trivial.
Đủ nhỏ để laptop và Docker stack vẫn chạy ổn.
Đủ skew để có post “hot”, user “active”, post “cold”, follow graph không đều.
Đủ để so trước/sau tối ưu mà không bị noise từ dataset quá lớn.
4) Data shape and distribution
4.1 User distribution

Chia user theo role:

70% lurker: không hoặc gần như không tạo post
25% casual: 1–5 post
5% power users: 8–20 post

Không seed user đều nhau. Nếu đều, kết quả feed và engagement không còn phản ánh social pattern thật. Spec của bạn đã xác nhận app đang dùng social graph + trending/ranking, nên skew là bắt buộc.

4.2 Follow graph

Follow graph phải có tính lệch:

đa số user follow ít
một số user có rất nhiều follower
phân bố theo kiểu heavy-tail, nhưng ở quy mô đồ án thì không cần mô phỏng cực đoan

Gợi ý đơn giản:

mỗi user follow 0–20 người
average 10–14 followee/user
1–2% user có follower count vượt xa phần còn lại

Vì feed là fan-out on read, follow graph là dữ liệu trọng yếu nhất cho benchmark feed.

4.3 Post distribution

Post phải phân bổ kiểu:

60–70% posts chỉ có 1 ảnh và engagement thấp
20–30% có engagement trung bình
5–10% là posts “hot” để tạo áp lực lên ranking / feed / reaction path

Mọi post phải có ảnh.
Nhưng không upload thật từng post trong benchmark. Dùng `run-image-set` được khóa sẵn từ catalog để đa dạng ảnh mà vẫn repeatable.

4.4 Reaction distribution
80% posts: < 10 likes
15% posts: 10–50 likes
5% posts: 50–150 likes

Comments:

phần lớn posts không có comment
số ít post có thread sâu hơn
đừng cho mọi post cùng số comment, vì như vậy feed load không có skew thật
4.5 Repost/share distribution

Vì spec hiện tại có:

share endpoint
feed-with-reposts
profile shares endpoints

Nên seed một lượng repost đủ để:

feed-with-reposts có dữ liệu
test query trộn post/repost
không quá nhiều để làm nhiễu baseline
4.6 Tags and related content
mỗi post gắn 1–3 tags
tags được reuse
một số tag phổ biến hơn tag khác
related endpoint có đủ data để fallback semantic hoặc tag-based logic hoạt động
5) Seed pipeline: thứ tự thực hiện

Đây là phần bạn có thể giao AI code từng bước.

Step 0 — Constants & Seed Context 
Files
seed/config.cs
seed/SeedContext.cs
seed/seed-manifest.json
Nội dung
SEED_KEY (string cố định, ví dụ: "favi_v1")
counts:
users: 5000
posts: 10000–12000
follows: 50000–70000
reactions/comments/reposts ranges
role ratios:
lurker / casual / power
distribution configs:
like distribution
comment distribution
image catalog + run image set (loremflickr, deterministic)
output paths:
seed-output/
SeedContext (bắt buộc có)
public class SeedContext
{
    public string SeedKey { get; }
    public Random Random { get; }

    public SeedContext(string seedKey)
    {
        SeedKey = seedKey;
        // Không dùng string.GetHashCode() cho benchmark deterministic.
        // Dùng stable hash/int seed tự tính từ seedKey.
        Random = new Random(StableSeed.FromString(seedKey));
    }
}
Mục tiêu
toàn bộ seed phải deterministic
không có random “trôi nổi”
mọi step dùng chung context
Step 1 — Seed Users / Profiles 
Files
seed/steps/SeedUsers.cs
seed-output/users.csv (export sau)
Columns (DB + export)
profile_id
username
display_name
email
password
role
activity_role
avatar_url
cover_url
privacy_level
follow_privacy_level
is_banned
created_at
last_active_at
Rules
5,000 users
username/email unique
password fixed (vd: "123456")
deterministic theo SeedContext
timestamp spread 30–90 ngày
avatar_url và cover_url bắt buộc non-null, deterministic (loremflickr lock)
role account phải vary (ít nhất có `Admin`, `Moderator`, `User`)
Distribution
70% lurker
25% casual
5% power

Account role mapping (đề xuất deterministic)
- 1 tài khoản `Admin` (index đầu tiên)
- một tập nhỏ `Moderator` lấy từ nhóm active (`power` + rất ít `casual`)
- còn lại `User`
Output DB
insert vào Profiles + EmailAccounts
Validation (NGAY SAU STEP)
duplicate email = fail
duplicate username = fail
count != expected = fail
Step 2 — Seed Social Graph (Follows) 
Files
seed/steps/SeedFollows.cs
seed-output/follows.csv
Columns
follower_id
followee_id
created_at
Rules
không self-follow
không duplicate edge
skewed distribution:
mỗi user follow 0–20
average ~12
1–2% user có follower cao
Critical Logic
ưu tiên follow user đã có nhiều follower (preferential attachment)
Output DB
insert vào Follows
Validation
self-follow = fail
duplicate edge = fail
graph rỗng = fail
Step 3 — Seed Posts + Media 
Files
seed/steps/SeedPosts.cs
seed/steps/SeedMedias.cs

seed-output/posts.csv
seed-output/post-medias.csv
Posts columns
post_id
profile_id
caption
privacy
created_at
updated_at
is_archived
is_nsfw
location
Media columns
media_id
post_id
profile_id
url
thumbnail_url
position
public_id
width
height
format
is_avatar
is_poster
Rules
10k–12k posts
mỗi post = đúng 1 media (baseline)
media lấy từ `run-image-set.json` (được sinh deterministic từ `image-catalog.json`)
không upload ảnh trong benchmark
Critical
100% posts phải có media
Output DB
insert Posts
insert PostMedias
Validation
post không có media = FAIL NGAY
media url null = FAIL
FK sai = FAIL
Step 4 — Seed Engagement 
Files
seed/steps/SeedReactions.cs
seed/steps/SeedComments.cs
seed/steps/SeedReposts.cs

seed-output/reactions.csv
seed-output/comments.csv
seed-output/reposts.csv
Reactions

Columns:

reaction_id
post_id
profile_id
type
created_at

Rules:
- multi-target reaction (post/comment/repost), nhưng mỗi row chỉ target đúng 1 entity
- unique theo từng target:
  - (post_id, profile_id)
  - (comment_id, profile_id)
  - (repost_id, profile_id)
- target mix chuẩn MXH (deterministic):
  - post ~72%
  - comment ~20%
  - repost ~8%
- skew hot/cold theo heavy-tail (Zipf-like)
Comments

Columns:

comment_id
post_id
profile_id
parent_comment_id
content
media_url
created_at
updated_at

Rules:
- tree depth 1–2 (parent + child), không tạo reply của reply
- không orphan
- có tỷ lệ comment chứa URL (content có http/https)
- reply rate có kiểm soát (khoảng 25–35%)
Reposts

Columns:

repost_id
profile_id
original_post_id
caption
created_at
updated_at

Rules:

unique (profile_id, original_post_id)
Validation
duplicate reaction theo target = FAIL
reaction có 0 hoặc >1 target = FAIL
orphan comment = FAIL
comment depth > 2 = FAIL
không có reply comment = FAIL
không có comment chứa URL = FAIL
không có reaction cho comment = FAIL
repost duplicate = FAIL
Step 5 — Seed Tags & PostTags 
Files
seed/steps/SeedTags.cs

seed-output/tags.csv
seed-output/post-tags.csv
Rules
50–120 tags
mỗi post: 1–3 tags
skew popularity (không đều)
tag name unique
Output DB
insert Tags
insert PostTags
Validation
duplicate tag = FAIL
post không có tag = WARNING (không fail)
Step 6 — Seed Lightweight Notifications 
Files
seed/steps/SeedNotifications.cs
(optional) seed-output/notifications.csv
Scope
seed ít
không phải benchmark target
chỉ để giữ side-effect hợp lệ
Rules
generate từ reaction/comment/follow
không cần full coverage
Step 7 — Validation Gate (MỚI - BẮT BUỘC)
Files
seed/SeedValidator.cs
Must-have checks
- users unique
- follows valid
- posts have media (100%)
- reactions unique
- comments tree valid
- repost unique
- tags valid
- counts match expected ranges
Behavior
FAIL → STOP PIPELINE
PASS → tiếp tục export

👉 Đây là thứ spec cũ của bạn thiếu hoàn toàn.

Step 8 — Export Dataset 
Files
seed/SeedExport.cs

output:
seed-output/
Output
users.csv
follows.csv
posts.csv
post-medias.csv
reactions.csv
comments.csv
reposts.csv
tags.csv
post-tags.csv
tokens.csv
image-catalog.json
run-image-set.json
seed-manifest.json
login mỗi user 1 lần
export token
k6 reuse
seed-manifest.json (content example)
{
  "seedKey": "favi_v1",
  "users": 5000,
  "posts": 12000,
  "generatedAt": "timestamp"
}
Critical rule

👉 k6 CHỈ đọc từ đây
👉 k6 KHÔNG tạo data

Step 9 — Optional Snapshot 
Option
pg_dump > seed.sql
Purpose
restore nhanh
debug
6) Seed output artifacts

Sau seed, phải có các file này:

seed-output/
  users.csv
  follows.csv
  posts.csv
  post-medias.csv
  reactions.csv
  comments.csv
  reposts.csv
  tags.csv
  post-tags.csv
  tokens.csv
  image-catalog.json
  run-image-set.json
  seed-manifest.json

Nếu thiếu seed-manifest.json, sau này bạn khó chứng minh benchmark là repeatable.

7) Seed generation algorithm

Đây là level đủ để code luôn.

7.1 Users
Sinh N=5000.
Gán role theo tỉ lệ đã chốt.
Gán username/email unique.
Gán created_at rải đều.
Gán last_active_at dựa vào role:
lurker: xa hơn
casual: trung bình
power: gần hiện tại hơn
7.2 Follows
Với mỗi user, chọn số followee theo skew.
Ưu tiên follow users có degree cao hơn.
Chống duplicate/self-follow.
Ghi CSV.
7.3 Posts
Tính số post cho từng user theo role.
Sinh caption ngắn hoặc trung bình.
Gán 1 ảnh từ `run-image-set.json` đã freeze.
Gán privacy hợp lệ.
Gán created_at rải trong khoảng thời gian gần đây.
7.4 Engagement
Chọn post hot / cold.
Gán likes theo distribution.
Gán comments theo distribution.
Gán reposts ít hơn likes/comments.
Ghi consistent foreign keys.
7.5 Tags and related
Mỗi post gắn 1–3 tag.
Tag phổ biến hơn phải xuất hiện nhiều hơn.
Related test sẽ có candidate đủ tốt.
8) Test strategy: k6 suite

Spec của bạn xác nhận các path chính cần test là:

feed / guest-feed / feed-with-reposts / explore / latest / profile
post detail / create / update / delete / archive
media upload
reactions
share
semantic search / related fallback
File tree
k6/
  lib/
    auth.js
    data.js
    http.js
    metrics.js
  scenarios/
    smoke.js
    feed_baseline.js
    mixed_workload.js
    write_stress.js
    media_upload.js
    semantic_search.js
    related_fallback.js
    hotspot.js
    consistency.js
  config/
    baseline.js
    thresholds.js
  data/
    users.csv
    posts.csv
    tokens.csv
    run-image-set.json
9) Scenario definitions
9.1 smoke.js

Mục tiêu: biết hệ thống còn sống.

Flow

get one token
GET feed
GET post detail
POST reaction nhẹ hoặc create post test

Config

1–3 VU
30–60s

Fail if

5xx
auth fail
unexpected response shape
9.2 feed_baseline.js

Mục tiêu: baseline read path.

Flow

reuse token
GET /api/posts/feed?page=1&pageSize=20
GET /api/posts/feed-with-reposts?page=1&pageSize=20
GET /api/posts/guest-feed?page=1&pageSize=20
GET /api/posts/explore?page=1&pageSize=20
GET /api/posts/latest?page=1&pageSize=20
GET /api/posts/profile/{profileId}?page=1&pageSize=20

Config

40–60 VU
2–3 phút

Why

feed là fan-out on read và query trực tiếp từ DB, nên đây là path quan trọng nhất.
9.3 mixed_workload.js

Mục tiêu: mô phỏng user thật.

Mix

70% feed read
20% reaction/comment
10% create post

Config

60–80 VU
3 phút

Post create rule

API create hiện tại nhận `multipart/form-data` với `IFormFile`.
Vì vậy:
- baseline read benchmark: không ép create-with-upload trong loop chính
- write/media benchmark: chỉ dùng file pool tải sẵn từ `run-image-set.json`, rồi reuse
- không upload ảnh ngẫu nhiên ngoài pool
9.4 write_stress.js

Mục tiêu: write path.

Flow

create post
add media
react
comment

Config

20–40 VU
1–2 phút

Why

đảm bảo save path, PostMedias, reaction, notification side effects hoạt động bình thường.
9.5 media_upload.js

Mục tiêu: kiểm tra riêng upload media.

Flow

POST /api/posts
POST /api/posts/{id}/media

Config

5–10 VU
ngắn

Lưu ý

chỉ dùng với file pool đã freeze từ `run-image-set.json`
test riêng, không trộn với feed baseline
9.6 semantic_search.js

Mục tiêu: test vector path riêng.

Flow

POST /api/search/semantic
GET /api/posts/{id}/related

Config

10–20 VU
ngắn

Why

semantic search trong spec có fetch/filter nặng hơn, nên tách riêng để không làm mờ feed benchmark.
9.7 hotspot.js

Mục tiêu: viral-content pressure.

Flow

tất cả VU nhắm vào cùng 1 post hot
GET post detail
reaction/comment cùng một entity

Config

30–50 VU
1–2 phút
9.8 consistency.js

Mục tiêu: đọc lại ngay sau write.

Flow

create post
read post ngay
create reaction
đọc lại feed/post
verify post exists và trạng thái hợp lệ

Config

10–15 VU
ngắn

Why

app có side effects async như vector index và NSFW update, nên consistency test phải tách ra để kiểm tra read-after-write.
10) k6 pass/fail thresholds
Global thresholds
export const thresholds = {
  http_req_failed: ['rate<0.01'],
  http_req_duration: ['p(95)<500'],
};
Scenario-specific thresholds
feed_baseline: p95 < 300ms
mixed_workload: p95 < 450ms
write_stress: p95 < 600ms
semantic_search: p95 < 800ms
hotspot: p95 < 800ms

Không nên set threshold quá gắt ngay từ đầu. Bạn đang làm đồ án, không phải load test production.

11) Test data mapping
k6 reads from
users.csv
tokens.csv
posts.csv
run-image-set.json
Runtime behavior
chọn user theo round-robin hoặc seeded random
chọn post theo role / hotness bucket
chọn image URL từ run-image-set đã freeze
không tạo dữ liệu ngẫu nhiên ngoài seed
Không được
random user mới mỗi request
random image upload mỗi request
random auth login mỗi iteration
12) Benchmark workflow

Đây là trình tự chuẩn để bạn chứng minh before/after.

Run 0 — Seed
chạy seed pipeline
export snapshot
verify referential integrity
Run 1 — Warm-up
smoke test 30–60s
mục tiêu: JIT, connection pool, caches nóng lên
Run 2 — Baseline
chạy feed_baseline.js
chạy mixed_workload.js
chạy write_stress.js
lưu toàn bộ output JSON + logs
Run 3 — Optimize one thing

Chỉ sửa một nhóm tối ưu:

query
pagination
indexing
mapping
N+1
cache strategy sau này
Run 4 — Re-run same suite
cùng seed
cùng data
cùng env
cùng thresholds
Run 5 — Compare

So:

P50/P95/P99

13) Documentation strategy (full process, không chỉ Step 1–2)

Để giữ trace đầy đủ cho toàn bộ pipeline, dùng một thư mục chung:

`Favi-BE.API/Docs/seed-pipeline/`

Mỗi step trong mục 5) phải có tài liệu tương ứng, tối thiểu gồm:

- mục tiêu
- input/output
- rule bắt buộc
- distribution model (nếu có)
- validation gate
- definition of done

Danh sách tài liệu chuẩn cho full process:

- `step-00-seed-constants-and-context.md`
- `step-01-seed-users-profiles.md`
- `step-02-seed-social-graph-follows.md`
- `step-03-seed-posts-medias.md`
- `step-04-seed-engagement.md`
- `step-05-seed-tags-posttags.md`
- `step-06-seed-notifications.md`
- `step-07-seed-validation-gate.md`
- `step-08-seed-export-dataset.md`
- `step-09-seed-optional-snapshot.md`

Ghi chú trạng thái hiện tại:

- Đã có: `step-01-seed-users-profiles.md`, `step-02-seed-social-graph-follows.md`
- Còn thiếu: các doc step còn lại trong danh sách trên

Nguyên tắc bắt buộc:

- Step 2 phải nêu rõ mô hình skewed (`Zipf` hoặc `90-9-1`) + preferential attachment.
- Validator là bắt buộc cho toàn pipeline: fail validation thì stop.
- Không ghi tài liệu lệch với resource source-of-truth (`docker-compose.resource-allocation`).

14) Sequential coding tasks for AI

Đây là phần bạn cần nhất để giao việc theo từng chặng.

Task 1 — Seed constants and manifest

AI code:

seed-manifest.json
config file
image-catalog + run-image-set config
Task 2 — User/profile seeder

AI code:

generate users
export users.csv
deterministic IDs/passwords/timestamps
Task 3 — Follows seeder

AI code:

generate skewed follow graph
export follows.csv
prevent self-follow and duplicates
Task 4 — Posts + medias seeder

AI code:

generate posts
attach exactly 1 image/post
export posts.csv and post-medias.csv
Task 5 — Reactions/comments/reposts/tags seeder

AI code:

reaction skew
comment tree shallow
reposts
tags and post-tags
Task 6 — Auth bootstrap

AI code:

login each seeded user once
export tokens.csv
Task 7 — k6 shared libraries

AI code:

auth helper
csv loader
seeded random selector
response validator
Task 8 — feed_baseline.js

AI code:

feed / guest feed / latest / profile / feed-with-reposts
Task 9 — mixed_workload.js

AI code:

weighted behavior mix
think time
token reuse
Task 10 — write_stress.js

AI code:

create post
media attach
reaction/comment
Task 11 — semantic_search.js and related_fallback.js

AI code:

vector/search-only benchmark
Task 12 — reporting script

AI code:

parse k6 outputs
compare before/after
generate summary JSON or Markdown
15) Final acceptance criteria for the whole setup

Bạn xem setup này là đúng nếu:

Seed chạy lại cùng key thì dữ liệu giống nhau.
Resource limit luôn lấy từ `docker-compose.resource-allocation` và không đổi giữa before/after.
Mọi post đều có ảnh nhưng không phụ thuộc upload thật trong benchmark.
Image dataset dùng `loremflickr` URL deterministic (size/tag/lock cố định) cho DB + vector seed.
Catalog ảnh đa dạng nhưng run-image-set phải freeze và không đổi giữa before/after.
Feed baseline không trộn vector search.
K6 scripts không thay đổi giữa before/after.
Benchmark có thể giải thích được vì sao nhanh/chậm hơn, không chỉ đọc số latency.
16) Một câu chốt để bạn giữ đúng hướng

Baseline này không nhằm “đo xem hệ thống chịu được bao nhiêu user”.
Nó nhằm chứng minh:

cùng dữ liệu, cùng môi trường, cùng kịch bản, sau khi sửa code thì hệ thống tốt hơn một cách đo được.

Đó mới là thứ đồ án cần.