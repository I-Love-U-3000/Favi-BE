# Bounded adaptive export — 08/11/2026

Đã xuất schema2 từ confirmatory main seed20261111,250users đầu tiên theo source
ID đã arrival; B1/R cùng EASElambda1000 nhưng threshold1/5 đúng selection gốc.
Không tuning, thay seed hoặc sửa frozen source. Preflight manifest/source/lock/
dataset gate/prefix/onboarding PASS; refit top10 khớp nguyên bản cho mọi exported
target user của cả B1/R. Original GUID maps được giữ. Output manifest readback PASS.

Artifact11867251bytes, SHA256
7120ed20ae9c8db81bf897411be349581bdc2d6e297f613f04a9986af0a43824.
Refit EASE dùng chung một fit:0.365270s, model state32521008bytes.
Export+replay1.132263s; tổng preflight/refit/export12.125867s.
Đây không phải training/search cost gốc, HTTP load time hoặc process peak RAM.

Lượt1000users trước đó bị chặn vì vượt16MiB, không tạo artifact.json. Giữ nguyên
inputs tại `.tools/feed-experiment/adaptive-export-20261108-main11`; không chạy
đè, không tăng bound. Lượt250 thành công tại cùng parent tên
`adaptive-export-20261108-main11-250`. Cả hai CLI cùng dataset/ranking; chỉ output
và max-users khác. Giới hạn300candidates/user-method giữ nguyên.

Hai unittest cases PASS, gồm ba subcases refit thật EASE/PPR/LightGCN. Fixture
mock data gate/lock/tuner rõ ràng; lượt250 thực tế dùng đủ preflight thật.
Kiểm GUID, rank replay, cache, missing history, prior likes, sample deterministic,
optimizer fixed, tamper/overwrite rejection. `git diff --check` phần thay đổi mới
PASS; warning blank EOF của archive snapshot ở commit79ad77b là lịch sử, không
rewrite archive để sửa cosmetic.

Schema2 chưa được endpoint nạp; production vẫn default disabled. HTTP lifecycle,
privacy/fallback/resource gates và full cost còn pending. Mẫu250users không phải
full5k serving. Primary practical gate FAIL vẫn giữ; không claim cải thiện relevance.
