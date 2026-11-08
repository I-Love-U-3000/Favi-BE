# Revision tổng thể: đủ scenario trước benchmark

Ngày 05/11/2026. Trạng thái: kế hoạch thực thi thay thế cách bổ sung từng cohort.
Không có kết quả mới hoặc cam kết mọi simulation ngẫu nhiên sẽ đủ mẫu.

Implementation update: lifecycle-v2 config, informative/noisy/independent
sidecars, sparse-topic content cap và scenario-aware analyzer đã được thêm.
Pilot main/sparse 5k đang được đo; trạng thái kết quả sẽ ghi trong milestone
report, không suy ra PASS từ sự tồn tại của implementation. Noisy thay ceil(n/2)
slots vì n chỉ 1 hoặc 3; đây là cách làm tròn minh bạch của mức 50% dự kiến.
Population bootstrap có 13 creators trong seed pilot, không thay role/arrival
sau khi xem labels. Chưa hoàn tất formula parity hoặc confirmatory freeze.

## Vì sao các lượt trước còn thiếu

Chúng ta tối ưu từng thiếu hụt: chạy dài tạo warm, arrival sát cutoff tạo cold.
Chưa phân bổ population đồng thời cho transition và chưa sửa gate khi tách C3.
Một thiếu hụt khác: topic khai báo hiện ngẫu nhiên độc lập sở thích, nên dù đủ
cohort cũng chưa đo đúng giá trị onboarding có thông tin. Tests plumbing pass
không tương đương thiết kế nghiên cứu hoàn chỉnh.

## 1. Khóa ma trận và trách nhiệm từng scenario

| Scenario | Dữ liệu bắt buộc | Gate |
|---|---|---|
| Main lifecycle | C0/C1/C2, T1/T2, W1/W2 | >=30 users có target hợp lệ mỗi nhóm tại cả cutoff 300 và 400 |
| Sparse-topic | C3 với 0–9 bài khớp hợp lệ; có bài general để fallback | >=30 C3 có target mỗi cutoff; feed có thể lấp khi general đủ |
| Onboarding robustness | informative, noisy, independent | Cùng lịch sử/candidates/targets trong so sánh thay preference, ghi match rate |
| Serving failures | user/item mới, artifact missing/stale, đổi quyền sau export | Mọi fixture bắt buộc pass đúng route, quyền và fallback |

Gate main không đòi C3 xuất hiện trong catalog cân bằng. Gate tổng là AND của
main và sparse-topic cùng các kiểm tra integrity; báo rõ fail ở scenario nào.
Robustness và serving là experiment/gate riêng, không nhân thành mọi tổ hợp.
Ngưỡng 30 là minimum kỹ thuật, không phải chứng minh đủ statistical power.

## 2. Thiết kế population đồng thời

Pilot chính 5.000 users, 2.000 posts, 500 ticks, catalog 10 topics, seed pilot
20261110. Cấp cohort theo mục đích thiết kế arrival, không theo nhãn tương lai:

- 30% bootstrap: tạo nhóm warm; bảo đảm creators bootstrap bằng policy explicit,
  lưu phân phối role theo wave và kiểm tra lệch selection.
- 15% đến tick 299 và 15% đến tick 399: giữ cold như pilot đã thành công.
- 20% đến trước cutoff 300 ở các độ trễ 10/30/60 ticks, chia đều.
- 20% đến trước cutoff 400 ở các độ trễ 10/30/60 ticks, chia đều.

Đây là candidate design cần pilot kiểm chứng, không gán nhãn chắc chắn T1/T2
từ thời điểm arrival. Topic counts 0/1/3 phân bổ cân bằng theo seed độc lập
role; cùng user có thể chuyển nhóm giữa các cutoff. Không xóa likes, ép positive
count, chọn user dựa trên future targets hoặc thêm likes chỉ để đủ gate.

Trước native run, chạy dry-run lịch arrival/topic/role và projected workload.
Sau run, kiểm tra toàn ma trận cùng lúc. Nếu thiếu, chỉ sửa allocation/activity
trên pilot và tạo policy revision mới. Giới hạn hai candidate revisions; nếu
vẫn thiếu, báo rõ thiết kế không đạt và chọn thêm request cutoffs theo protocol
revision, không tiếp tục vá vô hạn hoặc âm thầm hạ gate. Multi-cutoff phải tránh
coi nhiều request cùng user là mẫu độc lập và công bố aggregate theo user.

## 3. Sở thích có thông tin và scenario thiếu bài

Informative: topic khai báo ánh xạ top latent interests tại arrival, trước mọi
future feedback; snapshot latent phục vụ generator provenance, không xuất làm
feature recommender. Đây là giả định synthetic self-report chính xác, không
phải thông tin khảo sát thật. Noisy: thay đúng 50% topic slots bằng topic ngoài
tập đã chọn, deterministic; independent giữ cách chọn hiện tại. Lưu realized
overlap, thời điểm và version của từng preference.

Robustness bắt đầu bằng counterfactual preference sidecars trên cùng event log,
vì exposure hiện không dùng declared preferences. Đây chỉ đo ranking replay;
nếu sau này exposure phụ thuộc declared topics thì phải sinh data riêng và ghi
confounding, không gọi sidecar replay là tác động hành vi.

Sparse-topic là config riêng: dành một topic làm rare, giới hạn toàn catalog
của topic đó tối đa 9 bài; các topic khác đủ general candidates. Sinh declared
preferences và content schedule trước labels; không sửa event log sau chạy.
Giữ target generation tự nhiên và report cả cold users không có target. C3
phải đo relevance lẫn khả năng fallback; không yêu cầu target phải khớp topic
khi bài matching không tồn tại. Tags phải là observed metadata có provenance,
không giả định adapter hiện đã import PostTags.

## 4. Một gate duy nhất kiểm tra đủ từ đầu đến cuối

Audit read-back bắt buộc xác minh manifest/harness/config/lock hashes, arrival
policy, 100% referential integrity, thứ tự timestamps, actor và follow target
đã xuất hiện, item existence, exposure trước feedback, topic catalog và tags.
Chỉ preferences có trước request mới được dùng. Cùng methods phải có candidate,
user và target fingerprints giống nhau; report số loại theo từng lý do, không
gộp mọi loại thành một số không giải thích.

Mỗi ô scenario/cohort báo: tổng users, có target, không target, số targets,
exclusions theo lý do và PASS/FAIL/N/A với lý do. Không bỏ ô rỗng. N/A chỉ cho
ô ngoài thiết kế; ô bắt buộc không mẫu phải FAIL. Unit/integration tests phải
có future preference/item, arrival boundary, missing cohort, duplicate ID,
corrupt manifest, target eligibility và C3 ưu tiên phân loại.

## 5. Thứ tự chạy có điểm kết thúc rõ

1. Implement config có version, main/sparse gates, informative/noisy metadata,
   analyzer độc lập và tests; cập nhật protocol trước confirmatory data.
2. Dry-run toàn thiết kế, rồi native pilot main và sparse-topic. Chỉ dùng số
   lượng/integrity sửa pilot; chưa dùng model scores để sửa dataset.
3. Khi tất cả gate pilot đạt, khóa source/config/search grid, formula parity
   C#/Python, literature limitations, budget và cách aggregate. Chọn 3 seed
   confirmatory chưa dùng: 20261111/12/13 nếu chúng vẫn chưa bị quan sát.
4. Sinh cả main/sparse bundles cho từng seed, read-back ZIP và lưu inventories.
   Seed lỗi/thiếu mẫu phải giữ lại, report; không thay bằng seed đẹp.
5. Chạy baseline/router với validation-only tuning và test đã khóa; báo cả
   kết quả âm tính, robustness và chi phí. Không gọi mức tăng là causal effect.
6. HTTP lifecycle/fallback và artifact limits; đóng gói report/source/results
   theo milestone có thể commit/revert, lưu raw ngoài Git.

## Báo cáo hoàn thành cần trả lời được

Đủ nhóm nào, thiếu nhóm nào? Dữ liệu có chủ đích synthetic ở đâu? Đối chứng có
nhận cùng tín hiệu không? Cold/topic giúp bao nhiêu? Khi nào chuyển warm có ích?
Điều gì không thắng? Chi phí phục vụ bao nhiêu? Mỗi câu nối bảng và raw hash.
Chỉ ghi "dataset đủ để benchmark" khi toàn gate thiết kế đạt; chỉ ghi "method
tốt hơn" khi đã có kết quả tương ứng. Không yêu cầu user review từng bước.
