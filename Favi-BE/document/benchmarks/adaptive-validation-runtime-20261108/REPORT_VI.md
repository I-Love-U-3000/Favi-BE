# Xác thực runtime dưới Python optimization — 08/11/2026

Kết quả phân biệt hai đường. Không thấy bypass do PYTHONOPTIMIZE trong cổng
tái sử dụng/đóng gói dataset ở 42 lượt CLI có kiểm soát. Có lỗi thực tế ở smoke
fixture: assertion kiểm source bất biến bị Python loại bỏ khi optimization bật.
Fault injection gọi adapter thật rồi append bytes vào bản SQLite fixture mới:
trước sửa, optimize0 thoát1, optimize1/2 thoát0 và in PASS. Đây là mô phỏng lỗi
producer, không phải bằng chứng attacker hoặc dataset nghiên cứu đã thay đổi.

`prepare_fixture.py` và `run_simulator.py` nay dùng `if`/`raise ValueError` cho
source hash, export hash, count, graph equality, SQLite integrity và FK checks.
Không thay đổi điều kiện, ngoại lệ DDL upstream được cho phép, generation policy,
manifest format hoặc kết quả nghiên cứu. Cổng C# trước Compose bổ sung kiểm export
bytes; không thay thế kiểm source SQLite, graph semantics và expected ManualAction
counts của Python. Vì vậy các kiểm Python phải hoạt động độc lập optimization.

## Bằng chứng và tái lập

`audit.py` chạy CLI thật check_dataset.py/package_dataset.py với PYTHONOPTIMIZE
0/1/2. Mỗi CLI có một valid control và sáu negative controls: raw hash sai, thiếu
coverage, path escape, config sai, exposure reference sai, report quality khác.
42/42 kiểm PASS; negative packaging không tạo ZIP hoặc receipt. Fixture manifest
này dùng opaque bytes cho oasis.db/export: audit chỉ chứng minh các nhánh reuse
CLI được thử, không chứng minh SQLite, SQL, snapshot hoặc full referential integrity.
Source hash của ba module được lưu trong receipt ignored
`audit.json` trong thư mục báo cáo; raw logs tại
`.tools/validation-runtime-final-20261108`. Mỗi negative đòi đúng thông báo lỗi
dự kiến, không chỉ exit khác0.

Regression `test_validation_runtime.py` gọi entrypoint fixture thật trong subprocess,
không kiểm text source hoặc giả lập chính điều kiện cần thử. 15 controls PASS:
mỗi mức optimization chạy một valid fixture cùng bốn faults riêng (source mutation,
SQL checksum, graph count và graph edge equality sau khi cập nhật CSV checksum).
Faults đều thoát khác0, có thông báo tương ứng và không in PASS.
Đây là một unittest với15subcases, không báo thành15unittests.
Raw trước sửa: `.tools/fixture-runtime-before-{0,1,2}.log`; regression sau sửa:
`.tools/validation-runtime-tests.log`.

Hai integrity tests và chín policy tests hiện hữu cũng PASS dưới optimization2.
ManualAction upstream thật chạy thành công tại
`.tools/simulator-runtime-20261108`:42runtime files/130locked packages,
3users/3posts/3likes/1follow/1comment, source/export checks đạt. Fault injection
ManualAction với optimization2 sau sửa bị chặn đúng `ValueError: Adapter changed
source SQLite`, exit1, không provenance thành công, log
`.tools/simulator-runtime-fault-final-20261108.log`. Một attempt trước của fault
harness thất bại ở import `datasets` vì chính harness đặt .tools lên sys.path;
không tính attempt đó là bằng chứng gate. Đã loại đường dẫn thêm này khỏi harness
để giống entrypoint thật rồi chạy control mới, giữ nguyên log attempt cũ.
Không inference hoặc model training; đây là native ManualAction integration.

```powershell
.tools/oasis-env/Scripts/python.exe -B document/benchmarks/adaptive-validation-runtime-20261108/audit.py .tools/validation-runtime-new
.tools/oasis-env/Scripts/python.exe -B -m unittest discover -s tools/oasis-smoke -p test_validation_runtime.py -v
```

Rà soát source generator cho thấy kiểm temporal graph/catalog/comments, quality,
source immutability và native audit dùng explicit raises trước dataset manifest.
Wrapper run-dataset.ps1 chặn exit khác0. Đây là source review, không phải rerun
generation hoặc bằng chứng bao phủ mọi lỗi/biến môi trường. Không thay frozen
check_dataset.py/source archives/inventories, dataset, artifact hoặc holdout scores.
Không sửa audit lịch sử dựa assertion; audit_readiness.py đã có isolation riêng.
Không claim chống PYTHONPATH injection hoặc chống sửa đồng bộ manifest.
