# Plan cold-start feed

Branch `feat/cold-start-feed`, base main `155d6be`, không chứa OASIS/moderation.

1. Auth sở hữu ProfileInterests: một hàng/profile, topics text[], version để chống
   ghi đè lựa chọn. GET/PUT self-only qua MediatR; PUT yêu cầu Idempotency-Key.
2. Catalog cố định 10 topic theo seed hiện có; [] nghĩa là general. Cho phép 1–10
   topic khi onboarding. Chưa thay registration/frontend; gọi PUT sau đăng nhập.
3. Cold = chưa có post Like/Love hoặc bài đã lưu trong collection. Warm giữ luồng cũ.
4. ContentDiscovery đọc preference qua query Auth tại host, không reference module.
5. Graph engine chưa tồn tại: IGraphCandidateSource trả PostId/Score. Default trả
   rỗng rõ ràng, không giả có model. Host adapter sau này map output thật tại một chỗ.
6. Mỗi topic lấy pool nhỏ public/safe/active từ database; round-robin topic, dedup,
   ưu tiên đa tác giả, fallback khi thiếu nội dung. Không training/vector/network mặc định.
7. Nối cả /feed và /feed-with-reposts; cold feed giữ thứ tự recommendation, không
   sort lại CreatedAt. Recheck quyền khi hydrate, lọc trước phân trang.
8. Migration cộng thêm bảng, test ranking/validation/handler/adapter nếu có DB fake,
   build/test, handoff nêu rõ contract engine có thể đổi và các file phải thay.

Phát hiện trong khi generate migration: model main đã có các Version/UpdatedAt
và index Posts chưa được migrations/snapshot mô tả. Đồng bộ prerequisite bằng
ADD IF NOT EXISTS, giữ các column/index cũ khi Down, tránh DROP hoặc default sai.
EF design-time factory tách tooling khỏi startup migration/seed/Redis của API.

## Gate tích hợp đã bổ sung

`tools/cold-start-smoke/verify.ps1` chạy build/unit/architecture và HTTP E2E trên
hai schema từ main có dữ liệu, gồm rollback/re-upgrade, concurrent insert/update
và cold ↔ warm. Workflow `.github/workflows/cold-start.yml` tự động hóa gate đó.
Review, rollback và điều kiện rollout nằm trong [COLD_START_RELEASE_REVIEW.md](COLD_START_RELEASE_REVIEW.md).
