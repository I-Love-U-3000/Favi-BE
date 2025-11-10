# Favi Backend Overview

## Kiến trúc tổng thể
- Frontend: Next.js
- Backend: .NET 8 (C#, EF Core)
- Database: PostgreSQL (lưu metadata)
- Auth & Realtime: Supabase
- Image Storage: Cloudinary

## Nguyên tắc thiết kế
- Notifications, Messages, Conversations: **không lưu EF Core**, chỉ realtime (Supabase Realtime).
- API tuân thủ REST, chuẩn hóa response.
- Áp dụng Clean Architecture.

## Sơ đồ (khái niệm)
Frontend <-> Backend (.NET API) <-> PostgreSQL + Cloudinary + Supabase (Auth/Realtime)
