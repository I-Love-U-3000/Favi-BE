# Nghiên cứu liên quan và giới hạn đóng góp

Đối chiếu nguồn gốc ngày 08/11/2026 trước confirmatory run. Đây là bảng nền
cho thiết kế, chưa phải systematic literature review hoặc bằng chứng tính mới.

| Công trình và nguồn gốc | Bài toán, tín hiệu và đối chứng | Giới hạn với Favi và quyết định thiết kế |
|---|---|---|
| [Rashid và cộng sự, Getting to Know You, IUI 2002](https://experts.umn.edu/en/publications/getting-to-know-you-learning-new-user-preferences-in-recommender-/) | New-user elicitation: sáu cách chọn item để hỏi rating, gồm information/aggregate/personalized strategies; offline và live study hơn 300 users | Hỏi rating khác chọn topic; Favi giữ topic-only/general và robustness, không coi topic onboarding đã được user-study xác thực |
| [Rashid, Karypis, Riedl, Learning Preferences of New Users, SIGKDD Explorations 2008](https://www.kdd.org/exploration_files/WebKDD08-Al-Rashid.pdf) | Information-theoretic elicitation, chọn item để cân bằng thông tin và khả năng user có ý kiến | Active item elicitation khác sidecar topic; ngưỡng {1,5} chỉ là finite search, không phải định luật về mức đủ lịch sử |
| [Burke, Hybrid Recommender Systems: A Comparative Study, technical report](https://facweb.cs.depaul.edu/research/TechReports/abstract06012.htm) | Bốn kỹ thuật/bảy cách hybrid, 53 hybrid thực nghiệm; nêu lợi ích cascade/augmentation khi thành phần bổ sung nhau | Hybrid nói chung không xác thực router Favi; đây là technical report/chapter, không gán nhầm thành Survey and Experiments 2002. Đóng góp dự kiến là đo điểm chuyển/serving, không phát minh hybrid |
| [Schein và cộng sự, Methods and Metrics for Cold-Start Recommendations, SIGIR 2002, author postprint](https://www.di.fc.ul.pt/~tc/projs/immersive/2_Andrew.pdf) | Probabilistic content+collaborative, so naive Bayes cho item chưa có rating, ba methodology và heuristic baselines | New-item khác new-user cold start. Giữ Formula/Popularity, phân biệt hai loại cold trong report và HTTP fixtures |
| [Ji và cộng sự, A Critical Study on Data Leakage](https://arxiv.org/abs/2010.11060) | Global timeline; BPR/NeuMF/SASRec/LightGCN với leave-last-one-out trên bốn datasets; future information có thể đổi model ranking | Không hiệu chỉnh exposure. Favi lọc arrival/item/preference theo request, selector chỉ nhận prefix <400, refit <400 |
| [Schnabel và cộng sự, Recommendations as Treatments, ICML 2016](https://proceedings.mlr.press/v48/schnabel16.pdf) | Propensity weighting cho evaluation/learning dưới selection bias; so estimator thông thường và matrix factorization | Phụ thuộc assumptions/propensities; Favi chưa triển khai estimator đó. Báo MNAR (missing not at random), không gọi replay là unbiased metric hoặc causal uplift |
| [Steck, EASE, WWW 2019](https://arxiv.org/abs/1905.03375) | Linear implicit-feedback model, closed-form solution; so CF gồm deep nonlinear baselines | Khác corpus/tín hiệu, không suy ra winner. Giữ grid lambda4 và đo dense memory/catalog limit |
| [He và cộng sự, LightGCN, SIGIR 2020](https://arxiv.org/abs/2002.02126) | Linear neighborhood propagation trên user-item graph; so NGCF và ablation transformation/nonlinearity | CPU full-batch/grid/synthetic Favi không tái lập paper scores. Báo epochs/optimizer seeds, không coi graph mặc nhiên hơn EASE/PPR |

Suy luận thiết kế của dự án: câu hỏi có thể bảo vệ là khi nào chuyển từ
topic/general sang CF dưới contract thời gian và ngân sách serving cụ thể.
Chưa có kết quả thì không tuyên bố router tốt hơn, thuật toán mới hoặc trải
nghiệm thật cải thiện. PPR là exact bipartite baseline của code hiện có, không
tự nhận tái lập biến thể paper chưa đối chiếu. Không chép lại paper vào repo.
