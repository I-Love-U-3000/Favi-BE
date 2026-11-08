# OASIS engine thật → Favi — 31/10/2026

## Bổ sung sau review Terra High

**Kết quả retest mới nhất: PASS**, exit 0. Có 12 adapter tests, 7 provenance
regression tests và 48 PostgreSQL/import/HTTP checks. Hai ca Docker isolation
(Python thiếu, port database/cache bận) giữ nguyên container IDs của sentinel.
Full gate chạy trong lúc listener khác giữ port API cũ 18191; listener đó không
nhận kết nối. Kestrel của lượt pass thực dùng `http://127.0.0.1:57924/`.

Evidence mới: `.tools/oasis-smoke/670c00a6ebf64fa9af623d583ccbf63a/`, project
`favi-oasis-670c00a6ebf64fa9af623d583ccbf63a`; run.json/provenance/checks đều ghi
đúng ownership, 42 file source đã xác thực và 130 dependency versions khớp lock.
Snapshot namespace `oasis-engine-manual-670c00a6ebf64fa9af623d583ccbf63a`.
Terra re-review các fix isolation/provenance/port không còn thấy blocker; Terra
không tự chạy lại tests. Runner/sentinel containers đã cleanup, không còn container
Compose test. Các evidence ID/port ở phần lịch sử phía dưới thuộc lượt trước review.

Tái lập regression bằng CLI ngoài sandbox:

```powershell
./tools/oasis-smoke/test-isolation.ps1
./tools/oasis-smoke/test-api-isolation.ps1
```

Phạm vi sửa: harness/provenance/tài liệu; giữ nguyên adapter và package upstream.
Các lỗi DDL upstream ở mục riêng vẫn được báo rõ, không âm thầm patch source.

Đã sửa runner dùng Compose project cố định/cleanup vô điều kiện: project mỗi run
`favi-oasis-<GUID>`, kiểm tra port trước up và chỉ down project do run đó thử tạo.
Report riêng `run.json` lưu project/cleanup ownership, không dựa vào latest.json
cho nhiều lượt đồng thời. Port cố định nên lượt sau fail nếu đang bận, không dùng
chung stack. Có regression Docker thật giữ nguyên container sentinel khi Python
thiếu hoặc port bận; `test-isolation.ps1` từ chối chạm project legacy đã tồn tại.

Provenance trước import đối chiếu toàn bộ Python/SQL của package thực thi với Git
blobs commit pin, bỏ khác biệt CRLF/LF; checkout phải sạch, file set phải khớp và
version distribution đúng 0.2.5. Bảy regression tests kiểm tra mismatch/tamper,
dirty checkout, wrong commit, extra/missing files và newline Windows. Khóa/check
130 dependency versions bằng requirements-oasis.lock.txt cho Windows Python 3.11 CPU.
Module path, từng file hash và actual dependency versions ghi trong provenance.
Đã xử lý thêm race port API theo re-review: Kestrel bind 127.0.0.1:0, runner chỉ
dùng URL do child stdout thông báo và kiểm tra process sống trước request. Regression
test-api-isolation.ps1 giữ port cũ 18191 trong suốt full gate; listener đó không
được nhận kết nối nào. PG readiness kiểm TCP server cuối, rồi bounded SELECT 1 từ
host trước khi tạo database; không retry import để che lỗi transaction.

Namespace snapshot engine giờ là oasis-engine-manual-<runGUID>, không tái sử dụng
run-id cho dữ liệu có timestamp khác. Cập nhật đầy đủ lịch sử fixture và engine,
lệnh tái lập luôn dùng constraint file khi cài runtime cho simulator.
Những evidence ở các mục phía dưới là lượt trước review, chưa bao gồm gate mới.

## Kết quả thực hiện

Đã clone [OASIS chính thức](https://github.com/camel-ai/oasis), khóa commit
`ba892e0667eb84b312e4c0e5630f275154bbb98f`, cài package `camel-oasis` 0.2.5 và
`camel-ai` 0.2.90 trong Python 3.11.14 environment riêng, PyTorch CPU.
Không dùng image không rõ nguồn hoặc chỉnh sửa source upstream.

Luồng đã chạy: `oasis.make → env.reset → env.step(ManualAction) → env.close`
→ SQLite do OASIS tạo → adapter nguyên bản → PostgreSQL test → HTTP Favi thật.
**12/12 tests adapter và 48/48 PostgreSQL/import/HTTP checks pass**, process exit 0,
report cuối `passed=true`. Favi base là main `ab875e9`, đã có cold-start.

Ba agents (ID 0/1/2) được signup qua OASIS, tạo ba posts, ba likes, một follow và
một comment qua các action chính thức. Không CREATE TABLE/INSERT bằng script để
làm giả source SQLite. Bảng trace do OASIS ghi xác nhận 3 sign_up, 3 create_post,
3 like_post, 1 follow và 1 create_comment.

## Phân biệt engine và LLM

Đây là **engine thật với action định sẵn**, không phải agent tự quyết định bằng LLM.
SocialAgent nhận StubModel để đáp ứng constructor, không gọi inference hoặc
LLMAction; không có API key, chi phí inference hoặc model local mới được tải.
Không dùng response giả để đóng vai kết quả LLM. Lượt này chỉ chứng minh engine,
schema/action/SQLite và đường tích hợp với Favi.

Lượt này ban đầu không có LLM inference. Phần chạy LLMAction độc lập dưới đây là
bổ sung, dùng model local; không dùng provider trả phí.

## Autonomous LLMAction với Ollama local — 31/10/2026

Đã chạy OASIS thật (pin `ba892e0667eb84b312e4c0e5630f275154bbb98f`) bằng
`LLMAction` / `SocialAgent.astep`, 3 agents x 3 vòng. Local Ollama `0.40.13`,
`qwen3:1.7b` digest `8f68893c685c3ddff2aa3fffce2aa60a30bb2da65ca488b61fff134a4d1730e7`;
thinking off, 512 max completion tokens/call, 0 retries. Inference: 9 calls,
11,519 prompt + 314 completion = 11,833 tokens. Không dùng paid API.

Trace: 3 sign_up, 1 create_post, 3 like_post, 3 create_comment, 8 refresh. DB có
1 post do model viết, 3 likes, 3 comments (7 mutations); 2 like calls trùng bị
no-op và ba comments trùng nội dung. Run evidence:
`.tools/oasis-smoke/llm-qwen3-1_7b-correct-prompt-20261031/` (`oasis.db`,
`requests.jsonl`, `provenance.json`, adapter `export/`). Adapter/PostgreSQL/HTTP
gate PASS: 12 adapter, 7 provenance, 52 PostgreSQL/HTTP checks; API
`http://127.0.0.1:56532/`; integration evidence
`.tools/oasis-smoke/9f832448b87243849f98cb4a974a4fc6/`.

Attempts before success used 28,836 tokens: qwen3:0.6b and an incorrectly wired
1.7b prompt caused invalid post IDs/actions. They were not exported. Final run
and attempts together used 40,669 tokens. Small-model behavior repeats comments
and likes; this is an integration proof, not a behavioral benchmark.

Reproduction (solution folder): start `ollama/ollama:latest` (image digest
`sha256:b2f0122c476bc9fe7ce679d8bb4bd555351e25e2554eefb156f61cb0a89cf429`),
mount cached `.tools/ollama-models`, load `qwen3:1.7b`, expose HTTP only on
`127.0.0.1:11439`, then run:

```powershell
$env:OASIS_LOCAL_MODEL = 'qwen3:1.7b'
./.tools/oasis-env/Scripts/python.exe tools/oasis-smoke/run_autonomous.py .tools/oasis-smoke/llm-replay-<unique-id> .tools/oasis-upstream http://127.0.0.1:11439
```

To run adapter/PostgreSQL/HTTP gate on its export:

```powershell
./tools/oasis-smoke/verify.ps1 -ExistingRunDirectory .tools/oasis-smoke/llm-replay-<unique-id> -Python .tools/oasis-env/Scripts/python.exe
```

Runner enforces loopback, checks pinned runtime/lock, records model digest and
usage, validates SQLite/logical FKs and source DB immutability. Cached model is
in ignored `.tools/ollama-models`; only the owned Ollama container was removed.

## Clock và nhãn media

OASIS Reddit sử dụng naive datetime. Runner tạo Platform với UTC-naive start_time
và Clock(k=1), thời gian trôi thực: tránh việc timestamp local Windows UTC+7 bị
adapter hiểu sai thành UTC. Đây là cấu hình public API, không đổi OS timezone,
không monkeypatch clock hoặc sửa timestamp sau khi source đã được tạo.

Media map được tạo sau simulation theo post_id do OASIS cấp. Hai safe/một restricted
là metadata test explicit để kiểm tra lọc feed, không phải nội dung NSFW được LLM
sinh hoặc nhãn dự đoán từ moderation. Chỉ kiểm tra URL metadata, chưa render ảnh.

## Hai lỗi khai báo FK từ upstream đã phát hiện

1. `social_platform/schema/like.sql`: post_id tham chiếu `tweet(post_id)` nhưng
   engine thực ghi và đọc bảng `post`; bảng tweet không tồn tại. Physical FK check
   báo lỗi trên ba likes dù logical endpoints đều tồn tại trong user/post.
2. `social_platform/schema/group_message.sql`: sender_id tham chiếu user.agent_id,
   cột parent không UNIQUE. Global foreign_key_check báo schema mismatch ngay cả
   khi group_messages rỗng. Group messages không nằm trong contract adapter.

Không sửa nguồn/schema hoặc bỏ toàn bộ validation. Runner ghi nguyên physical FK
diagnostics vào provenance, chỉ nhận diện chính xác các lỗi DDL trên ở commit đã
khóa. Sau đó chạy anti-join toàn bộ post.user, like.user/post, follow.users,
comment.user/post; mọi orphan logic đều làm fail. Adapter tiếp tục validation
timestamp/duplicate/self-follow/media và PostgreSQL kiểm tra FK thật lúc import.
SQLite integrity_check=ok; không tuyên bố global physical FK check đạt.

## PostgreSQL và API

48 checks gồm 30 persistence/import và 18 HTTP/status/behavior. Replay nguyên SQL
không đổi bất kỳ field đã import; lỗi FK trước COMMIT rollback toàn bộ import và
giữ sentinel preexisting. Mapping, Unicode, literal SQL-like text, UTC, NSFW và
media metadata khớp source. HTTP registration lấy JWT thật, đọc profile/post,
reaction summary/comment/followers và hai feed. Cả hai feed chỉ chứa đúng GUID của
hai safe posts; restricted post vẫn có trong storage nhưng không xuất hiện ở feed.

Lượt trước review dùng project `favi-oasis-smoke` và API localhost:18191. Runner
hiện dùng project duy nhất mỗi run và Kestrel tự cấp port; URL thực được ghi trong
report/checks.json, lấy trực tiếp từ stdout process con trước health/request.
Không đọc/ghi database ứng dụng. API và stack test đã dừng sau pass.

## Tái lập và evidence

Clone source vào `.tools/oasis-upstream`, checkout commit đã khóa; tạo
`.tools/oasis-env` bằng Python 3.11. Cài dependencies:

```powershell
uv pip install --python .tools/oasis-env/Scripts/python.exe --torch-backend cpu --constraint tools/oasis-smoke/requirements-oasis.lock.txt .tools/oasis-upstream
./tools/oasis-smoke/verify.ps1 -Simulator -Python .tools/oasis-env/Scripts/python.exe
```

Runner script: `tools/oasis-smoke/run_simulator.py`. Simulator giới hạn 120 giây;
Docker command giới hạn 90 giây; cleanup trong finally. Cần Docker engine hoạt động.

Evidence cuối dưới `.tools/oasis-smoke/7648b5aa3740441f8fe8e38c73eeb9bd/`:

- `oasis.db`: database do engine tạo, không bị adapter sửa.
- `log/`: logs OASIS.
- `provenance.json`: source/version/mode, trace, checksum và physical/semantic FK.
- `environment.freeze.txt`: dependency versions của environment đã chạy.
- `export/`: snapshot, SQL, CSV và manifest checksums.
- `report/checks.json`: 48 checks, passed=true.

SHA-256 source: `9e87930cedbd402a6bfc5bef9e8d1b8b672e2fc5475dc78663662cb189bee067`.
Report tổng `.tools/oasis-smoke/latest.json` ghi mode engine ManualAction, exit 0.
Nhánh `codex/oasis-favi-integration`; chưa commit/push/merge các thay đổi tích hợp.
File `nul` giữ nguyên. Chưa LLMAction, frontend/NGINX/media bytes, tải lớn hoặc CI.

## Kiểm chứng snapshot có tương tác biến thiên

`verify.ps1 -ExistingRunDirectory <run-root> -Python <python>` kiểm chứng một export
đã sinh mà không chạy lại hoặc thêm hành động. Export được sao chép vào run tích hợp
riêng; `run.json` ghi đường dẫn nguồn. Không dùng đồng thời với `-Simulator`.
PostgreSQL so sánh toàn bộ comments/follows theo identity và endpoints, kể cả bảng
rỗng; replay so sánh JSON toàn bộ sáu bảng. HTTP kiểm tra mọi profile, mọi safe post,
reaction count, comment identity/content, follower count và đúng tập safe post IDs
trên hai feed. Snapshot phải vừa một trang (tối đa 100 hàng cho từng tập được đọc).
Số checks phụ thuộc dữ liệu thực tế, không còn cố định 48. Không yêu cầu LLM phải
sinh đủ mọi loại tương tác; những loại không xuất hiện không được coi là đã test
bằng hành vi LLM. Kịch bản ManualAction vẫn kiểm tra các loại đó riêng.

Retest cơ chế snapshot biến thiên ngày 2026-10-31: 12 adapter tests, 7 provenance
tests và 63 PostgreSQL/HTTP checks đạt. Nguồn là export ManualAction đã có
`670c00a6ebf64fa9af623d583ccbf63a`, report mới
`.tools/oasis-smoke/a39057e1e92d4d108c33d6f82e4ba91d/report/checks.json`.
Đây là regression bộ kiểm chứng, chưa phải bằng chứng LLMAction.

## Mô phỏng LLMAction thật, 2026-10-31

Agent Luna thực hiện simulation bằng model local Ollama qwen3:1.7b, không dùng API
trả phí. Ba agent qua ba vòng, 9 inference calls, thinking tắt, tối đa 512 output
tokens/call, không retry. Model tự chọn tool và arguments; không ManualAction.
Kết quả: 3 users, 1 post, 3 likes, 3 comments, 0 follows. Hai lượt like cuối lặp lại
like đã tồn tại nên không thêm hàng. Các comments giống nhau; personas đồng nhất.
Lượt thành công dùng 11.519 prompt + 314 completion = 11.833 tokens. Đây là smoke
simulation nhỏ, không chứng minh hành vi xã hội thực tế hoặc khả năng chịu tải.
Nguồn và request/response đầy đủ:
`.tools/oasis-smoke/llm-qwen3-1_7b-correct-prompt-20261031/`.
Adapter giữ nguyên source DB; integrity và semantic FK đạt. Các giới hạn physical
FK upstream nêu trên vẫn áp dụng; image URLs chỉ là metadata safe explicit.
Import, replay, rollback và HTTP thật đạt 52 checks; 12 adapter tests và 7 source
provenance tests đạt. Report:
`.tools/oasis-smoke/9f832448b87243849f98cb4a974a4fc6/report/checks.json`.
Kestrel của chính process test nhận request tại http://127.0.0.1:56532/; đã dừng.
Container model và stack test đã dọn. Model cache giữ tại `.tools/ollama-models`.
Các thử nghiệm trước thất bại được giữ riêng: hai lượt 0.6B và lượt 1.7B với prompt
cũ không tạo post; probe 0.6B sau sửa prompt vẫn bịa ID. Probe 1.7B đạt trước full
run. Không dùng dữ liệu từ lượt thất bại để tuyên bố simulation thành công.

## Persona và kiểm tra đa dạng bình luận

Runner dùng Mai (backpacker quan tâm chi phí), Bao (người nấu ăn quan tâm nguyên
liệu) và Linh (nhiếp ảnh gia quan tâm văn hóa/đám đông), temperature 0.7. Prompt
được truyền qua constructor để CAMEL ghi đúng system memory. Mỗi inference được
bổ sung tối đa 6 bình luận đã commit trong OASIS, không thêm nội dung giả. Không
sửa hoặc retry bình luận để vượt gate. Evidence ghi persona, temperature và metric.
Gate yêu cầu ít nhất 2 bình luận từ 2 tác giả, không trùng sau chuẩn hóa chữ thường
và dấu câu, mọi cặp có SequenceMatcher similarity <0.85. Đây là phép đo giống nhau
về chuỗi, không phải đánh giá tương đồng ngữ nghĩa hoặc chứng minh cộng đồng thật.
Không đủ bình luận cũng fail gate; giữ nguyên snapshot/request/response để review.

Lượt persona riêng `.tools/oasis-smoke/llm-varied-personas-20261031`: 9 calls,
12.540 prompt +393 completion =12.933 tokens; 3 users, 2 posts, 2 likes, 4 comments,
0 follows. Diversity gate đạt: 4 comments/3 authors, 0 normalized duplicates,
max pair character similarity 0.6816. Bốn unit tests mới đạt. Không tuyên bố đa dạng
ngữ nghĩa hoàn toàn: Bao hỏi về chợ rẻ hai lần với cách diễn đạt khác; cả hai posts
vẫn trùng nội dung. Một call create_comment thiếu post_id bị CAMEL từ chối, không
sinh hàng; 8/9 calls có mutation. Đây là cải thiện bình luận, chưa giải quyết toàn
bộ chất lượng model nhỏ. Không sửa raw content hoặc che lần tool lỗi. Container
Ollama của lượt này đã được remove, cache còn nguyên.
Retest: 12 adapter tests, 11 source/diversity tests, 65 PostgreSQL/HTTP checks passed. Integration report: .tools\oasis-smoke\5db6f85ac7d1479f85da1791b8e79f84/report/checks.json. Test stack cleaned; exit 0.
