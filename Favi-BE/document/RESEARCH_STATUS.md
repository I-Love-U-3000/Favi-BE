# Đề tài nghiên cứu Favi và tiến độ triển khai

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
branch tại tools/moderation-smoke/README.md. OASIS vẫn chưa chạy simulator thật.
