# Tối ưu hoá Feed (Big Strategy + Small Tactics)

## Mục tiêu
- Feed nhanh, scale tốt, vẫn cá nhân hoá.
- Không giới hạn bởi một pool nhỏ cố định.
- Tránh tính score nặng trên mỗi request.

---

## Tổng quan chiến lược (Big Strategy)

### 1) Hybrid Feed = Global Pool + Personal Pool
- **Global Pool**: top trending toàn hệ thống (làm nền)
- **Personal Pool**: bài từ followees + bài liên quan user (cá nhân hoá)
- **Mix**: mỗi page trộn 3–4 bài personal + còn lại global
- **Dedupe**: loại trùng theo `PostId`

### 2) Rerank theo batch (15 phút/lần)
- Không tính score realtime mỗi request
- Tính theo batch định kỳ (cron/worker)
- Chỉ cập nhật những posts mới/đang hot (partial update)
#### Lưu ý: tôi chọn 15p vì tôi nghĩ với mấy chục nghìn rows của Posts trong PgDb thì việc tính lại score toàn bộ sẽ mất tầm 5–10 phút, nên 15 phút là hợp lý để đảm bảo feed luôn fresh mà không quá nặng. Nếu thao tác này nặng hơn thì con số 15p sẽ đổi thành lâu hơn. Nếu scale lên vài triệu posts thì có thể cần 30p–1h, nhưng tôi nghĩ với app hiện tại thì 15p là ổn.
### 3) Cache server-side (không client-side)
- Cache global/personal bằng Redis hoặc MemoryCache
- Client chỉ cache state đã scroll để tránh duplicate

---

## Tactics chi tiết (Small Strategy)

### A) Partial Rerank (delta update)
**Ý tưởng:** chỉ tính lại các post có tương tác mới.

**Cách làm:**
1. Khi có like/comment/repost → push `PostId` vào Redis Set (unique)
2. Worker 15 phút/lần:
   - Lấy tất cả `PostId` từ set
   - Tính lại score
   - Cập nhật cache
   - Clear set

**Ưu điểm:**
- Không scan toàn DB
- Không query nhiều bảng theo timestamp

---

### B) Personal Pool cache (TTL 10–15 phút)
**Nguồn:**
- Posts từ followees
- Posts user tương tác gần đây
- Posts có tag/user interest

**Refresh:**
- Khi user login
- Hoặc worker refresh định kỳ

**Lưu ý:**
- Không cache client-side để tránh stale state
- Dedupe với global pool trước khi trả response

---

### C) Mixing Rule (đề xuất)
```
PageSize = 20
- 16 global
- 4 personal
- Nếu personal trùng → thay bằng global
```

---

## Lịch rerank đề xuất (theo scale)
- < 1M posts: 5–10 phút
- 1–10M posts: 15–30 phút
- > 10M posts: 30–60 phút

**App hiện tại:** 15 phút + partial update = fit

---

## Redis keys gợi ý
- `feed:dirty_posts` (Set) → list `PostId` cần rerank
- `feed:global:top` (List/ZSet) → global pool
- `feed:personal:{userId}` (List/ZSet) → personal pool

---

## Tóm tắt ngắn
- **Hybrid feed** là hướng đúng
- **15 phút rerank + partial update** là tối ưu cho app hiện tại
- **Redis Set** cho dirty posts giúp giảm query nặng
- **Cache server-side** là bắt buộc nếu muốn scale
