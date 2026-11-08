# Agent xã hội tự chọn danh tính và hành động

## Mục tiêu và phạm vi

Bối cảnh là xã hội con người đương đại, tuân giới hạn vật lý bình thường; không
cho khả năng siêu nhiên. Đây là ranh giới thế giới, không phải persona hoặc nghề
nghiệp gán sẵn. Agent được chọn danh tính bất kỳ trong bối cảnh con người đó.

Chế độ mới `run_free_society.py` bỏ role 90/9/1, persona có sẵn, topic food/travel,
bootstrap follows/posts và xác suất hành động do code quyết định của hybrid ABM.
Giữ nguyên chế độ cũ để so sánh. Không thêm endpoint/schema production.

Mỗi người khởi đầu bằng một lần inference tự chọn name, persona, public_bio,
emotion, goal. Không đưa danh sách nghề, cảm xúc hoặc sở thích cho model chọn.
Các lần inference sau cho model tự cập nhật persona/emotion/goal, ghi một
personal_event tùy chọn, chọn một action hoặc không hoạt động, tự chọn thời gian
nghỉ 0..3 vòng. Không có quota post/comment, không ép nội dung đa dạng và không
loại bài vì ý tưởng trùng. “Tự do” vẫn có model priors và giới hạn của platform;
không đồng nghĩa agent là người thật hoặc mô phỏng được kiểm định xã hội học.

## Bộ nhớ và quyền riêng của từng nhân vật

Mỗi agent có state độc lập; dùng chung model local stateless nhưng prompt chỉ gồm
private state của chính mình, tối đa 6 ký ức hành động/kết quả gần nhất, public
directory và feed. Public directory chỉ có ID/name/public_bio_preview ban đầu; persona,
cảm xúc, mục tiêu và nhật ký riêng không bị lộ cho agent khác. Danh tính public
giữ tên lúc signup; persona riêng có thể phát triển mà không tự sửa profile Favi.
Raw audit trên máy dành cho người vận hành có đầy đủ private state để review.

Personal event là trải nghiệm hư cấu tự viết trong đời sống nhân vật. Nó không
tự trở thành sự thật công khai hoặc điều khiển người khác. Agent có thể nói về
nó trong post, người khác chỉ nhìn thấy lời kể gắn với tác giả. Model không được
đổi state, cảm xúc hoặc hành động của agent khác bằng một câu khẳng định.

## Môi trường và action

Mặc định 6 agents/6 ticks, thứ tự shuffled theo seed 20261101. Mỗi agent có tối đa
một cơ hội quyết định mỗi tick; được chọn wait/read và ngủ thêm vòng. Đây là lịch
thực thi kỹ thuật, không ép cá nhân hoạt động. Không đồng nhất tick với ngày thật.
Feed gồm 5 posts mới nhất và 3 comments mới nhất/post; memory giữ ID đã nhìn thấy.
Feed là cửa sổ thời gian, không phải benchmark recommendation OASIS/Favi.

- wait/read: không mutation công khai; read nhận feed trong prompt.
- post/comment/like/unlike/follow/unfollow: model chọn action, target và nội dung;
  code chỉ kiểm target hợp lệ, không self-follow, không lặp trạng thái quan hệ và
  thực thi bằng public OASIS `perform_action_by_data`.
- event: model tự viết thông báo sự kiện, engine tạo post thật. Simulator tạo
  event ID liên kết post, owner và members ban đầu rỗng. Chủ sự kiện cũng phải
  tự chọn join_event nếu muốn tham gia; một lời mời không tự ghi danh bất kỳ ai.
- join_event/leave_event: mỗi người tự chọn tham gia/rời một event thực sự có
  trong môi trường. Membership là sidecar JSON của simulator, không có bảng/API
  Event trong Favi; adapter chỉ import post thông báo. Không tuyên bố đã nhập
  membership sang Favi hay rằng nhân vật đã tham dự một sự kiện đời thực.

Đây là model-selected decisions qua native Ollama, không phải OASIS LLMAction.
StubModel chỉ đáp ứng constructor, không được dùng để chọn hành vi/nội dung.
Không direct INSERT/schema fabrication vào SQLite. Pinned OASIS/source/dependency
lock và kiểm ID mapping như các runner trước vẫn áp dụng.

## Giới hạn kỹ thuật và chi phí

Ollama local (mặc định qwen3:4b, chọn model đã cài bằng `--model`), think=false, temperature1, context4096, tối đa384 generated
tokens/call; seed khác mỗi call. Max calls=agents*(rounds+1), mặc định42; không
retry ẩn. Profile/turn có JSON schema, giới hạn độ dài để giữ bộ nhớ/token hữu hạn.
Không dùng sentiment score cố định, luật fatigue/trust hoặc ép character arc.

Tổng timeout mặc định1200 giây; `--timeout-seconds` cho phép60..7200 để chạy số
người lớn hơn trên CPU mà vẫn có trần rõ ràng. Lượt10 người/4 vòng đặt3600 giây,
vẫn tối đa50 inference calls; timeout lớn hơn không tăng số lần gọi hoặc output
tokens/call. Timeout không được tính là lượt diễn thử thành công.

Không dùng maxLength grammar cho string vì decoder có thể kết thúc giữa từ khi
chạm biên. Validator kiểm sau khi nhận nguyên response: private state và bio có
trần bảo vệ 4096 ký tự mỗi field, content <=1000, name <=60; không có trần ngắn
riêng cho emotion/goal. Chi phí thực được chặn bởi 384 generated tokens/call và
context 8000 bytes. Quá giới hạn thì reject/abort, không cắt state hoặc sửa ý model.
Public directory dùng bio preview tối đa160 ký tự và dấu ellipsis nếu rút ngắn;
bio đầy đủ trong profile/OASIS/private audit không thay đổi.

Context JSON gửi model bị chặn 8.000 UTF-8 bytes: trim bản sao observation theo
thứ tự memory cũ, comments, posts cũ rồi public events cũ; không sửa canonical
private/world state. Budget bytes là proxy bảo thủ, không phải tokenizer chính xác.
Nếu phần bắt buộc vẫn quá lớn thì từ chối turn và giữ audit. Request log ghi đúng
context thực gửi. Agent chỉ join event đang quan sát hoặc leave event đã tham gia.

Decision sai target/schema/quan hệ bị ghi rejected, không sửa tay hoặc gọi model
lại để có kết quả đẹp. State không được nhận từ turn invalid; memory ghi lỗi cho
lần quyết định sau. HTTP/infrastructure errors làm abort và vẫn giữ audit. Khởi
tạo profile sai làm abort vì không có persona fallback gán sẵn. Nghỉ/không đăng,
đồng thuận, trùng persona và nội dung kém là kết quả phải báo, không phải cớ để
tự thêm action hoặc rewrite nội dung. Không yêu cầu số post/comment tối thiểu.

## Validation và end-to-end

Native JSON schema dùng anyOf cho mọi action đang hợp lệ: các action không có
target chỉ cho null; comment/like chọn trong known IDs; follow loại self/đã follow;
unlike/unfollow và event membership chỉ cho đảo trạng thái thực đang có. Agent
vẫn chọn bất kỳ action hợp lệ, gồm wait, không có trọng số xác suất. Schema cũng
kiểm type/structure state; validator Python kiểm độ dài như defense-in-depth. Không sửa
nhầm target bằng code hoặc tự chọn action thay model.

Unit tests kiểm quyền target, không self-follow, quan hệ đảo ngược, event membership,
không mutate state khi validation/trimming và quyền không hoạt động. Sau engine.close: SQLite integrity và
physical FK từng bảng (ghi rõ lỗi upstream like->tweet đã biết). Adapter validate
100% semantic references, temporal ordering, duplicate edges, media map trước
publish. Source SHA256 không đổi sau export. Không tuyên bố global physical FK
pass vì DDL upstream group_messages còn sai, theo báo cáo simulator hiện có.

Media URL chỉ là metadata test safe explicit, không phải ảnh sinh/moderation.
Chạy `verify.ps1 -ExistingRunDirectory` kiểm PostgreSQL/import/replay/rollback và
API Favi thật như các mode trước; empty feed là kết quả hợp lệ. Không sửa dữ liệu
để ép đủ bài, bình luận, follows hoặc kiểm chứng tâm lý. Đây là kiểm chứng kỹ thuật.

```powershell
./.tools/oasis-env/Scripts/python.exe tools/oasis-smoke/run_free_society.py .tools/oasis-smoke/<new-run-id> .tools/oasis-upstream http://127.0.0.1:<owned-ollama-port> --agents 6 --rounds 6
./tools/oasis-smoke/verify.ps1 -ExistingRunDirectory .tools/oasis-smoke/<new-run-id> -Python .tools/oasis-env/Scripts/python.exe
```

Dùng Ollama container riêng bind loopback, cache `.tools/ollama-models`; dừng đúng
container do lượt chạy tạo. Evidence: free-report.json, llm-calls.json, history.json,
private-state.json, public-events.json, oasis.db, export và review.md. Report ghi
hash runner/policy, source/model/dependencies, counts, budget, rejected decisions,
sleeping opportunities và toàn bộ identity ban đầu. Không push/merge/deploy.

## Tiêu chí đánh giá tiếp theo

Quan sát danh tính có tự hình thành và thay đổi hay không, memory có ảnh hưởng
quyết định sau, có nghỉ/chọn lọc tương tác, quan hệ/sự kiện tự xuất hiện hay không.
Không kết luận “như xã hội thực” từ một lượt nhỏ. Cần nhiều seeds, so sánh model,
đánh giá ngữ nghĩa và đối chiếu hành vi người thật; persona tự chọn không tự loại
được xu hướng chung của model. Tăng tự do thường cần nhiều inference hơn hybrid.

## Lượt kiểm chứng ngày 01/11/2026

Lượt `.tools/oasis-smoke/free-6x6-20261101` dừng chủ động khi framing “fictional
person” làm model chọn dream-weavers/người kể chuyện fantasy và sinh lỗi target.
Giữ partial raw audit cùng ABORTED.txt; không export, không tuyên bố gate pass.
Sau đó sửa world thành contemporary human society và dùng schema anyOf cho mọi
action hợp lệ; không thay persona cụ thể hay ép tương tác để có số liệu đẹp.

Lượt qwen3:1.7b `.tools/oasis-smoke/free-human-6x4-20261101`: 6 agents/4 rounds,
30 calls, 30.525 input +4.538 output =35.063 tokens. 20 posts, 4 read decisions;
0 likes/follows/comments, 0 invalid decisions, 0 opportunities bị ngủ qua. Không
có quota hoạt động nên không thêm interaction để sửa số liệu. Profile tự chọn
đời thường nhưng model thiên về tên Lila, nghệ thuật, tò mò và mong muốn kết nối.
Đây là bằng chứng autonomy kỹ thuật, chưa đạt hành vi xã hội thuyết phục.

Harness thực thi được giữ trong thư mục harness của run, hash đối chiếu report.
Lượt so sánh tiếp theo dùng qwen3:4b, cùng cơ chế/seed và 6 agents/2 rounds để đối
chiếu định tính với hai vòng đầu baseline. Không coi một lượt/model là benchmark
thống kê hoặc bằng chứng nhân quả. Model local chính thức:
https://ollama.com/library/qwen3:4b — actual digest/version ghi trong từng report.

Baseline 1.7B đạt 264 PostgreSQL/HTTP checks ở run tích hợp
`.tools/oasis-smoke/33da07fbe21a4b659c313a1881fab229`. Lượt 4B 6x2 hoàn tất:
18 calls, 11.686 input +2.957 output =14.643 tokens; 12 wait, không public
mutation, không sleep và không invalid decision. Đạt 44 PostgreSQL/HTTP checks ở
`.tools/oasis-smoke/0732a02019c64c7dbb5e6fada8d5b102`. Empty feed không bị thêm
data giả để qua gate. Cả hai dùng phiên bản giới hạn state cũ trong harness archive.

Sau đó bỏ maxLength grammar gây string cụt và trần emotion 120 ký tự: regression
profile 125 ký tự đã bắt lỗi này. Không sửa profile output đã sinh. Lượt regression
đầu bị mất tool sessions/tiến trình và container Exited255, giữ INTERRUPTED.txt;
lượt r2 abort vì trần emotion cũ, có raw calls/console. Đây không phải success.

Bản cuối `.tools/oasis-smoke/free-final-4b-2x1-20261101-r3`, qwen3:4b, đạt:
2 agents/1 round, 4 calls, 1.543 input +405 output =1.948 tokens; 1 post và 1 read,
0 invalid decisions. Alex Chen tự chọn là software developer, Ava tự chọn là
urban gardener. Ava tự kể chuyện thấy cây cà chua hàng xóm và chọn đăng; Alex
chọn đọc, không bị tự chuyển câu suy nghĩ riêng thành public comment. Không có
public event hoặc membership do model tự tạo ở lượt này; chỉ protocol tests đã
kiểm create/join/leave, không tuyên bố event tự xuất hiện trong diễn thử.

12 adapter +30 source/policy/agent tests (42 total) và 35 PostgreSQL/HTTP checks
đạt, exit0. Integration report:
`.tools/oasis-smoke/48bb0a7e7d2a4d51aec7024662a4bfee/report/checks.json`.
Ollama container và stack test đã cleanup; model cache còn nguyên. Mặc định runner
chuyển sang4B, cùng model với regression đã chạy explicit bằng `--model qwen3:4b`.

Đọc [nhật ký bản cuối](../.tools/oasis-smoke/free-final-4b-2x1-20261101-r3/review.md),
[baseline1.7B](../.tools/oasis-smoke/free-human-6x4-20261101/review.md) và
[lượt4B6x2](../.tools/oasis-smoke/free-human-4b-6x2-20261101/review.md).
Đây là kiểm chứng quyền tự quyết/state isolation và đường tích hợp. Kết quả phụ
thuộc mạnh vào model, prompt và giới hạn serialization; chưa có bằng chứng hành vi
xã hội thực hoặc đa dạng dân số. Không gọi đây là một xã hội đã được mô phỏng đúng.

## Diễn thử 10 người — 01/11/2026

Tham số yêu cầu: 10 agents, 4 rounds, qwen3:4b, seed20261101, timeout3600 giây,
tối đa50 inference calls, 384 output tokens/call. Không bootstrap graph/posts,
không gán persona hoặc quota tương tác. Request/harness/raw audit nằm tại
`.tools/oasis-smoke/free-10x4-20261101/`. Cùng cơ chế với bản regression cuối;
số người lớn hơn làm thay đổi directory, thứ tự lượt và context nên không coi
khác biệt counts giữa hai lượt là kiểm định nhân quả của population size.

Kết quả hoàn tất: 10 users, 16 posts, 18 follows, 2 comments, 0 likes; 4 wait
decisions, 0 sleeping opportunities, 0 invalid decisions. 50 calls, 52.432 input
+6.247 output =58.679 local tokens. Tất cả follows/posts phát sinh từ quyết định
model, không có bootstrap. Không agent nào chọn event/join/leave ở lượt này.

Hành vi theo IDs (tên trùng vẫn là các state và identity độc lập):

- IDs0,1,2,4: mỗi người đăng4 posts.
- ID3: chọn wait ở cả4 vòng.
- ID5: follow2 lần và comment2 lần.
- IDs6,7,8,9: mỗi người follow4 người.

Chất lượng: 16 posts chỉ có10 nội dung sau lowercase+trim, tức6 duplicate rows.
Hai comments của ID5 trùng nguyên văn và cùng target post11 của ID1; không phải
hai người khác nhau chọn cùng câu. Không loại/sửa nội dung lặp vì đây là quan sát
chế độ tự quyết, không có diversity filter. Kết quả có quan hệ xã hội tự hình thành
nhưng còn lặp mạnh, chưa chứng minh đối thoại hoặc trải nghiệm xã hội chân thực.

12 adapter +30 source/policy/agent tests và264 PostgreSQL/import/HTTP checks đạt,
exit0. Integration evidence:
`.tools/oasis-smoke/21256e8e953e40ca8744ea7371b78bed/report/checks.json`.
Model container riêng và Compose test đã cleanup; cache giữ nguyên. Không merge.

[Bảng10 người và toàn bộ public conversation](../.tools/oasis-smoke/free-10x4-20261101/summary.md)
được dựng từ report/history, không viết thay nội dung model. Raw private states,
goals/events và prompts/responses vẫn ở review.md, private-state.json,
history.json, llm-calls.json; snapshot đã validate nằm trong export/.

## Đối chiếu khả năng OASIS với runner — 01/11/2026

Kiểm source pin ba892e0667eb84b312e4c0e5630f275154bbb98f và upstream docs:

- `SocialAgent` kế thừa CAMEL `ChatAgent`, có memory/context và tool integration;
  `perform_action_by_llm` quan sát môi trường rồi gọi astep. Runner free hiện tự
  gọi Ollama và chỉ dùng perform_action_by_data để execute; không sử dụng native
  ChatAgent memory cho quyết định, dù action vẫn được ghi vào memory nội bộ đó.
- Native `SocialEnvironment.get_posts_env` gọi refresh; `env.step` cập nhật rec
  table. OASIS có interest/hot-score recommendations. Runner free lấy global
  latest5 bằng SQL, không dùng recommendation này; memory gửi model chỉ last6.
- Có custom prompts/tools/models cho từng SocialAgent; ví dụ upstream
  examples/different_model_simulation.py và custom_prompt_simulation.py. Dùng
  chung một model hiện tại là lựa chọn runner, không phải giới hạn OASIS.
- Native action space có search/trend/refresh và group chat, rộng hơn runner
  hiện tại. Group chat/repost không có mapping Favi đầy đủ trong adapter hiện tại;
  không bật thêm rồi tuyên bố toàn bộ state đã được import.
- Một mutation/cơ hội là giới hạn của runner, không phải khẳng định OASIS chỉ
  hỗ trợ như vậy. Không thấy vòng reflection/planning chuyên biệt trong package
  OASIS pin qua source search; ChatAgent memory không tự chứng minh có cơ chế
  suy ngẫm về lặp, mục tiêu hoặc tâm lý được hiệu chỉnh.

Vì vậy kết quả lặp vừa rồi chưa đủ để kết luận là giới hạn của OASIS. Đối chứng
đề xuất: cùng10 self-authored profiles, cùng4B, cùng ngân sách quyết định; so
native LLMAction/recsys/memory với wrapper hiện tại, đo duplicate text, reciprocal
interaction, invalid calls và tokens. Đây là đề xuất, chưa chạy hoặc chứng minh
native tốt hơn/ít token hơn. Giữ quyền tự chọn persona và không ép quota tương tác.

Nguồn: https://github.com/camel-ai/oasis và https://docs.oasis.camel-ai.org/;
implementation pin: oasis/social_agent/agent.py, agent_environment.py,
oasis/environment/env.py, examples/different_model_simulation.py.
