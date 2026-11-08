# Native OASIS — lượt mạnh nhất khả thi trên máy hiện tại

## Mục đích và điều kiện

Chạy agent bằng `env.step({agent: LLMAction()})`, native CAMEL memory/tool loop,
recommender TwHIN-BERT và native social tools. Giữ10 initial profiles tự chọn từ
`.tools/oasis-smoke/free-10x4-20261101/free-report.json`, không sinh persona mới,
không bootstrap posts/follows/groups. Mỗi người có state/memory riêng. So sánh với
lượt cũ chỉ mang tính định tính: model, hardware, feed, memory và action loop cùng
thay đổi, không phải kiểm định nhân quả riêng về OASIS.

OASIS0.2.5 pin ba892e0667eb84b312e4c0e5630f275154bbb98f; CAMEL0.2.90 và dependency
lock hiện có. Không sửa package upstream hoặc adapter gốc. Source/runtime files
và harness được hash/archive như các lượt trước.

## Hardware và model

Docker đã xác nhận NVIDIA RTX4060 Laptop8188MiB, driver595.79. Lượt cũ dùng CPU;
lượt này dùng `--gpus all`. Model local qwen3:8b Q4_K_M chính thức (~5.2GB), alias
`favi-oasis8b-native` chỉ thêm context16384 bằng Modelfile. Flash attention và KV
cache q8_0 để giảm VRAM; phải đọc runtime GPU/context thật, không coi có flag là
đã offload thành công. Không dùng API trả phí hoặc embedding OpenAI.

TwHIN-BERT lấy từ Twitter/twhin-bert-base, cache workspace riêng. Preparation ghi
revision và SHA256 từng file. Native run đặt HF_HUB_OFFLINE và tắt implicit token;
model load từ cache pin, không tải code remote. Torch env hiện CPU nên recommender
chạy CPU, generative model chạy GPU. Repo nguồn:
https://huggingface.co/Twitter/twhin-bert-base và https://ollama.com/library/qwen3:8b.

## Native brain và giới hạn

Mỗi agent có OpenAICompatibleModel local riêng, cùng8B, không StubModel cho brain.
SocialAgent kế thừa ChatAgent và giữ native memory; không cắt còn6 messages hoặc
lấy global latest5 bằng SQL cho observation. Backend khai báo token_limit12000
trong context16K, dành khoảng trống cho tools/output. CAMEL native summarization
được giữ; đây là nén memory, không đồng nghĩa tự phản tỉnh về tâm lý.

Tối đa3 model iterations/mỗi opportunity, hỗ trợ nhiều tool calls trong response.
10 người/4 vòng, global cap200 inference requests gồm cả native summaries, output
<=512tokens/request, timeout tổng7200s, timeout step300s. Không hidden SDK retries;
retry_attempts của ChatAgent đặt1. Cap hit hoặc lỗi không được báo như hoàn tất40
opportunities. Không có quota tương tác hoặc diversity rewrite.

Tool boundary kiểm ID thật cho post/comment/group/person và cấm self-follow trước
khi delegate nguyên callable OASIS. Source follow không tự kiểm đủ các invariant
này khi SQLite FK enforcement tắt; guard chỉ trả lỗi, không tự chọn target khác.
Giữ tool name/schema/signature, native brain đọc lỗi và tự quyết tiếp. Ghi riêng
tool-attempts gồm cả native/guard failures, không coi mọi model request là mutation.

Rút function description còn dòng mô tả đầu bằng public FunctionTool schema API;
không thay tên, types, required parameters hoặc callable. Giữ parameter descriptions.
Lưu original/compact schemas để audit token savings và contract.

Mở toàn bộ native social tools, gồm search/refresh/trend, phản ứng lên posts và
comments, follow/mute/unfollow, repost/quote/report và create/join/leave/send/listen
group. Loại purchase_product vì không cấu hình marketplace; interview là research
measurement, không hành động xã hội tự phát. Danh sách thực tế ghi trong report.

Thêm custom tool private_reflection bằng extension tools OASIS hỗ trợ. Agent tự
chọn có dùng không; tool chỉ cập nhật persona/emotion/goal/experience riêng và trả
state cho chính native memory của người đó. Không đăng bài, không sửa người khác,
không phải built-in OASIS psychology. Public directory chỉ gồm public profiles.

## Recommender và thời gian

Platform TwHIN: buffer5, refresh3, following3, allow_self_rating=False,
use_openai_embedding=False. Native env.step cập nhật rec trước mỗi cơ hội; agent
có thể chủ động refresh/search/trend. Ghi rec rows, thời gian update và lỗi để phân
biệt “đã bật” với “đã hoạt động”. Nếu source nuốt lỗi recommender, phải ghi nhận;
không thay bằng feed SQL mà vẫn gọi là native. Không bật đồng thời Reddit hot
ranking như một feed khác; native trend là tool khám phá riêng.

Twitter/TwHIN lưu timestamps dạng logical tick (signup0, tăng sau env.step), không
phải ISO UTC của mode Reddit. Giữ raw nguyên vẹn. Projection ánh xạ tick vào UTC
origin ghi lúc bắt đầu, 1s/tick, áp dụng nhất quán toàn bộ core created_at. Không
điền timestamp thiếu/sai, không UPDATE SQLite. Tick là thứ tự logic, không phải
ngày hoặc elapsed time đo ngoài đời.

## Raw world và Favi projection

Giữ oasis.db và dump mọi table, toàn bộ model requests/responses, tool results,
native memories, private reflection và rec diagnostics. Audit mọi declared FK
bằng logical anti-join, thêm group_members.agent_id còn thiếu FK. Known DDL bugs
ở pin: like/dislike/rec trỏ tweet thay vì post; group_messages.sender_id trỏ
user.agent_id không UNIQUE. Ghi physical errors và aliases, không nói physical
FK toàn hệ thống đạt. Runtime xác nhận user_id=agent_id, unique trong lượt này.

Projection chỉ nhập users, original posts, likes/comments trên original posts và
follows. Repost/quote và engagement của chúng được liệt kê IDs excluded; groups,
messages, dislike, comment reactions, mute/report và các counters tương ứng chỉ
ở raw world, chưa map đầy đủ sang Favi. Không gọi projection là import toàn bộ
OASIS. Không biến dislike thành like hoặc group message thành comment.

Trước exclusions: kiểm raw post/repost/engagement references và temporal ordering
core. Sau mapping: dùng adapter.normalize nguyên bản để kiểm full constraints của
slice. Xuất snapshot/SQL/manifest bằng staging rồi publish; source SHA không đổi.
Manifest ghi mọi exclusion/count và clock mapping. Media là URL metadata safe
explicit, chưa kiểm bytes/moderation. API gate chỉ chứng minh projected subset.

Unit tests kiểm non-mutating projection, dangling reference không bị giấu bởi
filter, clock mapping, temporal ordering và tool contract/private isolation.
Kiểm chứng Favi bằng verify.ps1 -ExistingRunDirectory sau khi engine.close.

## Thực thi

Chuẩn bị bằng prepare_native_recsys.py với cache `.tools/oasis-native-hf-cache`,
Ollama GPU container riêng bind loopback, cache `.tools/ollama-models`; create
alias từ native-8b.Modelfile. Runner run_oasis_native.py ghi run-config/harness.
Pilot nhỏ dùng cùng protocol để kiểm tool chain/GPU/recsys trước lượt10x4.
Pilot không phải dữ liệu được chèn vào lượt chính. Không thêm manual action để
đảm bảo có bài hoặc group. Dừng đúng container/Compose của tác vụ sau test.

Số tool requests của model tách khỏi automatic refresh/listen/signup và source
trace counts. Native failures/no-tool replies đều được giữ, không coi env.step
return bình thường là mọi tool thành công. Báo cáo diversity/reciprocity dựa raw
data, không filter hoặc sửa content. Không merge/deploy tự động.

`review_native.py <run-root>` sinh review.md gồm toàn bộ bài/comment/group và
interaction-metrics.json: số tác giả comment, cặp tác giả/người được phản hồi,
follow hai chiều và lý do tool failure. Đây là thống kê mô tả, chưa phải thước đo
đã calibration về hành vi người thật. Dùng `python -X utf8` khi chạy trên Windows
để logger upstream ghi được emoji; raw JSON luôn dùng UTF-8 độc lập logger.

## Kết quả thực thi 01/11/2026

Lượt chính `.tools/oasis-smoke/native-full-10x4-20261101` hoàn tất40/40
opportunities trong khoảng15phút37giây, tính từ UTC origin đến engine.close.
GPU status xác nhận size_vram=size=6.278.736.772 bytes, context16384. TwHIN
revision82ac392ce81f94560c391311ee2ddd024c5ac1fc, probe shape[1,12,768].
Model generation chạy GPU; BERT recommender chạy CPU. World không bootstrap dữ liệu.

Raw:10 users,41 post rows (35 original,4 repost,2 quote),9 likes,3 follows,
30 comments,1 group/10 members/7 messages;2 private reflections do actor0 và3 tự
chọn. Không mute/dislike/report/comment reactions thành công. 147 trace rows bao
gồm automatic actions; không coi đây là147 quyết định độc lập của model.

148 inference requests:120 decisions +28 native context summaries. Prompt865.206
tokens, completion15.375, tổng880.581 local tokens, không trả phí provider.
Decision requests dùng756.984 tokens, summaries123.597. Tổng cao gấp khoảng15lần
baseline58.679 tokens; đồng thời nhiều tool opportunities hơn, không so chi phí
như cùng protocol. Max3 iterations đã được dùng ở cả40opportunities. Không lỗi
inference hoặc native turn exception;18 tool requests bị từ chối đúng luật:
14 tự like comment,3 tự like post,1 join group đã tham gia. Không sửa target hoặc
retry để ép thành công. Không có follow hai chiều.

Toàn bộ10actors đều comment;11posts được comment,17 cặp comment author→post
author. 30comments chỉ có18unique texts, tức12duplicate rows. 35original posts có
25unique texts, tức10duplicate rows. Raw post duplicate metric15 còn tính blank
repost text và quote; dùng metric original-only khi đánh giá chất lượng sáng tác.
Nhiều lời tự giới thiệu/tán đồng và chủ đề urban gardening vẫn lặp. Persona đầu
vào đã thiên gardening/urban tech; không thể quy toàn bộ sự hội tụ này cho OASIS.
Native summary dùng mẫu tổng kết công việc của CAMEL, chưa chuyên biệt social
memory. Không có bằng chứng tâm lý/xã hội đã calibration hoặc cải thiện nhân quả
chỉ nhờ framework.

TwHIN update40lần: lượt đầu thế giới trống log `list index out of range`, được
upstream catch;39lượt có posts còn lại ghi rec rows hợp lệ, khi >5posts luôn50rows
(5/user). Không thêm fallback feed. Đây là known empty-world bug, chưa sửa source.
Windows cp1252 logger có UnicodeEncodeError với emoji; calls/tool results/raw DB
vẫn đầy đủ UTF-8, không dùng native text log làm nguồn duy nhất. BERT báo pooler
weights không có trong checkpoint và được khởi tạo mới. Source
process_recsys_posts.process_batch thực tế dùng outputs.pooler_output, vì vậy
native recommender có chạy nhưng ranking dựa một pooling head chưa được train
trong checkpoint này. Không coi rec rows hợp lệ là bằng chứng semantic ranking
tốt. Cần một lượt riêng sửa/đánh giá pooling và pin source mới trước khi kết luận
chất lượng feed; lượt này giữ upstream nguyên bản để audit.

Logical FK anti-join mọi table đạt0orphans; physical FK DDL defects ở trên vẫn
còn. Source hashf1b839e74b7218418c14388032a38aeea713b2a768c585c02ac0a169138c2e58
không đổi qua projection. Favi import10users/35posts/9likes/3follows/30comments;
6repost/quote và groups/messages chỉ giữ raw, explicit manifest khai báo đầy đủ.

`verify.ps1 -ExistingRunDirectory` tạo integration run
`.tools/oasis-smoke/bb9b70297eec4148add51c9a57501f6f`:50unit tests (12adapter,
38smoke) và551 PostgreSQL/import/HTTP checks đạt, gồm replay unchanged, injected
FK rollback, profile/post/media/reaction/comment/follower và2feed endpoints qua
Kestrel loopback thật. Không qua frontend/NGINX/SignalR/load test; media chỉ là
metadata. Build vẫn có warning NU1903 SignalR.Common9.0.11 hiện hữu, chưa phải
production sign-off. Compose riêng được teardown sau kiểm chứng.

Evidence chính: native-report.json, review.md, interaction-metrics.json,
source-validation.json, gpu-model-status.json, calls.json, turns.json và
export/projection-manifest.json. Không chỉnh content hoặc loại duplicate để cải
thiện số liệu. Báo cáo này chứng minh native integration chạy được và tương tác
rộng hơn baseline trong lượt đã quan sát; chất lượng chưa đạt xã hội tự nhiên.
