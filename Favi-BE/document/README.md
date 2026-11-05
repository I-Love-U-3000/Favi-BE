# Favi-BE Technical Documentation

- [Milestones adaptive và cách đọc báo cáo tiến độ](ADAPTIVE_MILESTONES.md)

- [Execution plan: nghiên cứu và triển khai gợi ý thích ứng cold → warm](ADAPTIVE_RECOMMENDER_EXECUTION_PLAN.md)

- [Agent tự chọn danh tính, cảm xúc, sự kiện và hành động](FREE_SOCIAL_AGENTS.md)

- [OASIS native: CAMEL memory, TwHIN-BERT và model local GPU](OASIS_NATIVE_FULL_RUN.md)

- [Flow mặc định sinh dataset cho benchmark recommender và graph](DATA_GENERATION_FLOW.md)

- [Bộ dữ liệu Favi dùng chung cho benchmark: favi-5k-v1](FAVI_BENCHMARK_DATASET.md)

- [Các quy mô dữ liệu Favi: 2.000, 5.000 và 10.000 người](FAVI_DATASET_SCALES.md)

- [Benchmark Popularity và công thức feed Favi hiện có](RECOMMENDER_BENCHMARK.md)

- [Formula V2: tuning bằng validation và benchmark so với V1](FORMULA_V2_BENCHMARK.md)

- [Mở rộng benchmark: EASE, Personalized PageRank và LightGCN](EXPANDED_RECOMMENDER_BENCHMARK.md)

- [Benchmark cùng protocol trên ba bộ dữ liệu 2k, 5k và 10k](THREE_SCALE_RECOMMENDER_BENCHMARK.md)

- [Bước 1: độ ổn định recommender trên ba dataset seed cùng quy mô 5k](MULTISEED_RECOMMENDER_BENCHMARK.md)

- [Bước 2: kiểm chứng trên MovieLens độc lập với simulator](EXTERNAL_RECOMMENDER_BENCHMARK.md)

- [Bước 3: feed thử nghiệm EASE/PPR và kiểm thử HTTP thật](EXPERIMENTAL_RECOMMENDER_FEED.md)

- [Cộng đồng nhỏ: phân phối hành vi, trạng thái tâm lý giả lập và ngân sách LLM](SMALL_SOCIAL_COMMUNITY.md)

- [OASIS engine thật: simulation → PostgreSQL → API Favi](OASIS_SIMULATOR_VERIFICATION.md)

- [OASIS adapter và kiểm chứng tích hợp PostgreSQL/API](OASIS_ADAPTER.md)

- [Đề tài nghiên cứu và tiến độ sau ba feature](RESEARCH_STATUS.md)

- [Cold-start feed và onboarding chủ đề](COLD_START_FEED.md)

Tài liệu kỹ thuật chính của Favi-BE được lưu trữ tập trung tại thư mục gốc:
👉 **[../../document/README.md](../../document/README.md)**

### Danh mục tài liệu:
- [00 — Tổng Quan Hệ Thống & Kiến Trúc Tổng Thể](../../document/00_OVERVIEW_AND_ARCHITECTURE.md)
- [01 — Bounded Contexts, Danh Mục Module & Trách Nhiệm](../../document/01_MODULES_AND_BOUNDED_CONTEXTS.md)
- [02 — Cấu Trúc Phân Tầng, MediatR Pipeline & Design Patterns](../../document/02_LAYERS_AND_DESIGN_PATTERNS.md)
- [03 — Cơ Chế Idempotency & Độ Tin Cậy Dữ Liệu](../../document/03_IDEMPOTENCY_AND_RELIABILITY.md)
- [04 — NGINX API Gateway, Load Balancing & Mạng Lưới](../../document/04_NETWORKING_GATEWAY_AND_LOAD_BALANCING.md)
- [05 — Mở Rộng Hệ Thống, Hiệu Năng & Giám Sát](../../document/05_SCALING_PERFORMANCE_AND_OBSERVABILITY.md)
- [06 — Quản Lý Phiên Bản (Git), Migrations & Seed Pipeline](../../document/06_VERSION_CONTROL_AND_DATABASE_MIGRATIONS.md)
- [07 — Chiến Lược Kiểm Thử, Hiệu Năng & Đo Lường Tải](../../document/07_TESTING_AND_BENCHMARKING.md)
