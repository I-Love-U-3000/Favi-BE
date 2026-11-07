# Đề tài nghiên cứu Favi và tiến độ triển khai

## Baseline ranking parity — 07/11/2026

524 C#/Python checks pass (20 warm/25 cold fixtures); 13 tests .NET ColdStart
pass. Pure ranking parity, chưa bao gồm cold reader SQL pool hoặc HTTP mới.
[Report](benchmarks/BASELINE_PARITY_20261107.md) và
[handover](ADAPTIVE_HANDOVER.md) ghi phạm vi, source và bước tiếp theo.

## Lifecycle-v2 — pilot data gate PASS, 05/11/2026

Đã implement và chạy main + sparse-topic, mỗi bộ 5k users/2k posts/500 ticks.
Tất cả tám nhóm bắt buộc có >=30 target users ở cả validation/test; không hạ
ngưỡng hoặc chọn user theo future labels. 98 regression tests pass.
[Báo cáo tiếng Việt](benchmarks/adaptive-lifecycle-v2-20261105/REPORT_VI.md)
ghi số liệu, giới hạn và bước tiếp theo. Đây là data readiness, chưa có điểm
adaptive, formula parity mới hoặc confirmatory freeze.

## Cold expansion — 05/11/2026

Đã chạy OASIS với cold waves; cold/general, một topic và nhiều topics đều đủ
92–97 users có target mỗi phase. 97 tests pass, read-back 28 files pass.
Gate tổng chưa đạt vì transition validation thiếu và C3 vắng. Xem
[report cold waves](benchmarks/adaptive-cold-waves-20261105.md).

## Adaptive onboarding — 05/11/2026

Đã nối opt-in `--adaptive` vào generator và chạy OASIS thật: 1.000 users,
30 ticks, 45 posts, 898 likes, generator audit pass; 95 regression tests pass.
Lịch sử pilot có cold/transition nhưng thiếu W2 và W1 ít mẫu, chưa đạt gate
cohort cho benchmark chính. Theo dõi [milestones](ADAPTIVE_MILESTONES.md).

Đã thêm policy arrival/preference deterministic và validator độc lập, dùng đúng
10 topics production. Bốn tests mới pass: catalog parity, deterministic/variation,
timestamp visibility và dữ liệu hỏng. Chưa nối policy vào OASIS action loop;
chưa sinh pilot có targets hoặc chạy benchmark adaptive. Chi tiết trong
[data generation flow](DATA_GENERATION_FLOW.md).

## Adaptive milestone — preflight 04/11/2026

Đã commit nền benchmark/experimental feed và checkout `codex/adaptive-recommender-v1`.
Đã replay benchmark fixture nhỏ và đóng gói/đọc lại 25 archive members thành công.
[Preflight report](benchmarks/adaptive-preflight-20261104.md) ghi kết quả, checksum,
related work ban đầu và khoảng cách 8 topics simulator/10 topics production.
Protocol còn draft; chưa có kết quả adaptive. Người dùng cho phép tiếp tục không
dừng chờ review từng bước; mục tiêu và cách làm việc được lưu trong AGENTS.md.

## Kế hoạch tiếp theo — 04/11/2026

Đã lập [execution plan gợi ý thích ứng cold → warm](ADAPTIVE_RECOMMENDER_EXECUTION_PLAN.md).
Nền tảng hiện đủ để thiết kế nghiên cứu tiếp theo, chưa chứng minh hệ thích ứng
vượt baseline hoặc phục vụ production ở quy mô lớn. Kế hoạch ưu tiên protocol,
dataset có cold/transition thật tại cutoff, đối chiếu công thức hiện tại, rồi mới
thử chính sách chuyển tiếp và kiểm chứng HTTP. Các bước trong kế hoạch chưa chạy;
kết quả lịch sử bên dưới không phải kết quả của phương pháp thích ứng mới.

## Bước 2 và 3 — cập nhật 04/11/2026

MovieLens 100K đã chạy đủ Popularity/EASE/PPR/LightGCN, cùng model/search budget,
global chronological split giữ timestamp ties và positive threshold >=4. Tập
test có 290 users, gồm 97 warm/193 cold. Popularity dẫn nDCG toàn tập; PPR dẫn
nDCG nhóm warm. Kết luận collaborative luôn thắng trên synthetic không chuyển
nguyên sang dữ liệu độc lập; cần giữ cohort và cold fallback rõ ràng. Kết quả
đầy đủ ở [EXTERNAL_RECOMMENDER_BENCHMARK.md](EXTERNAL_RECOMMENDER_BENCHMARK.md).

Đã thêm endpoint feed thử nghiệm có JWT và mặc định tắt, dùng artifact EASE/PPR
thật qua mapping GUID, lọc lại quyền/public/NSFW/ban/archive/delete và positive
hiện tại. Ba gate PostgreSQL/HTTP đều pass và cleanup không lỗi: disabled 1544,
fresh 1687, stale 1553 checks. Regression: 103 Python, 24 .NET unit, 67
architecture/privacy tests. Chỉ là proof trên native fixture 100 users/31 posts,
không phải large-scale serving hay online relevance. Báo cáo và raw-run index ở
[EXPERIMENTAL_RECOMMENDER_FEED.md](EXPERIMENTAL_RECOMMENDER_FEED.md) và
[actions23-20261104.json](benchmarks/actions23-20261104.json).

Lượt đo bổ sung đã hoàn tất sau khi hệ thống duyệt phục hồi: 20 HTTP request warm
cho mỗi phương pháp, mọi sample đều dùng model thật. Gate fresh mới đạt 1767 checks;
ba mode hiện tại đạt 4864 checks, cleanup không lỗi. Kết quả và source backup đã
được lưu local; chưa commit/push. Sleep chỉ được yêu cầu sau khi marker hoàn tất
và checksum báo cáo/archive khớp; trạng thái OS được ghi riêng trong raw results.

## OASIS engine thật — cập nhật 31/10/2026

Sau review Terra High đã sửa harness isolation/cleanup, provenance thực thi,
race port API và namespace snapshot. Bổ sung lockfile cùng regression tests.
Retest đạt 12 adapter tests + 7 provenance tests + 48 checks tích hợp; Docker
sentinel và listener API không bị lượt test khác chạm vào. Chưa commit/push/merge.
Không thay đổi kết luận chất lượng mô hình/agent hoặc thuật toán recommendation;
lượt này tiếp tục là OASIS ManualAction, không LLMAction. Xem báo cáo simulator.

Đã chạy upstream OASIS 0.2.5/commit ba892e0 bằng ManualAction với ba agents,
SQLite và trace do engine tạo; đưa qua adapter/PostgreSQL/API đạt 12 tests + 48
checks. Chưa LLMAction hoặc kết quả đánh giá agent/recommender. Hai lỗi khai báo
FK upstream đã được ghi nhận, logical references user/post hợp lệ. Chi tiết tại
[OASIS_SIMULATOR_VERIFICATION.md](OASIS_SIMULATOR_VERIFICATION.md). Chưa merge/push.

## Bổ sung kiểm chứng OASIS ngày 31/10/2026

Adapter `b325c8e` đã chạy cùng main có cold-start (`ab875e9`) trên nhánh tích hợp
riêng: 12 Python tests + 48 checks PostgreSQL/import/HTTP pass. Đã kiểm chứng SQL
replay, rollback lỗi FK, mapping và dữ liệu đọc qua API/hai feed lọc NSFW. Source
vẫn là fixture tổng hợp, chưa simulator OASIS/LLM thật; chưa push/merge OASIS.
Các nhận định "chưa PostgreSQL import thật" ở báo cáo lịch sử phía dưới được cập
nhật bởi [báo cáo tích hợp mới](OASIS_INTEGRATION_VERIFICATION.md). Không có thêm
kết quả nghiên cứu thuật toán graph hoặc đánh giá agent từ lượt kiểm tra này.

## Trạng thái tích hợp cold-start local

Cold-start được squash vào main local từ feature `7859ded`, tách biệt OASIS và
moderation. Gate mới: 19 unit, 67 architecture/privacy, 109 checks mỗi schema
upgrade/rollback/re-upgrade. Quy trình và giới hạn tại
[COLD_START_RELEASE_REVIEW.md](COLD_START_RELEASE_REVIEW.md). Không push/deploy;
remote main vẫn chưa nhận feature. Các số 82/40 bên dưới là lịch sử kiểm chứng.

Gate cold-start trước merge bổ sung ngày 25/10/2026: hai lượt 82/82 checks pass
trên database test nâng cấp từ migration cuối main có fixture legacy, gồm trường
hợp thiếu Posts.Version và có sẵn Posts.Version=9/index. Dữ liệu profile/post cũ
được giữ nguyên; Like/Love/save đầu tiên chuyển cả hai feed sang warm qua HTTP.
Chỉ mở rộng smoke runner/tài liệu; chưa thử database ứng dụng thật, rollback hoặc
frontend/gateway/load. Chi tiết trong [handoff cold-start](../HANDOFF_COLD_START.md).

Kiểm chứng độc lập bổ sung ngày 25/10/2026 đã chạy lại các bộ test trên từng
commit: cold-start `960e9af` pass 19 unit + 67 architecture/privacy + 40 HTTP;
moderation `e17ae65` pass 10 Python + 11 .NET + 10 HTTP với weights thật;
OASIS `b325c8e` pass 12 Python. Không thay đổi mức hoàn thành nghiên cứu hoặc
tuyên bố accuracy. Xem [báo cáo kiểm chứng](INDEPENDENT_FEATURE_VERIFICATION.md)
để biết môi trường, giới hạn và phát hiện theo mức độ. Các nhánh chưa merge/push
trong lượt kiểm chứng này; graph engine và lifecycle moderation đầy đủ vẫn chưa có
bằng chứng tích hợp.

Ngày cập nhật: 25/10/2026. Báo cáo tổng hợp mã nguồn và kết quả kiểm tra trên ba
nhánh tính năng, đối chiếu với đề cương `Favi_De_Cuong_Graph_Recommendation.doc`.

**Favi đang được chuẩn bị thành môi trường nghiên cứu gợi ý bài đăng dựa trên
đồ thị tương tác. Ba feature mới bổ sung đường chuyển dữ liệu mô phỏng, kiểm duyệt
ảnh và feed cho user mới; phần so sánh thuật toán graph vẫn chưa triển khai.**

## 1. Đề tài đang triển khai là gì

Tên đề tài theo đề cương:

**Xây dựng Favi và đánh giá phương pháp gợi ý dựa trên đồ thị cho mạng xã hội
chia sẻ hình ảnh.**

Mục tiêu là tìm hiểu phương pháp nào gợi ý bài đăng tốt và có chi phí vận hành
phù hợp trong những điều kiện dữ liệu khác nhau. Favi cung cấp ứng dụng để tích
hợp, hiển thị và đo hiệu năng; dữ liệu tương tác có thể được tạo bằng cộng đồng
người dùng giả lập do không có người dùng thật.

### Câu hỏi nghiên cứu

1. Popularity, Personalized PageRank và LightGCN khác nhau thế nào về chất lượng
   gợi ý trên cùng dữ liệu và giao thức đánh giá?
2. Khi tương tác thưa hoặc người dùng ít hoạt động, phương pháp nào phù hợp hơn?
3. Chi phí training, cập nhật, inference, bộ nhớ và latency đổi lại chất lượng ra sao?
4. Chính sách chọn phương pháp theo trạng thái dữ liệu/ngân sách có tốt hơn dùng
   cố định một thuật toán được chọn trên validation hay không?

| Phương pháp dự kiến | Ý tưởng |
|---|---|
| Popularity | Xếp hạng bài theo mức phổ biến từ tương tác được phép sử dụng |
| Personalized PageRank | Lan truyền mức liên quan từ user qua các liên kết graph |
| LightGCN | Học embedding user–post từ cấu trúc tương tác |
| Chính sách lựa chọn | Chọn phương pháp dựa trên điều kiện dữ liệu và chi phí đo được |

Cold-start giải quyết user chưa có lịch sử, còn NSFW/violence giúp kiểm soát nội
dung ứng dụng. Hai phần này hỗ trợ hệ gợi ý, không thay thế đóng góp nghiên cứu
so sánh thuật toán và chính sách lựa chọn.

### Vai trò của cộng đồng giả lập

Hướng đã thống nhất là dùng agent có sở thích, mức hoạt động và hành vi riêng
để tạo cộng đồng có tương tác. OASIS có thể đóng vai trò simulator; adapter mới
chỉ chuyển database đầu ra của simulator sang dữ liệu dùng được trong Favi.

```text
Cộng đồng OASIS có tương tác
    → SQLite của simulator
    → Adapter kiểm tra và xuất snapshot
        → Graph CSV dùng cho nghiên cứu
        → SQL seed dùng cho Favi hiển thị
    → Thuật toán graph và bộ đánh giá cần xây tiếp
    → Feed Favi và đo hiệu năng
```

Đây là hướng sinh dữ liệu phục vụ đề tài recommendation. Chưa chuyển mục tiêu
khóa luận thành chứng minh agent giống người thật. Nếu muốn tuyên bố mức độ giống
con người, cần thêm phương pháp và bằng chứng kiểm chứng riêng.

## 2. Độ tương thích với ứng dụng hiện tại

**Ước lượng mức phù hợp kỹ thuật khoảng 80–85%.** Đây là nhận định kiến trúc,
không phải tỷ lệ test pass hoặc kết quả thực nghiệm. Phần lớn nền tảng ứng dụng
có thể giữ lại; cần bổ sung graph engine và quy trình nghiên cứu phía sau feed.

| Thành phần | Phần tận dụng được | Phần cần bổ sung hoặc kiểm chứng |
|---|---|---|
| Backend .NET, CQRS và module | Các luồng nghiệp vụ và query reader hiện có | Graph adapter thật và tích hợp các nhánh |
| PostgreSQL | User, post, reaction, follow, collection, tag | Import snapshot thật; cold-start đã kiểm tra migration và truy vấn trên PostgreSQL riêng |
| Feed | API, phân trang, DTO và giao diện hiển thị | Ranking graph cho user có lịch sử; đánh giá chất lượng |
| Redis, Docker, gateway, k6 | Hạ tầng phục vụ và kiểm thử tải | Workload riêng cho từng chiến lược và kích thước graph |
| Vector API và Qdrant | Tìm kiếm ảnh tiếp tục sử dụng | Qdrant không tự trở thành user–post graph recommender |
| Dữ liệu seed | Nội dung và fixture cho ứng dụng | Một cộng đồng mô phỏng có cấu hình, lịch sử và snapshot được khóa |
| Kiểm duyệt ảnh | Có client, worker và cờ IsNSFW | Hiệu chỉnh ngưỡng và khép kín trạng thái trước khi công khai nội dung |
| Onboarding | Có tài khoản và API interests mới trên cold-start branch | Frontend chọn chủ đề chưa được xây trong task vừa rồi |

Không cần đổi sang graph database để bắt đầu. CSV có thể được Python đọc thành
graph/ma trận thưa trong RAM, còn PostgreSQL tiếp tục phục vụ ứng dụng.

Chênh lệch cần thống nhất trong báo cáo: đề cương ghi .NET 8, API hiện target .NET 9.

## 3. Ba feature mới đã bổ sung những gì

### 3.1 OASIS snapshot adapter

**Đã có:** script Python đọc user, post, like, follow, comment từ OASIS SQLite;
kiểm tra tham chiếu, timestamp và dữ liệu không hợp lệ; xuất snapshot JSON, graph
CSV, manifest checksum và SQL seed. GUID được ánh xạ ổn định trong cùng run-id.

Graph positive hiện chỉ gồm like vào bài được media map đánh dấu an toàn; follow
được xuất riêng thành graph user–user. Comment phục vụ demo, chưa là positive edge.

**Chưa có:** chạy OASIS/LLM thật, cấu hình cộng đồng agent, live adapter gọi API
Favi, save edge hoặc khôi phục lịch sử unlike/unfollow. Cần cung cấp ảnh và nhãn
NSFW trong media map. Agent import chưa có email/password để tự đăng nhập Favi.

### 3.2 Kiểm duyệt ảnh bằng pretrained model

**Đã có:** một SigLIP2 vision encoder chạy CPU và hai ONNX head pretrained
NSFW/violence dùng chung embedding. Không cần train lại hoặc gọi API inference trả phí.

Routing kiểm tra NSFW trước. Nếu đủ điểm reject thì dừng; các trường hợp khác
tiếp tục kiểm tra violence. Kết quả approve/review/reject; lỗi hoặc không chắc
chắn không được coi là ảnh an toàn. Có giới hạn ảnh, CPU concurrency và cache score.

**Chưa có:** đánh giá accuracy trên ảnh Favi, calibration ngưỡng, workflow duyệt
thủ công riêng và bảo đảm mọi feed/search đều chặn ảnh chưa được worker kiểm tra.
IsNSFW legacy hiện gộp review/reject. Không có model được chứng minh hoàn hảo.

### 3.3 Cold-start feed và lựa chọn chủ đề

**Đã có:** API catalog chủ đề, GET/PUT interests self-only, lưu version để chống
ghi đè lựa chọn, migration và feed cold-start ở cả hai endpoint feed.

- User chưa có Like/Love/save và chưa chọn chủ đề: trộn nhiều chủ đề.
- User cold đã chọn 1–10 chủ đề: ưu tiên các chủ đề đó.
- Ranker loại trùng, ưu tiên nhiều tác giả, bù catalog chung khi thiếu bài.
- Chỉ lấy nội dung public/safe/active, lọc lại trước phân trang.
- Có contract IGraphCandidateSource chờ output PostId/Score từ engine tương lai.

**Chưa có:** graph engine thật hoặc frontend onboarding. Default graph source
trả rỗng và dùng catalog thật làm fallback. Sau positive đầu tiên, user trở lại
feed warm cũ; cold-start này không phải LightGCN hoặc chính sách chọn thuật toán.

## 4. Tiến độ chung sau ba feature

### Trạng thái Git và tích hợp

| Nhánh | Commit | Trạng thái tại thời điểm báo cáo |
|---|---|---|
| feat/oasis-adapter | b325c8e | Đã commit và push origin |
| feat/pretrained-image-moderation | e17ae65 | Local, chưa push |
| feat/cold-start-feed | 7859ded | Source cold-start và gate đã review |
| main local | Squash cold-start từ 7859ded | Tích hợp riêng cold-start; không OASIS/moderation |
| origin/main | 155d6be theo ref local | Không push trong phiên này |

Ba nhánh cùng xuất phát từ main và không chứa commit của nhau. Vì vậy, **đã có
code ở ba nhánh không đồng nghĩa main đã chạy đồng thời cả ba feature**. Chưa
có kiểm thử full-stack của tổ hợp sau merge.

### Ước lượng mức hoàn thành đề cương

Nếu tính phần code hiện có trên cả ba nhánh, mức đáp ứng tổng thể được ước lượng
**khoảng 40%, với khoảng tham khảo 35–45%**. So với ước lượng trước khoảng 35%,
tiến bộ chủ yếu ở dữ liệu đầu vào, kiểm duyệt và trải nghiệm user mới. Phần lõi
thuật toán và đánh giá chưa có bước tiến tương ứng.

Thang trọng số giữ theo đánh giá trước để tránh đánh đồng số feature với tiến độ
nghiên cứu. Các mức hoàn thành trong bảng là đánh giá chủ quan có căn cứ mã nguồn,
không phải số liệu đo lường:

| Nhóm công việc | Trọng số | Mức đáp ứng ước lượng | Điểm quy đổi |
|---|---:|---:|---:|
| Nền tảng Favi và hạ tầng | 25% | 85% | 21,25% |
| Dữ liệu tương tác và pipeline graph | 15% | 50% | 7,50% |
| Ba baseline recommendation | 20% | 10% | 2,00% |
| Chính sách chọn chiến lược | 15% | 0% | 0,00% |
| Giao thức đánh giá tái lập | 15% | 5% | 0,75% |
| An toàn nội dung và hỗ trợ đo hiệu năng | 10% | 70% | 7,00% |
| **Tổng** | **100%** | | **38,50% ≈ 40%** |

Main local đã tích hợp cold-start; remote chưa được cập nhật trong phiên này. Phần
lõi nghiên cứu vẫn mới ở mức chuẩn bị nền tảng; chưa có kết quả chứng minh phương
pháp nào tốt hơn hoặc chính sách thích ứng có lợi.

### Những kiểm tra đã thực hiện

| Feature | Kiểm tra đã pass | Bằng chứng và giới hạn |
|---|---|---|
| OASIS adapter | 12/12 Python tests | Fixture SQLite, mapping, validation, graph export, SQL replay trên SQLite stand-in; chưa OASIS/PostgreSQL thật |
| Moderation | 10/10 Python tests; 11/11 .NET tests | Thêm 10 kiểm tra HTTP/weights thật qua .NET NSFWService; chưa đánh giá accuracy hoặc vòng đời upload đầy đủ |
| Cold-start | 19/19 unit tests; 67/67 architecture/privacy tests; 40 HTTP checks | Kestrel + PostgreSQL 15 + Redis 7 thật; JWT, migration, hai feed, replay và concurrency đã pass |

Các bộ .NET có test cũ dùng chung giữa các nhánh, không cộng các con số này như
số test mới độc lập. Moderation có 5 .NET test mới; cold-start có 13 test mới,
gồm 8 ranking/domain/handler và 5 reader tests.

Backend đã build thành công trên các nhánh có thay đổi .NET. Cold-start đã sinh
idempotent migration SQL và kiểm tra model khớp snapshot; đã apply migration
trên PostgreSQL test riêng, chưa đụng database ứng dụng. Model moderation cho warm inference khoảng 0,656 giây trên
một ảnh RGB nhân tạo trong một lượt đo; đây không phải SLA hoặc kết quả accuracy.

## 5. Phần cần làm tiếp để có kết quả nghiên cứu

1. Chạy một cộng đồng OASIS nhỏ với sở thích và mức hoạt động khác nhau; kiểm tra
   dữ liệu tiếp xúc và tương tác sinh ra có cấu trúc phù hợp.
2. Khóa dataset, source ID, timestamps và giao thức train/validation/test. Adapter
   hiện xuất current-state snapshot; temporal evaluation cần event log hoặc chuỗi
   snapshot nhất quán, không suy ra toàn bộ lịch sử từ database cuối cùng.
3. Triển khai Popularity độc lập và Personalized PageRank, dùng chung candidate
   set và bộ đo Recall@10/nDCG@10.
4. Thêm LightGCN, đo chất lượng, training/refresh cost, latency và RAM.
5. Chọn policy trên validation rồi so với best-fixed trên test độc lập.
6. Tích hợp các nhánh, kiểm tra PostgreSQL/full-stack, nối graph adapter thật,
   hoàn thiện frontend onboarding và đo tải qua gateway hiện tại.

Chỉ kết luận trong các điều kiện mô phỏng đã kiểm soát. Dữ liệu agent/seed không
tự chứng minh hiệu quả trên người dùng thật; không mặc định GNN hoặc policy luôn thắng.

## 6. Nguồn kiểm chứng trong repository

- OASIS: `feat/oasis-adapter:Favi-BE/HANDOVER_OASIS_ADAPTER.md`.
- Moderation: `feat/pretrained-image-moderation:Favi-BE/HANDOFF_PRETRAINED_MODERATION.md`.
- Cold-start: [HANDOFF_COLD_START.md](../HANDOFF_COLD_START.md).
- Cold-start spec: [COLD_START_FEED.md](COLD_START_FEED.md).

Có thể đọc handoff trên nhánh khác mà không đổi working tree bằng `git show`,
ví dụ:

```powershell
git show feat/pretrained-image-moderation:Favi-BE/HANDOFF_PRETRAINED_MODERATION.md
```

## Kiểm chứng bổ sung ngày 25/10/2026

Cold-start smoke bắt được lỗi version tăng hai lần; đã sửa và thêm regression test.
NSFW dùng chung production router cho vector API và entrypoint moderation riêng,
preload trước nhận request, có GET /nsfw/ready. Client .NET thật đã gọi qua HTTP.
Ảnh mẫu COCO là ảnh thường nhưng violence score khoảng 0,234 nên bị review với
ngưỡng safe 0,2: đây là bằng chứng còn cần calibration, không phải accuracy đã đạt.
Hướng dẫn chạy lại nằm trong tools/cold-start-smoke/README.md và trên moderation
branch tại tools/moderation-smoke/README.md. Ghi nhận này thuộc ngày 25/10; OASIS
đã chạy engine, LLMAction và cộng đồng hybrid thật trong các lượt ngày 31/10/2026.
Xem [báo cáo simulator](OASIS_SIMULATOR_VERIFICATION.md) và
[mô hình cộng đồng nhỏ](SMALL_SOCIAL_COMMUNITY.md) cho kết quả, giả định và giới hạn.

## Cộng đồng hybrid nhỏ — 31/10/2026

Đã thực thi 100 tài khoản/20 ticks với vai trò 90/9/1, homophily, memory riêng,
bounded-confidence opinions và fatigue/trust giả lập. Bản cuối sinh 4 posts,
65 likes, 218 follows (200 bootstrap), 4 comments từ 4 authors bằng 12 local LLM
calls/3.171 tokens; code chọn hành động và valid IDs, LLM chỉ sinh văn bản.
30 unit tests và 853 PostgreSQL/HTTP checks đạt. Đây là smoke research model,
chưa calibration người thật, frontend, load test hoặc production deployment.

## Agent tự quyết — 01/11/2026

Thêm chế độ tự chọn identity/emotion/goal, trải nghiệm riêng, hành động và nghỉ;
không role/topic/quota gán sẵn. Default local qwen3:4b, raw state/memory riêng,
event membership chỉ do từng người tự chọn trong simulator. Bản cuối đã qua42
unit tests và35 PostgreSQL/HTTP checks. Các lượt trước: 1.7B thiên về đăng bài,
4B với format cũ thiên về chờ; không sửa dữ liệu để che kết quả này. Đây là quyền
tự quyết kỹ thuật, chưa phải hành vi người thật đã được kiểm định.
Xem [thiết kế, kết quả và audit](FREE_SOCIAL_AGENTS.md).

Lượt10 người/4 vòng đã hoàn tất: 16 posts, 18 follows, 2 comments, 0 likes,
4 wait; 50 local calls/58.679 tokens, không invalid decision. 42 unit tests và264
PostgreSQL/HTTP checks đạt. Chất lượng chưa đạt tự nhiên: 6 duplicate post rows
(sau lowercase+trim), 2 comments trùng nguyên văn từ cùng ID5 vào cùng post11.
Giữ nguyên dữ liệu, không thêm hành vi để che hạn chế này.

## OASIS native trên GPU — 01/11/2026

Đã chạy10người/4vòng bằng LLMAction/CAMEL memory thật, TwHIN-BERT và Qwen3:8b
hoàn toàn trên GPU cho generation. Sinh35bài gốc +6repost/quote,30comments từ cả
10người,9likes,3follows,1group/7messages,2private reflections. 148local requests
dùng880.581tokens (28requests là native memory summaries). 50unit tests và551
PostgreSQL/import/HTTP checks đạt cho Favi subset10users/35posts/9likes/3follows/
30comments. Groups/repost/quote chỉ giữ raw, chưa import đầy đủ vào Favi.

Chất lượng chưa tự nhiên:10duplicate original post rows,12duplicate comment
rows,18tool requests bị từ chối; native empty-feed và physical FK DDL bugs được
ghi rõ. Nhiều tương tác hơn nhưng không phải single-variable A/B hoặc bằng chứng
mô phỏng xã hội đã calibration. Chi tiết và toàn bộ evidence trong
[OASIS_NATIVE_FULL_RUN.md](OASIS_NATIVE_FULL_RUN.md).

## Flow dataset mặc định cho benchmark — 01/11/2026

Chọn controlled hybrid ABM + corpus tổng hợp có seed + public OASIS actions;
không LLM trong vòng lặp. JSON config version1, wrapper một lệnh, immutable output,
manifest checker và ordered exposure/event history giúp chạy lại và dùng dataset
cũ. Đây là workload graph, chưa benchmark relevance trên người thật/nội dung ảnh.

Profile mặc định đã chạy1000users/80ticks trong259,406giây:374posts,3660likes,
2511follows,644comments,12678activation exposures;605users có >=3distinct likes
trong cả lượt. Gate đạt65unit tests và19982PostgreSQL/import/HTTP checks. Chưa
train/evaluate Popularity/PPR/LightGCN hoặc chọn temporal cutoffs. Chi tiết config,
setup, dữ liệu và cách dùng lại trong [DATA_GENERATION_FLOW.md](DATA_GENERATION_FLOW.md).

## Popularity và audit công thức feed — bổ sung 01/11/2026

Đã triển khai evaluator offline, split global ticks60/20/20, cùng candidate set,
loại self/seen, khóa fit trước mỗi label window. Test448users: Popularity đạt
Recall@10=0,022879/nDCG@10=0,011188; bản port công thức warm feed đạt0,028646/
0,013791 với giả định1hour/tick. Riêng417warm users: Recall0,022182 vs0,025180.
Khi bỏ decay, formula Recall còn0,016257; kết quả phụ thuộc thang giờ và synthetic
policy. Không đổi production ranking; chưa bằng chứng tốt hơn có ý nghĩa thống kê.
73unit tests đạt, repeated quality metrics giống nhau. Chưa PPR/LightGCN hoặc
multi-seed evaluation. [Protocol, audit và kết quả](RECOMMENDER_BENCHMARK.md).

## Dataset Favi dùng chung đã provision

`favi-5k-v1` đã lưu cục bộ:5000users,2000posts,137198likes,28835follows,
20248comments;665528events với477247exposures. Bundle có SQLite, event history,
Favi SQL/snapshot và graph CSV; ZIP60.285.459bytes đã kiểm từng member và checksum.
Gate đạt288118PostgreSQL/import/HTTP checks, test stack đã cleanup. Popularity
chạy trực tiếp trên bundle đã lưu với4943test users; không phải regenerate.
Hướng dẫn, inventory, đường dẫn và giới hạn tại
[FAVI_BENCHMARK_DATASET.md](FAVI_BENCHMARK_DATASET.md).

## Formula V2 đã tuning và benchmark

Đã chạy12cấu hình validation trên favi-5k-v1, khóa lựa chọn trước test: trọng số
social1.5, decay0, cap2bài/tác giả. Trên4943test users: Popularity Recall@10
0,004258/nDCG0,003644; V1 đạt0,005224/0,004081; V2 đạt0,006863/0,005726.
V2 hơn V1 khoảng31,4%Recall và40,3%nDCG, nhưng tác giả/top10 giảm9,87→5,66;
catalog coverage tăng3,6%→9,8%.79unit tests và kiểm hash/selection/data đều đạt.
Đây là offline heuristic benchmark trên một dataset tổng hợp, không đổi production
và chưa chạy EASE/LightGCN. Chi tiết tại [FORMULA_V2_BENCHMARK.md](FORMULA_V2_BENCHMARK.md).

## Benchmark mở rộng đã chạy

Đã bổ sung EASE, Personalized PageRank chính xác trên graph user–post và CPU
LightGCN/BPR, tuning trên validation rồi refit/test với cùng dataset/candidates.
EASE Recall@10=0,021262/nDCG0,017025; PPR0,019536/0,015564; LightGCN trung bình
3training seeds đạt0,019731/0,015873. Formula V1/V2 giữ làm reference riêng vì có
thêm follows/comments.89unit tests đạt; checkpoint/data/artifact hashes đều kiểm.
Không đổi production. Chi tiết cấu hình, training budget, chi phí và giới hạn tại
[EXPANDED_RECOMMENDER_BENCHMARK.md](EXPANDED_RECOMMENDER_BENCHMARK.md).

## Bổ sung hai quy mô dataset

Đã lưu thêm favi-2k-v1: 2.000 users, 1.000 posts, 53.607 likes, 10.862 follows,
7.751 comments; và favi-10k-v1: 10.000 users, 4.000 posts, 277.340 likes,
58.957 follows, 40.891 comments. Cả hai chạy đủ 500 ticks, đạt kiểm tra native
OASIS/event parity/adapter và có ZIP đã đọc lại toàn bộ file để kiểm SHA256.
Popularity dùng lại dữ liệu đã lưu với lần lượt 1.971 và 9.903 test users;
92 unit tests đạt. Bundle 5k cũ được kiểm lại và không đổi hash. Chưa chạy lại
EASE/PPR/LightGCN/Formula hoặc gate PostgreSQL/HTTP trên hai bộ mới. Hai seed và
quy mô cùng thay đổi, chưa thể quy chênh lệch kết quả riêng cho quy mô dữ liệu.
Cấu hình, inventory và cách dùng tại [FAVI_DATASET_SCALES.md](FAVI_DATASET_SCALES.md).

## Benchmark đầy đủ trên ba quy mô

Đã chạy tuần tự Popularity, Formula V1/V2, EASE, PPR và LightGCN trên cả 2k, 5k,
10k với cùng K=10 và cutoffs 300/400. Mỗi bộ tuning bằng validation riêng;
LightGCN test đủ ba seed huấn luyện. Cả sáu manifest, selection hash, fingerprint
tập đánh giá và hash SQLite/events đều được kiểm. Toàn bộ metric chất lượng của
bộ 5k tái lập chính xác kết quả trước. nDCG test cao nhất trong nhóm likes-only:
PPR ở 2k (0,03325451), EASE ở 5k (0,01702500) và 10k (0,00841507).
Formula V2 tăng accuracy so với V1 cả ba bộ nhưng tập trung tác giả mạnh ở 2k/10k.
Chưa có bằng chứng về người dùng thật hoặc ảnh hưởng riêng của quy mô, vì seed
và kích thước cùng thay đổi. Không đổi production và không chạy gate HTTP ở
lượt này. Bảng Recall/nDCG, độ lệch seed, chi phí, tham số và artifact tại
[THREE_SCALE_RECOMMENDER_BENCHMARK.md](THREE_SCALE_RECOMMENDER_BENCHMARK.md).

## Bước 1: kiểm tra nhiều dataset seed cùng quy mô 5k

Đã hoàn tất ba seed mới 20261106/20261107/20261108, mỗi bộ 5.000 users,
2.000 posts và đủ 500 ticks. Likes lần lượt 136.713/136.752/137.719. Tất cả
đạt native/event/adapter validation và ZIP readback; benchmark cùng protocol và
search budget, LightGCN đủ ba optimizer seed trên mỗi dataset. 98 tests pass.
Đã kiểm raw manifest, model artifact, frozen selection, population và source hash.

Trung bình nDCG test trên ba dataset mới: Popularity 0,00387281; EASE
0,01610449; PPR 0,01647590; LightGCN (đã trung bình optimizer trong từng dataset)
0,01623641. Collaborative baselines vượt Popularity cả ba bộ; nDCG cao nhất từng
bộ lần lượt là EASE, LightGCN mean và PPR. Không có phương pháp collaborative
thắng nhất quán. Bộ 5k cũ chỉ làm reference, không trộn vào primary mean.
Đã lưu báo cáo, JSON, config và inventory; chưa commit/push. Dừng tại bước 1 để
người dùng review; chưa thực hiện kiểm chứng dữ liệu độc lập hoặc tích hợp feed.
Chi tiết: [MULTISEED_RECOMMENDER_BENCHMARK.md](MULTISEED_RECOMMENDER_BENCHMARK.md).
