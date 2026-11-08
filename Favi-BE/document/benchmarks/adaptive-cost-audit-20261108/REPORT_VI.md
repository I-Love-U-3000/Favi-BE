# Kiểm toán chi phí gốc adaptive — 08/11/2026

Đã đối chiếu sáu manifest gốc, toàn bộ file thành viên, report/selection/tuning
tracked, log và provenance exporter. Không chạy lại model, search, relevance
hoặc HTTP. Chi phí pipeline tổng và refit có bằng chứng; thời gian search riêng
và peak RAM huấn luyện gốc **không được đo**, không thể phục hồi từ log hiện có.
Đây là hoàn tất kiểm toán bằng chứng, không phải PASS toàn bộ resource gates.

## Chi phí lịch sử có thể xác nhận

| Scenario | Seed | Tổng lượt gốc (giây) | Refit duy nhất | Tổng refit (giây) |
|---|---:|---:|---:|---:|
| main | 20261111 | 2040.177213 | 5 | 20.754938 |
| sparse | 20261111 | 2060.176479 | 5 | 16.576502 |
| main | 20261112 | 2160.197929 | 8 | 96.866821 |
| sparse | 20261112 | 2280.327203 | 5 | 15.231184 |
| main | 20261113 | 2060.175018 | 5 | 20.842579 |
| sparse | 20261113 | 2040.181442 | 5 | 29.363474 |
| Tổng | | **12641.235283** | **33** | **199.635498** |

Tổng sáu lượt khoảng 3 giờ 30 phút 41 giây. Đây là elapsed của từng child từ
trước Popen đến sau polling và kiểm hash, gồm data gate, lock validation, search,
refit, test evaluation, diagnostics và I/O. Batch polling20 giây có thể thêm gần
20 giây/lượt; sau polling còn source/manifest readback. Không bao gồm generation
dataset, toàn bộ batch preflight, robustness replay hay export/HTTP sau đó.
Không dùng tổng này làm riêng training time hoặc CPU time.

Runner khóa threadpool4, LightGCN CPU/deterministic; máy phục vụ được ghi tại
HTTP summary. Không có telemetry tải/điện/nhiệt lúc sáu lượt gốc để kết luận
hardware isolation, điện năng, giá tiền hoặc so runtime công bằng giữa family.

## Search và refit khác nhau thế nào

Search trên prefix<300:4 EASE fits,3 PPR fits,4 LightGCN trajectories; mỗi fit
đánh giá thresholds1/5, LightGCN kiểm mỗi10epochs với dual patience. Formula V2
có12trials. `tuning.json` giữ số records và terminal epochs của cả4trajectories:

| Scenario | Seed | Validation records | Terminal epochs grid0/1/2/3 |
|---|---:|---:|---|
| main | 20261111 | 196 | 300/130/300/120 |
| sparse | 20261111 | 206 | 300/190/300/110 |
| main | 20261112 | 190 | 300/110/300/110 |
| sparse | 20261112 | 184 | 300/140/240/110 |
| main | 20261113 | 192 | 300/120/290/120 |
| sparse | 20261113 | 200 | 300/140/230/200 |

Đây là ngân sách thực thi và dấu vết search, không phải số giây từng family.
Log progress không timestamp; `tune` không đặt timer cho search/validation fit.
Không lấy tổng elapsed trừ refit rồi gọi phần dư là training/search: phần dư
còn evaluation, diagnostics, gate, I/O và polling chưa đo riêng.

Refit trên prefix<400 chỉ dùng selection đã lưu trước test, exact selected epochs.
Timer bắt đầu ngay trước `refit(...)`, kết thúc khi model trả về; không gồm xây
`InteractionData` chung, tạo feed, evaluation hoặc serialization. Các refit
LightGCN dùng cả3optimizer seeds đã khóa, không lấy best test seed.

Cache key gốc là `(family, JSON parameters sorted, optimizer seed)`; threshold
và label không nằm trong key. B1/R/family có thể dùng chung model và lặp lại
cùng `refit_seconds` trong report. Script kiểm các dòng dùng chung phải khớp
seconds/model-state, sau đó chỉ cộng mỗi key một lần trong từng dataset.
Không chia sẻ cache xuyên dataset. Main12 có hai cấu hình LightGCN khác nhau,
nên phải giữ sáu fits LightGCN thay vì gộp hết theo tên family.

## Model state không phải peak RAM

`ScoreModel.state_bytes()` cộng NumPy weights hoặc embeddings và ba CSR arrays
`x.data/x.indices/x.indptr`. Một model EASE/PPR refit báo32515620–32524892byte;
LightGCN báo2035660–2057308byte trên sáu datasets này. Đây không phải file model,
managed heap hay resident process RAM.

Không bao gồm Python dictionaries/events/indexes, trainer temporaries, dense
Gram/precision/solve workspaces, PyTorch optimizer/gradients, runtime/libraries,
allocator hoặc OS overhead. Các models refit chung `InteractionData`; cộng
`state_bytes` giữa model khác key còn lặp CSR arrays chung. JSON giữ sum/max
để audit số học, **không coi sum đó là physical cache allocation**, cũng không
lấy max một model làm peak RAM cả pipeline. Không có peak RSS/working-set
sampler gốc; không thể suy peak từ state bytes hay RAM API sau này.

## Export, load và serving là các phép đo riêng

| Pha | Bằng chứng | Giới hạn |
|---|---|---|
| Export refit main11 | EASE1fit0.365270s, state32521008byte | Refit mới cho export, không phải search gốc |
| Export/replay | 1.132263s | Có rank/GUID/replay/encode; không HTTP |
| Export tổng | 12.125867s | Timer trong exporter; không coi là toàn bộ CLI/ghi manifest cuối |
| Artifact | 11867251byte,250users | Không full5k;1000user attempt bị chặn16MiB, giữ nguyên |
| Load diagnostic trước HTTP | 355.0113ms một lần | Checksum/parse/validation, không distribution |
| Fresh start→health | 5processes,2422.4788–3073.2132ms | Gồm host/DB/JIT/load; không disk-cache-cold |
| API peak working set cuối workload | 258326528byte | Resident API process, không training/fullstack RAM |
| Direct p95 B1/R | 57.2082/57.4861ms | PASS lab10concurrent,100warmup+1000measured/method |
| Gateway measured | B1:1000×429; R:999×429+1×200 | **FAIL**, giữ150/minute,burst30;429 không là feed latency |

Artifact bound<=1000users/16MiB/300candidates/user-method giữ nguyên. Schema2
chỉ xuất saved B1/R; không phải serialized toàn bộ training models. API startup
không chạy search/refit. Không cộng các timing này thành một end-to-end cost
vì chúng có ranh giới đo và thời điểm thực thi khác nhau. Không có đủ dữ liệu
để quy thành chi phí tiền, rebuild cadence hay throughput production.

## Quyết định về replay và giới hạn kết luận

Không chạy cost-only replay ở milestone này. Nhiệm vụ là xác định bằng chứng
lịch sử; một replay mới không phục hồi thời gian từng pha/peak RAM quá khứ.
Nó chỉ phù hợp khi cần một thí nghiệm profiling mới được ghi rõ là phép đo mới,
với source/config/selection/prefix/optimizer không đổi, output mới và telemetry
định nghĩa trước; không dùng nó thay số lịch sử hoặc lựa chọn lại model theo test.

Không sửa frozen runner để thêm telemetry vào bằng chứng đã chạy. Không retune,
đổi seed/source/gates, ghi đè failed1000user export hay rerun HTTP đã đủ bằng
chứng. Primary practical relevance **FAIL**, robustness noisy/independent và
no-topics âm giữ nguyên. Gateway FAIL; chưa có multi-replica/TLS/SignalR proof.
Toàn bộ kết luận vẫn synthetic/sample-only, chưa chứng minh trải nghiệm người thật.

## Tái lập kiểm toán và kiểm thử

Từ solution directory, dùng output mới để tránh ghi đè:

```powershell
.tools/oasis-env/Scripts/python.exe -X utf8 -m unittest discover -s tools/oasis-smoke -p test_adaptive_cost.py -v
.tools/oasis-env/Scripts/python.exe -X utf8 tools/oasis-smoke/audit_adaptive_cost.py --output <fresh-summary.json>
```

Hai tests PASS: cache chia sẻ qua threshold/label, optimizer và parameters khác
phải tách, shared cost mâu thuẫn hoặc số âm/NaN/Infinity bị chặn. Actual audit
PASS trên tất cả member sáu manifests, report/selection/tuning tracked và export
manifest/artifact,223file nguồn bằng chứng. Phép cộng cache độc lập bằng
PowerShell cũng PASS cả6datasets, so key/count/tổng với JSON ở tolerance1e-9s.
Script chỉ stdlib, không import trainer hoặc đọc lại test để
chọn cấu hình. [summary.json](summary.json) giữ từng refit, nguồn SHA256 và null
cho missing measurements. SHA256 xác nhận đúng bytes đã giữ, không chứng minh
số telemetry chưa từng đo. Không cần chạy lại111numerical tests hay HTTP vì
không thay các implementation đó.
