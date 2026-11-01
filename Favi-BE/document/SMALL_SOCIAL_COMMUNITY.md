# Cộng đồng xã hội nhỏ bằng hybrid ABM và OASIS

## Phạm vi và cơ sở

Đây là mô hình thử nghiệm kỹ thuật cho Favi, không phải mô hình tâm lý đã hiệu
chỉnh bằng dữ liệu người thật. Giữ OASIS 0.2.5 ở commit
`ba892e0667eb84b312e4c0e5630f275154bbb98f`, không sửa package upstream hoặc API
production. `community_policy.py` giữ trạng thái và policy; `run_community.py`
điều phối inference local, gọi public OASIS action, validate và export adapter.
ABM (agent-based modeling) mô tả từng cá nhân có trạng thái và quy tắc tương tác,
rồi quan sát hành vi tập thể. Bản hybrid chỉ dùng LLM cho văn bản. Tool nghiên cứu
này không thay thế pipeline seed production hay tuyên bố tạo follower graph Zipf.

Nguồn tham khảo:

- Participation inequality: https://www.nngroup.com/articles/participation-inequality/
  dùng 90/9/1 làm giả định vai trò, không ép số tương tác cuối phải đúng tỷ lệ này.
- Bounded confidence: https://www.jasss.org/5/3/2.html — chỉ chịu ảnh hưởng từ quan
  điểm đủ gần. Đây là phiên bản cập nhật từng exposure, không tái hiện toàn bộ
  thí nghiệm Hegselmann–Krause.
- Generative Agents: https://arxiv.org/abs/2304.03442 — học ý tưởng trạng thái/bộ
  nhớ riêng và lấy context liên quan, không triển khai đầy đủ reflection/planning.
- Homophily: https://www.annualreviews.org/content/journals/10.1146/annurev.soc.27.1.415
  là xu hướng quan hệ giữa người tương đồng; lựa chọn affinity ở đây chỉ là cách
  hiện thực giả thuyết, không phải hệ số được suy ra từ bài nghiên cứu.
- Social influence: https://www.princeton.edu/~mjs3/salganik_dodds_watts06_full.pdf
  là thí nghiệm thị trường âm nhạc, không phải calibration cho Favi. Dùng làm động
  cơ đưa popularity vào feed; không chuyển kết quả của thí nghiệm thành quy luật
  phổ quát. Fatigue, trust và mọi trọng số hiện là tham số giả lập.

## Population và phân phối

100 tài khoản: 90 reader, 9 contributor, 1 creator; shuffle bằng seed 20261031.
Interests của ba chủ đề food/transport/photography là ba Gamma(0.6,1) chuẩn hóa
(Dirichlet đối xứng). Opinion ban đầu Uniform(0,1), openness Uniform(.15,.45),
learning Uniform(.05,.2), sociability Uniform(.2,.9). Mỗi người có một lens trong
chi phí, nguyên liệu, văn hóa, đám đông, an toàn, môi trường; một style gồm câu hỏi,
gợi ý thực tế hoặc phản biện thận trọng. Đây là biến giả lập, không chẩn đoán tính cách.

Bootstrap tạo hai follows khác nhau cho mỗi user, không self-follow; chọn không
hoàn lại với trọng số .1+affinity^3. Affinity=1-L1(interests)/2; baseline .1 cho
phép giao lưu ngoài nhóm. Founder là creator đăng ba topic mở đầu bằng LLM; graph
và các bài này được gắn bootstrap, không gọi là hành vi tự phát.

## Policy và trạng thái động

20 ticks, shuffle thứ tự user theo seeded RNG. Fatigue *= .75 mỗi tick; xác suất
hoạt động=baseline*(1-fatigue), baseline reader .12, contributor .65, creator .9.
Mỗi activation nhìn tối đa ba posts ngoài tác giả chính mình. Rank score:
.5*topic_interest + .15*affinity + .15*trust + .1*social_proof + .1*novelty.
Social proof=log(1+likes)/(1+log(1+likes)); novelty=1 nếu chưa từng thấy. Đây là
custom feed trong simulator, không phải kiểm chứng ranking OASIS hay Favi.

Mỗi exposure: nếu |opinion-post_opinion| <= openness, cập nhật
opinion += learning*(post_opinion-opinion); ngược lại không đổi. Opinion của post
là trạng thái số của tác giả lúc đăng, không suy ra bằng phân tích sentiment LLM.
Seen/liked/commented/follows/trust là bộ nhớ riêng. Trust mặc định .3, bootstrap
.45, reinforcement .03 sau tương tác thành công và chặn ở 1.

Sau exposure, policy chọn tối đa một mutation: creator có xác suất .15 tạo post;
contributor/creator có xác suất .35 bình luận nếu chưa comment post đó; follow nếu
chưa follow với xác suất .12*sociability; like nếu chưa like với xác suất
.2+.35*interest. Nếu các điều kiện không kích hoạt thì chỉ đọc. Đây là các nhánh
có thứ tự, không phải categorical distribution có tổng bằng 1. Reader không viết
nội dung. Fatigue tăng .12 sau activation và chặn ở .8. Không xóa/đổi nội dung
để tạo vẻ đa dạng; mọi quyết định, visible IDs, arguments và kết quả được ghi.

## Giới hạn token và hợp đồng LLM

Chỉ generate post/comment gọi Ollama local qwen3:1.7b; tối đa 12 calls, 128 output
tokens/call, context 4096, think=false, temperature .8, seed theo call. Like/follow,
đọc và cập nhật state dùng code, không LLM. Prompt chứa lens/style/opinion riêng,
topic, post mục tiêu và tối đa sáu nội dung cùng chủ đề đã được chấp nhận gần nhất. JSON schema
chỉ có content; code chọn ID hợp lệ, LLM không được tự bịa tool arguments.

Nội dung rỗng, quá 45 từ, JSON sai hoặc giống normalized text đã nhận >=.85 theo
SequenceMatcher bị từ chối, không retry hay sửa lại. Lượt bị từ chối vẫn tính vào
budget. Khi hết budget, tiếp tục read/like/follow và ghi quyết định không có mutation.
Giới hạn này kiểm soát chuỗi ký tự, không chứng minh đa dạng ngữ nghĩa.

## Engine, validation và nhập Favi

Engine được reset thật; action thực thi tuần tự bằng public
`SocialAgent.perform_action_by_data`, await và kiểm success. Đây là hybrid policy,
không phải LLMAction toàn quyền; StubModel chỉ đáp ứng constructor. Custom feed
không gọi env.step recommendation mỗi action. Không INSERT hay tạo schema SQLite
bằng tool. Timestamp OASIS bắt đầu UTC-naive như adapter contract hiện tại.

Sau close: SQLite integrity, physical FK từng bảng và semantic anti-join toàn bộ
post/user, like/user/post, follow/users, comment/user/post phải đạt; chỉ chấp nhận
lỗi DDL upstream like->tweet đã nêu ở OASIS_SIMULATOR_VERIFICATION.md. Adapter
kiểm thêm temporal, duplicate, self-follow và mọi tham chiếu trước publish snapshot.
SHA256 source trước/sau export phải giống. Media URL là metadata safe explicit,
không tạo ảnh hoặc gọi moderation. Không sửa production database.

Quality gate: >=3 posts, >=3 comments, >=2 comment authors. Không buộc đạt bằng
thêm nội dung giả. Failure giữ raw evidence. Báo cáo ghi role counts, calls/tokens,
content bị từ chối, exposure, số opinion updates, Gini mutations (gồm bootstrap),
population trước/sau, source/dependency/model digest và mọi event. Gini đo tập
mutation của lượt này, không chứng minh Zipf hoặc hiệu ứng xã hội đã xuất hiện.

RNG policy tái lập theo seed; thời gian engine và tính toán LLM có thể khác giữa
hai lần chạy. Không hứa byte-identical snapshot. Unit tests kiểm role counts,
seed, graph invariants, fatigue, bounded confidence và chống like lặp của reader.

## Chạy end-to-end

Từ solution root, dùng environment và dependency lock hiện có. Khởi động một
Ollama container riêng bind loopback với cache `.tools/ollama-models`; lấy host
port bằng `docker port`. Chạy:

```powershell
./.tools/oasis-env/Scripts/python.exe tools/oasis-smoke/run_community.py .tools/oasis-smoke/<new-run-id> .tools/oasis-upstream http://127.0.0.1:<port>
./tools/oasis-smoke/verify.ps1 -ExistingRunDirectory .tools/oasis-smoke/<new-run-id> -Python .tools/oasis-env/Scripts/python.exe
```

Gate tích hợp tạo Compose project riêng, import/replay/rollback, gọi Kestrel child
port động với JWT thật và so snapshot qua HTTP; cuối cùng dọn stack test. Dừng đúng
container Ollama do lượt chạy tạo; giữ cache. Không push/merge/deploy tự động.
Evidence: oasis.db, community-report.json, llm-calls.json, events.json, media.json,
export và review.md; integration report nằm trong run riêng do verify.ps1 tạo.
`review_community.py <run-root>` dựng lại review.md từ raw report/events/snapshot,
liệt kê toàn bộ bài và bình luận, role activity và Gini không gồm bootstrap.
Gini=sum((2*i-n-1)*sorted_value_i)/(n*sum(values)), trả 0 khi tổng bằng 0.
Artifact chỉ là bản đọc dữ liệu, không frontend hay dịch vụ mạng xã hội đang host.

Creator được phép quyết định đăng ngay cả khi không có bài của người khác trong
feed. Nếu đã chạm giới hạn 5 posts hoặc hết budget LLM thì không sinh thêm bài;
không ép creator có mutation ở mọi tick. Guard runtime đối chiếu user_id/agent_id
trong package pin trước khi tạo graph, không giả định hai ID luôn bằng nhau ở mọi
phiên bản OASIS. Response LLM hết token, sai JSON/type/content hoặc lặp bị từ chối.

## Lượt kiểm chứng đầu ngày 31/10/2026

Run `.tools/oasis-smoke/community-100-20261031`: 100 users, 3 posts, 58 likes,
221 follows (200 bootstrap +21 phát sinh), 7 comments từ 6 authors. 12 LLM calls,
3.739 input +432 output =4.171 tokens; 2 generated texts bị từ chối vì gần trùng.
909 post exposures, 516 cập nhật opinion theo luật; không coi đây là quan sát
tâm lý người thật. Non-bootstrap mutation Gini .4881, gồm mọi read-user likes và
follows, không chỉ số bài được viết.

PostgreSQL/API đạt 847 checks tại
`.tools/oasis-smoke/4a6ba51870744a7593522aa8453bcfc7/report/checks.json`, API child
http://127.0.0.1:50066/, đã cleanup. Bản này phát hiện creator bị chặn đăng tiếp
khi feed không có tác giả khác; bản cuối đã sửa và thêm regression test. Context
toàn cục cũng đã đổi thành context cùng topic. Lượt này là evidence lịch sử,
không đại diện cho execution bản cuối.

Chất lượng văn bản còn hạn chế: ví dụ bài đầu tự nêu giả định pre-packaged
ingredients không có trong scenario. Gate hiện phát hiện cấu trúc/trùng chuỗi,
không fact-check hoặc chứng minh khác biệt ngữ nghĩa. Không dùng kết quả này làm
bằng chứng dự đoán xã hội, kiểm định tâm lý hay hiệu năng production.

## Kết quả bản cuối ngày 31/10/2026

Run `.tools/oasis-smoke/community-100-final-20261031` đạt quality gate. Có 100 users,
4 posts (3 bootstrap +1 phát sinh), 65 likes, 218 follows (200 bootstrap +18 phát
sinh), 4 comments từ 4 authors. 12 LLM calls, 2.728 input +443 output =3.171 tokens.
4 nội dung gần trùng bị từ chối, không retry hoặc thay bằng câu viết tay. Tổng
token hai lượt community đã thực hiện: 7.342; không gồm các lượt OASIS cũ.

Có 882 exposures và 516 opinion updates theo luật; mutation Gini không bootstrap
.4977. Reader có 192 activated turns/67 later mutations; contributors 102/20;
creator 15/1. Không coi phân phối đầu ra này là số liệu quan sát từ người thật.
Creator đã đăng được thêm bài khi chỉ có own posts trong feed.

12 adapter tests +18 policy/diversity/source tests và 853 PostgreSQL/HTTP checks
đạt, exit0. Gate mới gồm 7 policy tests. Integration evidence:
`.tools/oasis-smoke/c80ae305d31c46f1a56ff10d291964c1/report/checks.json`.
Ollama container riêng và Compose test đã cleanup; giữ model cache để tái sử dụng.

Đọc toàn bộ conversation và role activity tại
`../.tools/oasis-smoke/community-100-final-20261031/review.md`; raw prompts và
responses tại `llm-calls.json`, trạng thái từng người tại `community-report.json`,
mọi action/result/bootstrap và quyết định không mutation tại `events.json`.
Không push/merge. Đây là nền mô phỏng offline có dữ liệu kiểm chứng qua API Favi,
chưa phải frontend mạng xã hội hoặc dịch vụ được host cho người dùng đăng nhập.

Giới hạn bản cuối vẫn còn: post 2 và 4 gần nhau về ý; post 1 có giả định không được
scenario cung cấp. Context theo topic giảm trộn chủ đề nhưng không thay thế semantic
evaluation/fact checking. Các trọng số trust, fatigue, social proof và confidence
cần calibration, nhiều seeds và ablation trước khi đưa ra kết luận nghiên cứu.
