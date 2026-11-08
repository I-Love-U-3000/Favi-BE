# Rà soát khả năng bảo vệ và tái lập bằng chứng

Ngày 08/11/2026. Phạm vi là kiểm tra độc lập phần tổng hợp M5 và thử khôi phục
bằng chứng; không tạo kết quả recommendation hoặc HTTP mới. Kết luận thực dụng
vẫn là **chưa đủ cơ sở thay B1 bằng R**. Đây không phải xác nhận toàn bộ khóa
luận đã sẵn sàng bảo vệ hoặc hệ thống đã sẵn sàng production.

## Những điểm đã xác định và xử lý

1. Script snapshots trong thư mục `scripts/` dùng `Path(__file__)` để suy ra
   input/output. Chạy ngay từ thư mục tài liệu sẽ tìm sai đường dẫn. Ranking
   `run_batch.py` còn ghi `batch.json` trước preflight; các summarizer/auditor
   cũng ghi summary/audit. Vì vậy không chạy các script này trên bản bằng chứng
   gốc. Hướng dẫn dưới đây khôi phục bản archived vào một cây riêng rồi chỉ
   chạy summarizer/auditor stdlib; không chạy batch, exporter hoặc HTTP.
2. Các ZIP có gốc khác nhau. Ranking không có thư mục bao ngoài; replay có cả
   thư mục successful và `preflight-failed-01`; export chứa hai tên lần thử;
   HTTP bắt đầu bằng `.tools/`. Không giải nén mọi ZIP vào cùng một đường dẫn.
3. HTTP ZIP giữ logs, reports, metadata và script tổng hợp. Hash source/binary
   trong run inventory xác định bytes đã dùng nhưng không thay thế mã nguồn,
   binary, SDK, NuGet packages hay Docker images. Export ZIP cũng không tự chứa
   toàn bộ dependency/model pipeline. Gói hiện có hỗ trợ kiểm toán kết quả,
   chưa là bản phát hành tự chứa để tái dựng toàn bộ stack trên máy sạch.
4. Các dòng pending trong protocol/report cũ là snapshot lịch sử. Không sửa
   chúng sau test; đọc trạng thái mới ở [M5](../adaptive-thesis-evidence-20261108/REPORT_VI.md)
   và reports HTTP/cost. Chỉ mục milestone đã được sửa để không chỉ lại M5 như
   công việc chưa làm.

## Khôi phục đúng vị trí và kiểm tra lại

Gọi `S` là solution directory `Favi-BE/Favi-BE`, `R` là thư mục khôi phục mới.
Đây là ánh xạ logic, không phải lệnh tự chạy batch:

| ZIP trong `S/.tools/dataset-archives` | Gốc giải nén trong R | Dependency bổ sung |
|---|---|---|
| `adaptive-confirmatory-ranking-20261108.zip` | `.tools/benchmarks/adaptive-confirmatory-ranking-20261108/` | Chỉ ranked IDs/report/selection/tuning/manifest cho arithmetic audit |
| `adaptive-replay-20261108.zip` | `.tools/benchmarks/` | Ranking cùng cấp; onboarding của sáu dataset ở `.tools/datasets/<dataset>/` |
| Sáu `adaptive-confirmatory-<scenario>-<seed>.zip` | Kiểm tên members trước khi khôi phục | Audit hiện tại chỉ lấy các file `onboarding*.json`; không import DB |
| `adaptive-serving-export-20261108.zip` | `.tools/feed-experiment/` | Dataset GUID mapping và ranking nếu muốn tái export trong nghiên cứu khác |
| `adaptive-http-20261108.zip` | R | Source/binary/dependency/image đúng inventory nếu muốn chạy lại HTTP |

`frozen-source.zip` lồng trong ranking/replay giữ đường dẫn tương đối từ S;
phải khôi phục ở solution root riêng nếu cần thực thi runner. Các file Python
được chép phẳng cạnh per-dataset report không có đầy đủ cấu trúc configs/docs
mà runner mong đợi. Không dùng chúng làm một checkout hoàn chỉnh. Batch JSON
còn chứa absolute paths máy gốc; đổi đường dẫn cho lần chạy mới không làm các
bytes JSON mới khớp hash lịch sử. Giữ evidence lịch sử riêng.

Từ S, chạy kiểm toán có thể lặp lại với một output **chưa tồn tại**:

```powershell
.tools/oasis-env/Scripts/python.exe -B -X utf8 document/benchmarks/adaptive-defense-readiness-20261108/audit_readiness.py --output .tools/defense-readiness/restore-002
& document/benchmarks/adaptive-thesis-evidence-20261108/build-evidence.ps1 -Check
```

Script mới chỉ dùng stdlib. Nó kiểm SHA256/size/count của mười ZIP, đọc mọi
member để kiểm CRC, so member hashes khi receipt cung cấp, so toàn bộ nested
source ZIP với inventory, rồi khôi phục ranking/replay và onboarding vào R.
Sau đó chạy **bản script trong archive**: ranking `summarize.py`, `audit.py` và
replay `summarize_audit.py`. Summary tái sinh phải bằng JSON cũ theo cấu trúc và
giá trị. Không yêu cầu bytes whitespace giống nhau. Không gọi trainer, API,
Docker, batch hoặc generator. Output/logs được giữ trong R; output cũ bị từ chối.
Tiến trình con chạy bằng `sys.executable -I -B`: isolated mode bỏ qua biến
`PYTHON*`, user site packages và script directory trong import path; `-B` tránh
ghi bytecode. Vì audit lịch sử dùng `assert`, chỉ `-B` là chưa đủ: biến
`PYTHONOPTIMIZE=1` hoặc `2` có thể khiến dữ liệu sai vẫn thoát mã0. `-I` giữ
assertions hoạt động ngay cả khi tiến trình cha nhận biến này, đồng thời bỏ
`PYTHONPATH` có thể chèn startup code. Đây là cô lập cấu hình Python, không phải
sandbox hệ điều hành; chỉ chạy archive đã kiểm hash và tin cậy. Không sửa bytes
script trong archive. stdout/stderr luôn được ghi trước khi kiểm exit code.

Kiểm thử độc lập không cần raw ZIP:

```powershell
.tools/oasis-env/Scripts/python.exe -B -X utf8 -m unittest discover -s document/benchmarks/adaptive-defense-readiness-20261108 -p test_isolation.py -v
```

Hai test kiểm môi trường optimize1/2, `sitecustomize` chèn qua `PYTHONPATH`,
AssertionError và log lỗi được giữ, cùng đường chạy hợp lệ dùng stdlib. Workflow
`adaptive-evidence.yml` chạy fixture này trên Windows/Linux với Python3.11;
CI không có raw archives nên không thay phép khôi phục đầy đủ trên workspace.

Phép thử này chứng minh khả năng khôi phục và kiểm lại số học từ raw predictions
cùng metadata đã lưu. Nó không chứng minh có thể tái fit bit-for-bit trên mọi
CPU/BLAS/PyTorch, tái sinh simulator hoàn toàn, hoặc dựng HTTP từ một ZIP duy
nhất. Lock130packages là danh sách phiên bản, không là bản sao package wheels.
Chưa có kiểm chứng trên máy sạch, mirror độc lập hoặc backup public đầy đủ.

## Các câu hỏi phản biện cần trả lời bằng bằng chứng

| Phản biện | Trả lời có thể bảo vệ | Không được suy rộng |
|---|---|---|
| R có tốt hơn không? | Main mean delta +0.000334784, dưới ngưỡng tuyệt đối0.001; primary FAIL | Không đổi metric hoặc lấy cohort thắng thay primary |
| Chỉ thay threshold hay thay cả model? | B1/R joint selection; main12 khác LightGCN config/epoch. Same-config ablation mean +0.000416166 nhưng có seed âm | Không quy toàn joint gain cho routing |
| Có chín lần lặp độc lập? | Ba dataset seeds; optimizer repeats lồng trong từng dataset | Không coi chín optimizer runs là chín cộng đồng hoặc claim significance |
| Topics có tác động nhân quả? | C0 thua Popularity, C1/C2 cao hơn trong simulator; noisy/independent/no-topics có delta âm | Không lấy khác biệt giữa cohort làm causal effect; không đo hành vi mới trong replay |
| Các đối chứng có nhận cùng input? | Cùng eligibility/targets và fingerprints; Formula có follows/comments, V1/V2 khác tag semantics; cold retrieval bounded | Không gọi là mọi thuật toán nhận tín hiệu/pool hoàn toàn giống nhau |
| Điểm thấp có làm đề tài vô ích? | Đề tài xác định điều kiện thất bại bằng protocol và đối chứng, cho quyết định chưa nâng R mặc định | Không tuyên bố người thật không thích, hoặc thuật toán mới chỉ vì ghép mô hình |
| Serving chứng minh offline kết quả? | Offline likes-only khác live Like/Love UNION save; live count route/filter, scores không retrain | Không dùng HTTP PASS thay relevance; schema2 thực tế chỉ250users |
| Có đạt performance và tài nguyên? | Direct p95 đạt; gateway FAIL429; start→health và API working set có ranh giới đo riêng | Không coi429 là feed latency hoặc state bytes là training peak RAM |
| Tái lập được đến đâu? | Có thể khôi phục raw predictions và tính lại summary trên workspace với archives đã chỉ mục | Không claim clean-machine/full-stack reproducibility hoặc public package tự chứa |

Nguồn số liệu và đường dẫn claim cụ thể nằm ở
[M5 claim→evidence](../adaptive-thesis-evidence-20261108/REPORT_VI.md),
[bảng sinh từ JSON](../adaptive-thesis-evidence-20261108/TABLES_VI.md),
[frozen protocol](../adaptive-protocol-v1.md),
[cost audit](../adaptive-cost-audit-20261108/REPORT_VI.md) và
[HTTP report](../adaptive-http-20261108/REPORT_VI.md).

## Kết quả kiểm chứng lượt này

[Receipt máy đọc](summary.json): PASS mười ZIP/974members, nested source
62+65files; ranking78records và replay348records đều khôi phục và audit đủ sáu dataset. Summary
tái sinh bằng bản archived và bản tracked mà M5 sử dụng theo giá trị JSON.
Lượt cuối nằm `.tools/defense-readiness/restore-002`, giữ ba logs audit; lượt
`restore-001` giữ thử nghiệm ban đầu và `tamper-rejected.log`. Script SHA256
trong receipt áp dụng bytes file lúc thực thi, trước Git newline normalization.

Kiểm thử âm: output đã tồn tại bị từ chối trước khi ghi; sửa delta trong summary
**bản sao** khiến archived ranking audit báo AssertionError, sau đó khôi phục
đúng bytes. Bằng chứng gốc không đổi. M5 consistency PASS;39liên kết cục bộ ở
report này, M5 và milestone index resolve. Không huấn luyện hoặc chạy HTTP lại.

## Khoảng trống giữ nguyên

Rà soát nguồn helper08/11/2026: `tools/oasis-smoke/build_feed_artifact.py` có
trong `frozen-source.zip` của ranking batch. SHA256 bytes archived và hiện tại
cùng `363981e24471210c84499eab492cd6eb14d8eb73b0add39040d6321cda6abcfa`.
Do đó việc helper không thuộc inventory14file mỗi ranking run không có nghĩa
nguồn helper đã thất lạc. Export ZIP vẫn không tự chứa pipeline. Phép kiểm chỉ
đọc không chạy lại model hoặc sửa ZIP; log `.tools/mapping-coverage-actual.log`.
Exporter hiện tại còn bắt buộc checksum hai CSV ánh xạ trong dataset manifest;
xem [serving contract](../../ADAPTIVE_SERVING_CONTRACT.md). Sửa gate hiện tại
không thay bằng chứng export250 hoặc đưa thêm claim tái lập trên máy sạch.

Lượt sửa cô lập tiến trình: [receipt riêng](isolation-summary.json) ghi PASS
mười ZIP/974members và hai audits đủ sáu datasets, summary bằng archived/tracked.
Output `.tools/defense-readiness/restore-003` được tạo mới với môi trường cha
`PYTHONOPTIMIZE=1`; ba logs audit được giữ. Receipt `summary.json` cũ không đổi.
Hai unittest PASS; M5 consistency và LF/CRLF/BOM cùng hai tamper negatives PASS.
Không thay archives, frozen results, seeds, selection hoặc gates.

Không có training peak RAM/search-only timing gốc; không phục hồi bằng chạy mới.
Không có actual R empty learned-history row trong artifact250 để chứng minh
HTTP case đó; mới unit-fixture. Không có full5k serving, multi-replica, TLS,
SignalR, người dùng thật hoặc causal engagement evidence. Các PR vẫn tách lớp
và chưa merge; bằng chứng lab không tự cho phép triển khai.
