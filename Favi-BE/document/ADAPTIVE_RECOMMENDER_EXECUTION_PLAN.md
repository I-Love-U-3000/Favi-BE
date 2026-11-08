# Execution plan: gợi ý thích ứng theo trạng thái người dùng Favi

Ngày lập: 04/11/2026. Trạng thái: **kế hoạch, chưa thực thi các bước bên dưới**.

## 1. Chọn hướng và xác định nền tảng

Tên đề tài làm việc: **Thiết kế và đánh giá hệ gợi ý thích ứng theo trạng thái dữ liệu người dùng trong mạng xã hội Favi**.

Câu chuyện sản phẩm rất cụ thể: người mới chưa có tương tác vẫn nhận được feed theo chủ đề đã chọn hoặc feed general; khi tích lũy hành vi, hệ thống chuyển sang cá nhân hóa. Nghiên cứu cần trả lời chuyển như thế nào, khi nào có ích và cái giá phải trả. Đây là hướng có thể demo, đo lường và phản biện bằng dữ liệu. Chưa tuyên bố thuật toán mới hoặc tính mới học thuật; bước đọc nghiên cứu liên quan phải xác định phần đóng góp có thể bảo vệ.

Nền tảng hiện tại đủ để bắt đầu, nhưng chưa đủ để kết luận hệ thích ứng tốt hơn:

| Bằng chứng đã có | Điều được phép kết luận | Khoảng trống |
|---|---|---|
| [Benchmark ba quy mô](THREE_SCALE_RECOMMENDER_BENCHMARK.md) và [ba seed cùng 5k](MULTISEED_RECOMMENDER_BENCHMARK.md) | Pipeline chạy được; EASE, PPR và LightGCN không có một bên thắng ổn định trên mọi seed đã đo | Cùng cơ chế sinh tương tác; không tương đương nhiều cộng đồng thật |
| [MovieLens 100K](EXTERNAL_RECOMMENDER_BENCHMARK.md) | Popularity dẫn toàn tập, PPR dẫn nhóm warm trong protocol đã chạy; thứ tự trên synthetic không chuyển nguyên sang dữ liệu độc lập | MovieLens không có onboarding chủ đề của Favi; cold collaborative chưa phải đánh giá cold feed Favi |
| [Cold-start backend](COLD_START_FEED.md) | Đã có API sở thích và fallback topic/general | Chưa có benchmark relevance cho đúng đường xử lý này; chưa xác nhận UI chọn chủ đề hoàn chỉnh |
| [Feed EASE/PPR thử nghiệm](EXPERIMENTAL_RECOMMENDER_FEED.md) | Artifact thật đã đi qua PostgreSQL/API, kiểm tra quyền và fallback | Fixture 100 users/31 posts; chưa chứng minh phục vụ 5k/10k hoặc tải đồng thời |

Các thay đổi và báo cáo hiện còn trong working tree, chưa commit/push. Tài liệu lịch sử giữ nguyên bối cảnh của từng lượt chạy; kế hoạch này không biến các kết quả cũ thành bằng chứng cho một phương pháp mới.

## 2. Điều cần chứng minh

**RQ1 — Người mới:** topic/general hiện có cải thiện chất lượng feed so với Popularity và công thức Favi trong điều kiện nào? Khi người dùng không chọn, chọn ít hoặc chọn sai sở thích, lợi ích còn không?

**RQ2 — Quá trình chuyển tiếp:** chọn phương pháp theo lượng lịch sử có tốt hơn dùng một phương pháp cố định với cùng cold fallback không? Chuyển ngay sau positive đầu tiên có gây giảm chất lượng không?

**RQ3 — Tính thực dụng:** mức cải thiện có xứng đáng với chi phí training, cập nhật artifact, bộ nhớ và độ trễ HTTP không? Khi model thiếu hoặc hết hạn, feed có tiếp tục đúng quyền và ổn định không?

Giả thuyết chính: một chính sách chuyển tiếp đơn giản có thể cải thiện nDCG@10 so với hệ cố định được chọn bằng validation, đồng thời giữ được chất lượng của từng nhóm người dùng. Đây là giả thuyết cần kiểm tra. Nếu không thắng hoặc chỉ thắng dưới giả định simulator thuận lợi, phải báo cáo đúng điều đó.

Đóng góp dự kiến là protocol đánh giá xuyên suốt cold → ít dữ liệu → warm, phân tích điều kiện thắng/thua và một chính sách triển khai được trong Favi. Không lấy việc tích hợp LightGCN hay thêm router làm bằng chứng tự thân về tính mới.

## 3. Bước 1 — Chốt baseline và protocol trước khi tạo thêm kết quả

Trước hết bảo toàn công việc đã có: ghi commit nền, diff/source hash cho phần chưa commit, dependency lock, config, manifest dữ liệu và vị trí raw artifact. Kiểm tra archive đọc lại được và tái lập một lượt nhỏ. Tách thay đổi theo phạm vi có thể review/revert độc lập; không gộp dữ liệu raw vào Git.

Sau đó viết protocol v1, đóng băng câu hỏi chính, tập phương pháp, tín hiệu đầu vào, cửa sổ thời gian, tiêu chí chọn cấu hình và cách tổng hợp. Đọc và lập bảng nghiên cứu liên quan về cold-start có sở thích khai báo, hybrid chuyển tiếp, đánh giá theo thời gian và exposure bias. Mỗi công trình phải ghi rõ bài toán, tín hiệu, baseline, giới hạn và khác biệt với Favi; không chỉ liệt kê tên paper.

Định nghĩa trạng thái tại thời điểm yêu cầu feed bằng **thông tin đã tồn tại trước thời điểm đó**. Production Favi coi Like/Love/save là positive; benchmark cũ chỉ có likes. Protocol mới phải khai báo adapter tín hiệu, và tách kiểm chứng hành vi Like/Love/save khỏi benchmark likes-only nếu chưa sinh đủ loại hành vi.

Kết thúc bước này có `document/benchmarks/adaptive-protocol-v1.md` và bảng related work, đều là deliverable sẽ tạo. Protocol phải ghi được bằng câu cụ thể: thử giả thuyết gì, đối chứng nào, khi nào bác bỏ. Chốt ngưỡng suy giảm cohort chấp nhận được và ngân sách vận hành trước test, kèm lý do; không tự chọn phần trăm sau khi thấy kết quả.

**Điểm review:** duyệt câu hỏi và phạm vi trước khi chạy batch tốn thời gian. Nếu chưa có cách đo hợp lệ cho một câu hỏi, thu hẹp câu hỏi trước.

## 4. Bước 2 — Tạo dataset thực sự có cold và transition

Giữ các dataset hiện tại làm mốc lịch sử. Tạo phiên bản mới có thời điểm gia nhập, sở thích khai báo có timestamp, tag bài đăng và các cửa sổ quan sát. Người tham gia xuất hiện dần để có người chưa tương tác tại cutoff, thay vì mọi người đều đã warm. Sinh tuần tự, deterministic, giữ phân phối hoạt động lệch; lưu cả chính sách exposure và phản hồi của simulator.

Không lấy latent interest bí mật của agent làm feature của recommender. Nếu dùng nó để mô phỏng lựa chọn onboarding, phải ghi rõ phép ánh xạ và thử cả lựa chọn thiếu/sai. Đây vẫn là sở thích giả lập, không phải khảo sát người thật. Không dùng future likes để dựng sở thích ban đầu. Tag dùng catalog của Favi; việc ánh xạ topic simulator phải công khai và có validator.

Ma trận chính cần đủ nhỏ để giải thích được:

| Scenario | Trạng thái tại cutoff | Điều muốn biết |
|---|---|---|
| C0 | 0 positive, không chọn chủ đề | General có hơn Popularity không? |
| C1 | 0 positive, chọn 1 chủ đề | Một lựa chọn ban đầu giúp được bao nhiêu? |
| C2 | 0 positive, chọn nhiều chủ đề | Có cân bằng được nhiều sở thích không? |
| C3 | 0 positive, chủ đề đã chọn thiếu bài hợp lệ | Fallback có lấp feed an toàn không? |
| T1/T2 | 1 positive / 2–4 positives | Chuyển ngay hay duy trì thông tin topic tốt hơn? |
| W1/W2 | 5–19 / từ 20 positives | Lợi ích của collaborative thay đổi thế nào? |

Các nhóm trên gọi là cohort: những người có chung điều kiện tại cutoff. Tách riêng hai phép kiểm tra phụ: item đã tồn tại nhưng chưa có positive; artifact thiếu/hết hạn. Không nhân mọi biến thành toàn bộ tổ hợp. Chọn sai sở thích, đổi sở thích và biến động exposure là robustness ở bước 5, không mở cùng lúc trong lượt đầu.

Chạy fixture nhỏ trước, sau đó ba seed mới cùng quy mô 5k; khai báo seed trong protocol trước khi sinh. Chỉ mở rộng 2k/10k sau khi pipeline và câu hỏi chính đã có câu trả lời. Không thay seed thất bại bằng seed có kết quả đẹp.

Mandatory Validation Gate phải xác nhận toàn bộ khóa tham chiếu, ID, timestamp, catalog, sở thích hợp lệ, không dùng tương lai và số lượng từng cohort. Ghi số người đủ điều kiện, có target và bị loại theo lý do. Cohort không có target trả N/A, không gán 0 và không bỏ khỏi báo cáo. Nếu cohort quá ít, điều chỉnh thiết kế trên pilot rồi tạo phiên bản mới trước test; không chọn lại dữ liệu dựa trên điểm model.

Đầu ra: dataset v2 bất biến, resolved config, source hash, manifest, ZIP đọc lại được, inventory cohort và tài liệu trong `document/datasets/`; raw nằm trong `.tools`. Các tên/version cụ thể chốt ở bước 1.

**Điểm review:** kiểm tra dataset có đúng vấn đề sản phẩm cần nghiên cứu không. Không chạy model lớn khi dữ liệu chưa qua gate.

## 5. Bước 3 — Đặt đối chứng công bằng và tái hiện công thức hiện tại

Giữ Popularity, Formula V1 hiện tại, Formula V2 đã tuning, EASE, PPR và LightGCN. Trước benchmark, đọc lại code công thức và đối chiếu bản offline bằng fixture chung, cùng đồng hồ và tie-break. Nếu có khác biệt do candidate pool hoặc lọc serving, ghi rõ và sửa phép so sánh; không gọi hai đường xử lý là tương đương khi chưa kiểm tra.

Chia hai câu hỏi để tránh gán lợi ích sai nguồn:

- **So thuật toán trên cùng tín hiệu:** Popularity/EASE/PPR/LightGCN dùng cùng lịch sử likes. Báo cáo cold không có tín hiệu một cách tường minh.
- **So hệ thống dùng được:** Favi hiện tại; các warm model cố định dùng cùng cold topic/general fallback; hệ thích ứng đề xuất. Formula dùng thêm follows/comments và hệ dùng topics phải được ghi rõ. Thêm đối chứng topic-only để biết lợi ích đến từ thông tin onboarding hay chính sách chuyển tiếp.

Không lấy thứ tự hòa điểm của model không có lịch sử làm đối thủ cold duy nhất. Mọi phương pháp trong cùng phép so sánh dùng chung snapshot eligibility, target, loại bài của chính mình/prior positive và chính sách nội dung. Lưu fingerprint quần thể/candidates/targets để phát hiện lệch đầu vào.

Offline full-catalog và HTTP pool hữu hạn là hai phép đo khác nhau. Nếu serving dùng top 300 hoặc giới hạn tác giả, báo cáo ảnh hưởng của bước cắt/lọc; không gán metric offline cho feed đã qua xử lý khác.

Đầu ra: baseline parity report, bảng tín hiệu và fixture kiểm chứng C#/Python. **Điểm review:** công thức hiện có đã được đưa vào đúng nghĩa, và đối thủ có fallback thực dụng.

## 6. Bước 4 — Xây chính sách thích ứng nhỏ, giải thích được

Bắt đầu bằng chính sách đối chứng: 0 positive dùng topic/general; từ positive đầu tiên dùng một warm model được chọn bằng validation; thiếu model quay về fallback đã định nghĩa. Đây là mốc để đo giá trị của việc thay đổi thời điểm chuyển.

Ứng viên đầu tiên chỉ thay ngưỡng chuyển, chọn từ tập nhỏ `{1, 5}` trên validation. Với warm model, chọn từ EASE/PPR/LightGCN theo budget đã đóng băng. Cùng tập dữ liệu và cùng ngân sách chọn model cho đối chứng cố định. Không định tuyến theo kết quả test của từng user.

Chỉ thêm blending nếu kết quả validation cho thấy vấn đề ở vùng chuyển tiếp. Khi đó tạo protocol revision trước test, quy định chuẩn hóa hoặc kết hợp thứ hạng, tie-break và grid hữu hạn. Không cộng trực tiếp score khác thang đo. Tránh mở thêm neural router/LLM trước khi chứng minh ngưỡng đơn giản chưa đủ.

Đầu ra: config chính sách, lựa chọn validation đã hash, mô tả lý do route và unit tests cho các ranh giới 0/1/4/5 positives cùng fallback. **Điểm review:** có thể giải thích một request được chọn phương pháp nào mà không dựa vào nhãn tương lai.

## 7. Bước 5 — Chạy experiment và chủ động tìm điều kiện thất bại

Dùng global chronological split, không chia ngẫu nhiên tương tác. Mỗi cutoff chỉ thấy lịch sử, lựa chọn sở thích và bài đăng đã tồn tại. Bài xuất hiện sau cutoff không được đưa vào slate lúc đó; báo cáo target bị loại và đánh giá chúng ở cutoff sau nếu có. Các cửa sổ của cùng dataset không phải dataset độc lập.

Tuning chỉ trên validation; lưu lựa chọn trước test. Những lượt test cũ đã xem chỉ là bằng chứng thăm dò. Nếu sửa phương pháp dựa trên test mới, lượt đó cũng trở thành exploratory và cần một holdout mới để xác nhận.

Metric chính là macro nDCG@10 trên người có target hợp lệ, kèm phân rã từng cohort. Báo cả trung bình theo người của toàn tập và trung bình đều các cohort đã định nghĩa; hai số trả lời hai câu hỏi khác nhau. Không chọn cách gộp có lợi nhất sau khi chạy. Metric phụ gồm Recall@10, catalog coverage, tỷ lệ feed rỗng, độ đa dạng tác giả, mức tập trung exposure và tuổi bài theo đồng hồ phù hợp. Topic match mô tả độ khớp lựa chọn, không tự chứng minh hài lòng.

Cho từng seed, báo điểm tuyệt đối, chênh lệch cặp với baseline, cohort size, thời gian training/refit và bộ nhớ. Gộp optimizer seeds của LightGCN trong từng dataset trước, rồi mới gộp dataset seeds. Ba seed cho bằng chứng mô tả độ ổn định; không coi hàng nghìn user hoặc chín training runs là những cộng đồng độc lập. Không tuyên bố có ý nghĩa thống kê chỉ vì trung bình cao hơn. Muốn suy luận mạnh hơn cần kế hoạch số replicate và phân tích phụ thuộc dữ liệu riêng.

Lượt chính chạy ma trận ở bước 2. Sau đó chạy ablation — bỏ một thành phần để biết nó đóng góp gì: bỏ topics; bỏ chuyển tiếp; thay warm model; đối chiếu công thức Favi. Robustness ưu tiên sở thích thiếu/sai và thay đổi cơ chế exposure, với mức thay đổi chốt trước lượt xác nhận. Giữ cả kết quả xấu và lỗi chạy.

Labels của simulator chịu ảnh hưởng của chính sách cho xem bài. Kết quả replay không chứng minh tăng engagement nếu triển khai chính sách mới. Không tự suy ra propensity hay causal effect khi không có logging/randomization phù hợp. MovieLens tiếp tục là phép kiểm tra độc lập cho interaction-based ranking; nếu dùng genres làm proxy sở thích, phải tách thành thí nghiệm mới và không gọi đó là onboarding thật.

Đầu ra: `document/benchmarks/adaptive-v1-results.md`, JSON đầy đủ độ chính xác, per-cohort tables, biểu đồ điểm theo số positive và biểu đồ chất lượng–chi phí; đây là các artifact dự kiến.

**Quyết định sau kết quả:** nếu router ổn định hơn theo tiêu chí đã chốt, đưa vào demo; nếu ngang nhưng rẻ/đơn giản hơn, chỉ kết luận về trade-off đó; nếu thua hoặc phụ thuộc mạnh simulator, giữ baseline và viết rõ ranh giới thất bại. Không mở thêm model chỉ để tìm một chiến thắng.

## 8. Bước 6 — Kiểm chứng trong Favi bằng request thật

Tích hợp chính sách đã chọn vào đường thử nghiệm mặc định tắt. Giữ contract ở ContentDiscovery, host adapter nối module qua query contracts/facades và đăng ký DI trong extension. Không truy cập chéo DbContext. Preference mutation giữ JWT, Idempotency-Key và optimistic version; mọi feed lọc lại quyền/public/ban/NSFW/archive/delete/own-post và positives hiện tại. Giữ routing/rate limit NGINX hiện có; nếu đo qua gateway, phân biệt với direct localhost.

Demo theo một hành trình: tạo tài khoản → chọn hoặc bỏ qua chủ đề → nhận feed cold → tạo positive → đi qua transition → nhận warm feed → model hết hạn và fallback. Kiểm tra cả thay đổi quyền bài sau khi artifact được tạo. Xác minh UI hiện có trước khi lên phạm vi frontend; nếu chưa có, thêm UI tối thiểu hoặc trình diễn API và ghi rõ giới hạn.

Artifact hiện giới hạn 1.000 users/16 MiB và 300 candidates mỗi method/user. Không tăng số một cách máy móc để gọi là chạy 5k. Chọn và tài liệu hóa cơ chế phục vụ có giới hạn tài nguyên, hoặc ghi rõ chỉ phục vụ một sample trên catalog lớn. Đo cả load/startup artifact, peak memory, chi phí refresh và request sau lọc.

Đo HTTP thật trong stack riêng: cold/warm, tuần tự và tải đồng thời có ngân sách định trước, warm-up riêng, số request và lỗi rõ ràng, p50/p95/p99. Chỉ báo percentile khi số mẫu đủ để có ý nghĩa; 20 request cũ không đại diện tail latency ở tải lớn. Phân biệt cold user với cold process startup. Tính cả thời gian rebuild artifact khi đánh giá khả năng thích ứng sau positive đầu tiên.

Chạy .NET/Docker/numerical workloads bằng CLI trong môi trường được phép ngoài sandbox khi cần, tránh lặp cách chạy gây popup. Chỉ cleanup process/container do lượt test tạo, giữ log thất bại. Cập nhật tài liệu và chạy regression phù hợp, không mặc định deploy production.

Đầu ra: demo replay được, HTTP report, log route/fallback, số liệu vận hành và danh sách giới hạn. **Điểm review:** hành vi đúng, lợi ích nhìn thấy và chi phí được đo; không dùng số offline để thay thế bằng chứng serving.

## 9. Bước 7 — Đóng gói thành kết quả khóa luận có thể bảo vệ

Viết báo cáo theo ba câu hỏi nghiên cứu, mỗi kết luận nối tới bảng kết quả và artifact. Trình bày cả phản ví dụ, giả định simulator, bias exposure, độ nhỏ của số seed, giới hạn MovieLens và khác biệt offline/online. Nếu chưa có người thật, kết luận chỉ trong các điều kiện đã đo. Một nghiên cứu âm tính nhưng protocol tốt vẫn có giá trị; không bảo đảm trước việc hội đồng chấp nhận đề tài.

Bundle tái lập gồm source revision/diff hash, môi trường, configs, manifests, commands, kết quả thô và báo cáo. Source/config/report vào Git; dữ liệu/model lớn có inventory/checksum và vị trí backup. Tuân thủ điều khoản MovieLens, không đưa raw/derived data vào public repo. Review từng slice trước commit/PR, chỉ merge khi kiểm tra tương ứng đạt và phạm vi được duyệt.

Đầu ra cuối là một luận điểm có bằng chứng: **với nhóm người dùng nào nên dùng phương pháp nào, lợi ích bao nhiêu, đổi lại chi phí gì, và khi nào kết luận không còn đúng**.

## 10. Thứ tự thực hiện và giới hạn phạm vi

Thực hiện tuần tự: protocol → dataset → baseline parity → chính sách → experiment → HTTP/demo → báo cáo. Dừng ở mỗi điểm review để người dùng đánh giá trước khi mở rộng. Việc viết kế hoạch này chưa khởi chạy các bước đó.

Action tiếp theo có giá trị nhất là **bước 1: chốt protocol và inventory**, không phải train thêm một model. Sau đó ưu tiên dataset cold/transition pilot và parity của công thức hiện tại. Tạm hoãn neural router, LLM sinh hành vi quy mô lớn, thêm nhiều model và rollout production; chỉ mở khi một câu hỏi đã đo cho thấy cần chúng.

## 11. Nền tảng nghiên cứu để đối chiếu

- [Ji và cộng sự — A Critical Study on Data Leakage in Recommender System Offline Evaluation](https://arxiv.org/abs/2010.11060): nghiên cứu ảnh hưởng của việc không tôn trọng global timeline. Đây là cơ sở để kiểm soát thời điểm tồn tại của feature/item và tránh leakage trong protocol.
- [Steck — Embarrassingly Shallow Autoencoders for Sparse Data](https://arxiv.org/abs/1905.03375): nguồn phương pháp EASE; dùng để kiểm tra triển khai và giữ baseline đơn giản nhưng mạnh trong so sánh.
- [He và cộng sự — LightGCN](https://arxiv.org/abs/2002.02126): nguồn phương pháp graph baseline. Việc so sánh phải ghi cấu hình, budget và tín hiệu, không suy ra graph luôn tốt hơn từ tên mô hình.

Ba nguồn này là điểm bắt đầu, chưa phải literature review đầy đủ và chưa chứng minh tính mới của hướng thích ứng. Bước 1 phải bổ sung công trình trực tiếp về preference elicitation và hybrid cold-to-warm trước khi chốt phát biểu đóng góp.
