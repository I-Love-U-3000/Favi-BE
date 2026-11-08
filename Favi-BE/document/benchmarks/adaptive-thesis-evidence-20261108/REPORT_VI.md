# Favi: bằng chứng từ cold-start đến cá nhân hóa

Ngày 08/11/2026. Gói M5 tổng hợp các kết quả đã khóa, không tạo thí nghiệm mới.
**Chưa có bằng chứng đủ để thay B1 bằng router R vì lợi ích thực dụng.**
R đạt main mean nDCG@10 0.013260531, B1 đạt 0.012925747; chênh lệch
+0.000334784 thấp hơn ngưỡng tuyệt đối 0.001 đã khai báo trước test.
Robustness với topics noisy/independent hoặc không có topics cho delta âm.
HTTP lifecycle/SQL và direct latency đạt trong lab; gateway load thất bại do429.
Kiểm toán chi phí hoàn tất nhưng search-only time và peak RAM training gốc
không được đo. Không gọi toàn bộ relevance/serving/resource gates là PASS.

## Luận điểm và câu hỏi nghiên cứu

Đối tượng là recommendation trong Favi khi người dùng bắt đầu bằng topics hoặc
general defaults, có ít lịch sử rồi có nhiều positives. Đóng góp có thể bảo vệ
là protocol xuyên suốt, phép so sánh có đối chứng và ranh giới thất bại gắn với
phục vụ thử nghiệm; không khẳng định thuật toán mới hay tính mới chỉ vì dùng
EASE/PPR/LightGCN. Đây là tổng hợp trên synthetic observed-positive replay,
không phải bằng chứng tăng hài lòng hoặc engagement của người thật.

| Câu hỏi | Kết luận được hỗ trợ | Điều chưa được hỗ trợ |
|---|---|---|
| RQ1: topics/general giúp cold trong điều kiện nào? | Bảng cohort cho thấy kết quả phụ thuộc nhóm và chất lượng topics; topic-only main toàn tập cao hơn Popularity trong dữ liệu này | Không suy từ aggregate thành mọi cold cohort đều thắng; không coi topic match là hài lòng |
| RQ2: chọn thời điểm chuyển có hơn chuyển ở positive đầu tiên? | Joint R không đạt practical gate; sparse R=B1; ablation cùng cấu hình cho hiệu ứng threshold không ổn định mọi seed | Không khẳng định ngưỡng5 tối ưu phổ quát hoặc joint delta chỉ do routing |
| RQ3: có phục vụ được và chi phí có xứng đáng? | Artifact sample250, lifecycle/SQL và direct lab p95 đạt; gateway fail; có elapsed/refit đã audit | Chưa đủ full5k serving, training RAM/search-only cost, production throughput hoặc lợi ích chi phí tổng thể |

## Thiết kế có thể kiểm tra

Nguồn chuẩn là [frozen protocol](../adaptive-protocol-v1.md),
[offline contract](../../ADAPTIVE_OFFLINE_CONTRACT.md) và
[serving contract](../../ADAPTIVE_SERVING_CONTRACT.md). Kế hoạch ban đầu ở
[execution plan](../../ADAPTIVE_RECOMMENDER_EXECUTION_PLAN.md) là lịch sử ý định;
trạng thái thực thi phải đọc ở reports mới nhất, không dùng các dòng pending cũ
để thay kết quả đã chạy.

Sáu datasets main/sparse × seeds20261111/12/13, mỗi dataset5.000 users,
tối đa2.000 posts và500ticks. Mọi integrity/cohort gate và ZIP readback đạt;
không đổi seed hoặc hạ gate sau khi quan sát. Train events<300; validation
request300 với targets[300,400); refit events<400; test request400 với
targets[400,500). Features, arrivals, items và topics phải có trước request.
Latent interests không làm feature model. Topics informative vẫn bắt nguồn từ
thiết kế simulator; không đồng nghĩa khảo sát sở thích thật.

Eligible chung loại own, prior positive và future items. Targets là distinct
held-out likes giao eligible; người không có target là N/A, được giữ trong
gate inventory. Fingerprints users/candidates/targets giống nhau giữa methods.
Cold retrieval hữu hạn top20/topic và general top60 có thể bỏ relevant target;
target denominator không bị cắt để làm đẹp metric. Cùng eligible không có nghĩa
mọi method dùng cùng retrieval pool.

B1 chọn family/config/epoch ở threshold1; R chọn joint threshold{1,5} và
family/config/epoch trên cùng validation records. Selection lưu/hash trước test.
LightGCN validation dùng optimizer20261103, test dùng20261103/04/05, refit đúng
epoch đã chọn. Gộp optimizer trong dataset trước rồi trung bình đều ba dataset;
không coi chín optimizer runs là chín cộng đồng độc lập. Ba dataset seeds chỉ
mô tả ổn định, không có p-value hoặc claim statistical superiority.

Popularity/EASE/PPR/LightGCN dùng likes; system methods có cùng cold fallback.
Formula V1/V2 warm còn dùng follows/comments; V1 observed tag count1, V2
historical tag count0 và heuristic khác. Vì vậy bảng là so hệ thống đã khai báo
tín hiệu, không phải mọi thuật toán nhận đầu vào hoàn toàn giống nhau.
Primitive source ranking parity không chứng minh SQL/API parity.

## Kết quả chất lượng và phản ví dụ

[Bảng số liệu sinh trực tiếp từ JSON](TABLES_VI.md) chứa đủ chín methods cho
main/sparse, B1/R và đối chứng theo cohort lịch sử, robustness và hashes nguồn.
Script kiểm lại mean từng method từ ba dataset, paired delta và cohort delta;
không chép lại số đã làm tròn để tính toán. Bảng cohort là ảnh chụp các nhóm
người khác nhau tại cutoff, không chứng minh một người sẽ tiến bộ khi thêm likes.
Các cohort rỗng không được gán nDCG0.

Main thắng/thua theo dataset là2/1, sample std delta0.000661326. Practical gate
đòi cả mean delta≥0.001, ít nhất2/3 seed không âm và không mean cohort loss
dưới−0.002. Hai điều kiện sau đạt, điều kiện gain không đạt: **gate tổng FAIL**.
T1/T2 mean delta lần lượt+0.001508894/+0.001114060; W1/W2 lần lượt
−0.000003689/−0.000198901. Không lấy hai cohort dương thay primary user macro.
Cold C0/C1/C2 delta R−B1 bằng0 vì cùng fallback; không chứng minh topics vô ích.
Trong main, C0 B1/R mean0.002986432 thấp hơn Popularity0.003770260;
C1/C2 lần lượt0.018381056/0.011714377 cao hơn Popularity0.002508096/0.003274679.
Do đây là các nhóm khác nhau trong simulator, không gán chênh lệch giữa C0 và
C1/C2 thành tác động nhân quả của việc chọn topics.
Sparse C3 có704/675/671 target users, R=B1 ở mọi seed nên delta0 không chứng minh
router xử lý scarcity tốt hơn baseline.

Main12 chọn LightGCN khác config/epoch cho B1/R, nên joint delta có cả lựa chọn
model và threshold. Ablation giữ R config rồi đặt threshold1 cho
R−R-no-routing mean+0.000416166; R-no-routing−B1 mean−0.000081382.
Thành phần threshold có seed âm−0.000143273, không ổn định toàn bộ.
LightGCN family threshold1 có main test mean cao nhất trong các rows; không
được thay B1 bằng lựa chọn tốt nhất trên test rồi gọi đó là xác nhận mới.

Main R−B1 khi noisy là−0.001044392, independent−0.001499580,
no-topics−0.001437935; cả ba dataset đều âm trong mỗi mode này. Replay giữ
selection và event log, không đo hành vi mới hoặc causal effect. Sparse
informative là metadata trước scarcity, khác primary music-only và có chuyển
cohort; không so means của current cohort với primary cohort như cùng nhóm.
B1/R feed đủ10 ở target users không chứng minh retrieval recall cao: cold pool
misses vẫn được giữ trong report gốc.

## Chi phí và hành vi phục vụ

| Phép đo | Kết quả | Phạm vi diễn giải |
|---|---|---|
| Sáu lượt ranking gốc |12641.235283s, khoảng3h30m41s|Gate/search/refit/evaluation/diagnostics/I/O/polling≤20s và post-run hashes; không riêng training|
| Refit gốc duy nhất |33fits,199.635498s|Cache family+sorted parameters+optimizer seed; không cộng theo label/threshold|
| Search-only, validation-fit-only, training peak RAM gốc |Không đo|Không phục hồi từ log không timestamp; không lấy elapsed trừ refit làm search|
| Export sample |250users,11867251bytes|1000user attempt bị16MiB chặn; không full5k|
| Export refit / model state |0.365270s /32521008bytes|Fit mới cho export; state arrays+CSR không phải process peak RAM|
| Export/replay / exporter total |1.132263s /12.125867s|Ranh giới timer riêng; không cộng thành historical pipeline cost|
| Fresh start→health |5unique processes,2422.4788–3073.2132ms|Host/DB/JIT/load; không load-only hoặc disk-cache-cold|
| API peak cuối workload |258326528bytes|Serving process resident memory; không training hay fullstack RAM|
| Direct B1/R p95 |57.2082/57.4861ms, PASS≤100ms|10concurrent,100warmup+1000measured/method|
| Gateway measured B1/R |1000×429 /999×429+1×200, FAIL|Single upstream,150/minute,burst30 giữ nguyên;429 không là latency feed|

Model-state sum có thể cộng trùng CSR chung, không phải physical cache allocation.
Thiếu telemetry điện/nhiệt/tải và phần mềm chạy cùng nên không suy chi phí tiền,
energy, hardware isolation hoặc tốc độ tương đối công bằng giữa family.
Không chạy cost-only replay để giả phục hồi số quá khứ.

Fresh HTTP run a82952222e0243599448477ec5b72e08 đạt113848 checks với63lifecycle
requests; stale run23c76fb1b6f8485899f9d99ee5a43912 đạt113757 checks với17requests.
Phần lớn checks là import/SQL, không phải tương ứng số HTTP requests. Đã kiểm
PostgreSQL UNION distinct Like/Love UNION save, routing4→5, duplicate Like/save,
save-only, topics/version/idempotency, new user/item, privacy/ban/NSFW/archive/
delete/own/current positives, missing/stale/empty eligible fallback và startup
fail-closed khi thiếu file/checksum sai. Operational positives rộng hơn offline
likes-only; live count chỉ route/filter, không retrain scores.

Actual artifact250 không có row R empty learned-history: trường hợp đó chỉ có
unit-fixture, không claim actual-row HTTP. Không có multi-replica/TLS/SignalR
hoặc toàn gateway proof. Mặc định experiment vẫn tắt; không merge/deploy.
23focused .NET tests trước đó đạt; SignalRNU1903/EF conflict warnings còn tồn tại.

## Bảng claim → bằng chứng → giới hạn

| Claim có thể dùng trong khóa luận | Bằng chứng cụ thể | Giới hạn bắt buộc đi kèm |
|---|---|---|
| Dữ liệu đủ nhóm đã khai báo |[Data report](../adaptive-confirmatory-data-20261108/REPORT_VI.md), sáu gate/ZIP receipts cùng thư mục|Gate dữ liệu không chứng minh simulator đại diện người thật|
| R chưa hơn B1 đủ practical gain |[Ranking summary](../adaptive-confirmatory-ranking-20261108/summary.json): main_practical_gate, scenarios.main.delta_R_B1_ndcg|Engineering threshold, không significance hoặc satisfaction|
| Tác động khác nhau theo history |[Ranking summary](../adaptive-confirmatory-ranking-20261108/summary.json): datasets[].methods.*.cohorts; TABLES_VI|Cross-sectional cohorts; không causal learning curve|
| Router nhạy chất lượng topics |[Replay summary](../adaptive-replay-20261108/summary.json): scenarios.main.datasets[].modes; [report](../adaptive-replay-20261108/REPORT_VI.md)|Fixed events/selection, paired users; cohort có thể đổi|
| Có thể tách threshold khỏi joint config bằng ablation |[Replay report](../adaptive-replay-20261108/REPORT_VI.md), R-no-routing|Hiệu số replay, không causal effect|
| Refit không được cộng trùng label |[Cost summary](../adaptive-cost-audit-20261108/summary.json): runs[].unique_refits|Threshold không là cache key; state bytes không peak RAM|
| Export giữ GUID và replay ranks |[Export provenance](../adaptive-serving-export-20261108/provenance.json), manifest/archive và [report](../adaptive-serving-export-20261108/REPORT_VI.md)|250sample; không full5k hoặc full model deployment|
| Lifecycle an toàn trên fixture đã kiểm |[HTTP summary](../adaptive-http-20261108/summary.json): final fresh/stale runs; [report](../adaptive-http-20261108/REPORT_VI.md)|Không mọi SQL, actual empty learned row hoặc production proof|
| Direct nhanh trong lab nhưng gateway gate fail |[HTTP summary](../adaptive-http-20261108/summary.json): direct/gateway/restarts/memory|Không lấy429 làm latency feed hoặc5starts làm p99|
| Có khoảng trống chi phí lịch sử |[Cost summary](../adaptive-cost-audit-20261108/summary.json): missing|Null là chưa đo, không0 hoặc số ước lượng|

## Chỉ mục tái lập và thứ tự đọc

1. Đọc protocol/config `tools/oasis-smoke/configs/recommenders/adaptive-v1.json`,
   contracts và [runner](../ADAPTIVE_RUNNER_20261108.md). Source inventories và
   lock130packages xác định phiên bản từng pha; không dùng HEAD mới thay nguồn
   archived. Ranking source e72dcd3, replay source e5c23f0.
2. Kiểm sáu dataset manifests/ZIP receipts ở data report. Raw nằm solution
   `.tools/datasets/adaptive-confirmatory-{main|sparse}-{20261111|20261112|20261113}`.
   Archive datasets nằm `.tools/dataset-archives`; xem tên/hash cụ thể trong
   từng receipt, không tái tạo đè thư mục cũ.
3. Ranking: `batch.json` giữ CLI; `selection.json` từng dataset giữ lựa chọn
   validation; `report.json`, `tuning.json`, `manifest.json` và `audit.json`
   cùng thư mục ranking giữ results, search và kiểm toán per-user từ ranked IDs.
   Raw ZIP225members SHA256
   `d9c113d50ad147db42b092f990b46a3879d2a65cca3f082fe0b5f467629aa102`.
4. Replay: batch/source-inventory/audit/archive và [replay contract](../ADAPTIVE_REPLAY_20261108.md).
   Giữ preflight-failed-01; lỗi đường dẫn trước fit không phải failed model run.
   Raw modes giữ primary/current cohorts, overlap và transitions để đọc paired
   comparisons. Không rerun để tìm kết quả đẹp.
5. Export và HTTP: provenance/manifest/archive ở hai thư mục tương ứng. HTTP ZIP
   SHA256 `98559218e45e2a840a14e36b19668d6d3aa8aa820d8607797e3927395fd5ed41`.
   Raw logs,1000samples/method/transport, inventories máy/source/binary và cleanup
   nằm archive; giữ1000user export failure. Không coi raw/model đã kèm Git.
6. Cost audit: summary giữ223source evidence hashes, từng refit key và missing
   measurements; [report](../adaptive-cost-audit-20261108/REPORT_VI.md) giữ CLI.
7. Từ solution directory, chạy tổng hợp không model:
   `& document/benchmarks/adaptive-thesis-evidence-20261108/build-evidence.ps1 -Check`.
   Bỏ `-Check` để tái sinh TABLES_VI rồi review diff. Kiểm này xác nhận số học và
   drift của bảng/hashes, không thay independent raw per-user/ZIP audits đã có.

Bảng theo cohort được dùng thay đường cong tăng dần theo lịch sử: nối các nhóm
khác người thành một learning curve sẽ dễ tạo diễn giải sai. Gói hiện giữ bảng
đầy đủ để người đọc thấy cả số mẫu và các đối chứng.

Raw backup tồn tại trên workspace, chưa có bằng chứng mirror độc lập hoặc gói
phát hành public tự chứa mọi dependency. Người tái lập phải có đúng archives
được chỉ mục và quyền dùng dữ liệu; hash không bảo đảm file sẽ luôn được lưu.

Kiểm chứng M5: `build-evidence.ps1 -Check` PASS; thay thử displayed metric trong
bảng bị từ chối với lỗi drift rồi khôi phục đúng bytes và kiểm PASS lần nữa.
30liên kết tài liệu cục bộ của báo cáo và milestone index đều resolve; diff
whitespace check PASS. Không thay frozen results hoặc implementation. Kiểm
consistency chỉ tái tính từ JSON tổng hợp; independent per-user/ZIP audits của
các milestone trước vẫn là bằng chứng riêng, không được gọi là vừa chạy lại.

## Khuyến nghị thực dụng và phạm vi kết luận

Giữ B1 làm đối chứng thực nghiệm và chưa có cơ sở nâng R thành lựa chọn mặc định.
Cold topics/general là cơ chế khởi đầu có thể kiểm tra, nhưng không hứa lợi ích
khi người dùng chọn sai topics; warm frozen ranks cần rebuild để nhận lịch sử
và item mới. Không suy từ điểm thấp/cao rằng người thật sẽ thích hay không thích.
Không triển khai router chỉ vì code đã hoạt động.

Đề tài nên trình bày như nghiên cứu điều kiện áp dụng: protocol arrival-aware,
đối chứng cùng eligibility/targets, threshold selection validation-only, phản ví
dụ về topics, và khoảng cách giữa offline quality với serving có giới hạn.
Kết quả âm tính là kết luận chính, không phần phụ phải che bằng latency PASS.
Nếu nghiên cứu tiếp, cần protocol mới cho telemetry training và đánh giá người
thật/exposure; không dùng cùng holdout để chọn thêm phương pháp rồi gọi là
confirmatory. Đây là hướng tiếp theo chưa được thực hiện, không kế hoạch tự
khởi chạy thí nghiệm để cứu kết quả hiện tại.
