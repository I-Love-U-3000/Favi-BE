# Offline adaptive feed contract

Ngày 08/11/2026. Implementation: `tools/oasis-smoke/adaptive_feed.py`.
Contract mới cho experiment, không sửa kết quả benchmark lịch sử.

## Thông tin được nhìn thấy

Tại request tick t chỉ dùng events tick <t, user arrival <t và preference tick
<t. Metadata topic trong post event là observed single tag của synthetic corpus;
không dùng latent interest làm feature. Mỗi post có đúng một tag cho Formula
trong track mới. Điều này không khẳng định tag đã import vào production DB.

Tập eligible chung gồm post đã tồn tại trước t, loại bài của viewer và prior
positive của viewer. User chưa arrival không có candidates. Mọi method cùng
candidate/target fingerprints, dù từng method có bước retrieval riêng. Targets
là distinct held-out likes trong [t,end) giao eligible; không target là N/A.
Phân nhóm dùng chung `classify_cohort` với read-back data gate để tránh drift.

## Cold pool và ranking

Giả định production default graph source rỗng. Xếp eligible theo likes +
2*comments giảm dần, created tick giảm dần, source ID tăng dần. Mỗi topic chọn
top20 và gán score -index; general top60 score -100-index. General topics theo
đúng thứ tự production catalog khi user chọn rỗng. Pool duplicate được đưa vào
ranker đã parity: ưu tiên highest score, round-robin topic, soft author cap2,
relax rồi general fallback. Không trộn raw graph scores với popularity.

Các source IDs được ánh xạ GUID nguyên dương để gọi reference ranker. Actor
zero-based được cộng một trong miền author; đây là mapping experiment, không
phải GUID export production. Canonical source-ID order làm input tie order
deterministic. Production SQL tie ordering vẫn cần PostgreSQL-specific test;
không gọi Python pool adapter là production end-to-end parity.

Pool bounded có thể thiếu relevant target mà full catalog có: đó là kết quả
retrieval thực dụng cần báo, không thay target denominator để làm đẹp điểm.
Raw Popularity giữ toàn eligible để so sánh; không ép tất cả methods dùng cùng
top60 vì như vậy sẽ đổi câu hỏi nghiên cứu.

## Warm và routing

Formula dùng công thức đã source parity với comments/follows/mutual/recent
author likes và observed tag count. Tick tương đương một giờ; recent window
30 ngày theo clock đó. Likes-only là giới hạn của dataset, không phải toàn bộ
reaction semantics production. Self candidates đã bị loại nên self bonus
không phát huy trong offline contract, dù được kiểm tra ở parity fixture.

0 positives dùng cold cho system methods. Formula chuyển ở 1 positive; topic-only
giữ cold ranker với toàn bộ users; raw Popularity không dùng topic. Collaborative
system dùng cùng cold fallback dưới ngưỡng 1 hoặc 5, bên trên dùng warm model
có cùng cutoff. Router chưa được chọn bằng kết quả pilot baseline.

## Kiểm chứng và phạm vi

4 adapter tests kiểm tra topic priority/fallback, candidates chung, future item/
preference/arrival, own/prior-positive exclusion, route requirements và general
pool cap60 trong catalog80. Primitive ranker có 524 C#/Python checks riêng.
Full Python regression trước test cap bổ sung: 101 passed; test cap bổ sung pass.
Pilot evaluator yêu cầu scenario data gate PASS và kiểm tra denominator từng
cohort bằng đúng data gate. Không retune bằng test; baseline pilot là exploratory.

Không có DB/API changes, HTTP authorization verification mới, model superiority
claim hay causal engagement estimate trong milestone này.

Implementation 08/11/2026: common-candidate collaborative/router runner và
Formula V2 đã được thêm; xem benchmarks/ADAPTIVE_RUNNER_20261108.md.
V2 giữ zero-tag historical semantics; observed tag count1 chỉ áp dụng V1.
Runner chọn validation-only, persist selection trước test và refit theo epoch
đã khóa. Không đổi production reader hay các kết quả benchmark lịch sử.
