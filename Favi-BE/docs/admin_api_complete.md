# ?? Admin Portal API - Complete Specification

## Overview

Tài li?u này t?ng h?p t?t c? API endpoints cho Admin Portal.

---

## 1. User Management (`/api/admin/users`)

### 1.1 Single User Actions

| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/api/admin/users/{profileId}/ban` | POST | Ban a single user |
| `/api/admin/users/{profileId}/ban` | DELETE | Unban a single user |
| `/api/admin/users/{profileId}/warn` | POST | Warn a single user |

### 1.2 Bulk User Actions

| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/api/admin/users/bulk/ban` | POST | Ban multiple users (max 100) |
| `/api/admin/users/bulk/unban` | POST | Unban multiple users (max 100) |
| `/api/admin/users/bulk/warn` | POST | Warn multiple users (max 100) |

#### `POST /api/admin/users/{profileId}/ban`

**Request Body**:
```json
{
  "reason": "Spam content",
  "durationDays": 30
}
```

**Response**: `200 OK`
```json
{
  "id": "moderation-guid",
  "profileId": "user-guid",
  "actionType": "Ban",
  "reason": "Spam content",
  "createdAt": "2024-01-15T10:00:00Z",
  "expiresAt": "2024-02-14T10:00:00Z",
  "revokedAt": null,
  "active": true,
  "adminActionId": "audit-guid",
  "adminId": "admin-guid"
}
```

#### `POST /api/admin/users/bulk/ban`

**Request Body**:
```json
{
  "profileIds": ["guid-1", "guid-2", "guid-3"],
  "reason": "Spam content across multiple posts",
  "durationDays": 30
}
```

**Response**: `200 OK`
```json
{
  "totalRequested": 3,
  "successCount": 2,
  "failedCount": 1,
  "results": [
    { "id": "guid-1", "success": true, "error": null },
    { "id": "guid-2", "success": true, "error": null },
    { "id": "guid-3", "success": false, "error": "User is already banned" }
  ]
}
```

---

## 2. Content Management (`/api/admin/content`)

### 2.1 Single Content Actions

| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/api/admin/content/posts/{id}` | DELETE | Delete a post (soft delete) |
| `/api/admin/content/comments/{id}` | DELETE | Delete a comment (hard delete) |

### 2.2 Bulk Content Actions

| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/api/admin/content/posts/bulk/delete` | POST | Delete multiple posts (max 100) |
| `/api/admin/content/comments/bulk/delete` | POST | Delete multiple comments (max 100) |

#### `DELETE /api/admin/content/posts/{id}`

**Request Body**:
```json
{
  "reason": "Violates community guidelines"
}
```

**Response**: `200 OK`
```json
{
  "message": "Bài vi?t ?ã ???c xóa b?i Admin."
}
```

#### `POST /api/admin/content/posts/bulk/delete`

**Request Body**:
```json
{
  "postIds": ["post-guid-1", "post-guid-2"],
  "reason": "Violates community guidelines"
}
```

**Response**: `200 OK` (BulkActionResponse)

---

## 3. Report Management (`/api/admin/reports`)

### 3.1 Query Reports

| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/api/admin/reports` | GET | Get all reports with pagination |
| `/api/admin/reports/status/{status}` | GET | Get reports by status |
| `/api/admin/reports/target-type/{targetType}` | GET | Get reports by target type |
| `/api/admin/reports/target/{targetId}` | GET | Get reports for a specific target |

### 3.2 Single Report Actions

| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/api/admin/reports/{id}/status` | PUT | Update report status |
| `/api/admin/reports/{id}/resolve` | POST | Resolve a report |
| `/api/admin/reports/{id}/reject` | POST | Reject a report |

### 3.3 Bulk Report Actions

| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/api/admin/reports/bulk/resolve` | POST | Resolve multiple reports (max 100) |
| `/api/admin/reports/bulk/reject` | POST | Reject multiple reports (max 100) |

#### `GET /api/admin/reports?page=1&pageSize=20`

**Response**: `200 OK`
```json
{
  "items": [
    {
      "id": "report-guid",
      "reporterId": "user-guid",
      "targetType": "Post",
      "targetId": "post-guid",
      "reason": "Spam",
      "status": "Pending",
      "createdAt": "2024-01-15T10:00:00Z",
      "actedAt": null,
      "data": null
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 45
}
```

#### `POST /api/admin/reports/{id}/resolve`

**Response**: `200 OK`
```json
{
  "message": "Report resolved successfully."
}
```

#### `POST /api/admin/reports/bulk/resolve`

**Request Body**:
```json
{
  "reportIds": ["report-guid-1", "report-guid-2"],
  "newStatus": "Resolved"
}
```

**Response**: `200 OK` (BulkActionResponse)

---

## 4. Audit Logs (`/api/admin/audit`)

| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/api/admin/audit` | GET | Get paginated audit logs |
| `/api/admin/audit/{id}` | GET | Get a specific audit log |
| `/api/admin/audit/action-types` | GET | Get all action types |
| `/api/admin/audit/summary` | GET | Get action type summary |

See `docs/admin_phase_2.md` for details.

---

## 5. Analytics & Charts (`/api/admin/analytics`)

### 5.1 Dashboard Stats

| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/api/admin/analytics` | GET | Dashboard overview |
| `/api/admin/analytics/users` | GET | Paginated users list |
| `/api/admin/analytics/posts` | GET | Paginated posts list |

### 5.2 Charts

| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/api/admin/analytics/charts/growth` | GET | Growth chart |
| `/api/admin/analytics/charts/user-activity` | GET | User activity chart |
| `/api/admin/analytics/charts/content-activity` | GET | Content activity chart |
| `/api/admin/analytics/charts/user-roles` | GET | User roles distribution |
| `/api/admin/analytics/charts/user-status` | GET | User status distribution |
| `/api/admin/analytics/charts/post-privacy` | GET | Post privacy distribution |
| `/api/admin/analytics/charts/report-status` | GET | Report status distribution |

### 5.3 Top Entities & Comparison

| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/api/admin/analytics/top-users` | GET | Top users by engagement |
| `/api/admin/analytics/top-posts` | GET | Top posts by engagement |
| `/api/admin/analytics/comparison` | GET | Period comparison |

See `docs/admin_phase_3.md` for details.

---

## 6. Bulk Actions Summary (`/api/admin/bulk`)

Legacy endpoints (kept for backward compatibility):

| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/api/admin/bulk/users/ban` | POST | Same as `/api/admin/users/bulk/ban` |
| `/api/admin/bulk/users/unban` | POST | Same as `/api/admin/users/bulk/unban` |
| `/api/admin/bulk/users/warn` | POST | Same as `/api/admin/users/bulk/warn` |
| `/api/admin/bulk/posts/delete` | POST | Same as `/api/admin/content/posts/bulk/delete` |
| `/api/admin/bulk/comments/delete` | POST | Same as `/api/admin/content/comments/bulk/delete` |
| `/api/admin/bulk/reports/resolve` | POST | Same as `/api/admin/reports/bulk/resolve` |

See `docs/admin_phase_4.md` for details.

---

## 7. Export Data (`/api/admin/export`)

| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/api/admin/export/users` | GET | Export users data (CSV/JSON/Excel) |
| `/api/admin/export/posts` | GET | Export posts data |
| `/api/admin/export/reports` | GET | Export reports data |
| `/api/admin/export/audit-logs` | GET | Export audit logs |
| `/api/admin/export/charts/growth` | GET | Export growth chart data |
| `/api/admin/export/dashboard-summary` | GET | Export dashboard summary (JSON) |

See `docs/admin_phase_5.md` for details.

---

## 8. TypeScript Interfaces

```typescript
// ===== User Moderation =====

interface BanUserRequest {
  reason: string;
  durationDays?: number | null;
}

interface UnbanUserRequest {
  reason?: string;
}

interface WarnUserRequest {
  reason: string;
}

interface UserModerationResponse {
  id: string;
  profileId: string;
  actionType: 'Ban' | 'Warn';
  reason: string;
  createdAt: string;
  expiresAt: string | null;
  revokedAt: string | null;
  active: boolean;
  adminActionId: string;
  adminId: string;
}

// ===== Content Moderation =====

interface AdminDeleteContentRequest {
  reason: string;
}

// ===== Reports =====

interface ReportResponse {
  id: string;
  reporterId: string;
  targetType: 'Post' | 'Comment' | 'User';
  targetId: string;
  reason: string;
  status: 'Pending' | 'Resolved' | 'Rejected';
  createdAt: string;
  actedAt: string | null;
  data: any | null;
}

interface UpdateReportStatusRequest {
  newStatus: 'Resolved' | 'Rejected';
}

// ===== Bulk Actions =====

interface BulkBanRequest {
  profileIds: string[];
  reason: string;
  durationDays?: number | null;
}

interface BulkUnbanRequest {
  profileIds: string[];
  reason?: string;
}

interface BulkWarnRequest {
  profileIds: string[];
  reason: string;
}

interface BulkDeletePostsRequest {
  postIds: string[];
  reason: string;
}

interface BulkDeleteCommentsRequest {
  commentIds: string[];
  reason: string;
}

interface BulkResolveReportsRequest {
  reportIds: string[];
  newStatus: 'Resolved' | 'Rejected';
}

interface BulkRejectReportsRequest {
  reportIds: string[];
}

interface BulkActionItemResult {
  id: string;
  success: boolean;
  error: string | null;
}

interface BulkActionResponse {
  totalRequested: number;
  successCount: number;
  failedCount: number;
  results: BulkActionItemResult[];
}

// ===== Export Types =====

type ExportFormat = 'csv' | 'json' | 'excel';

interface ExportUsersRequest {
  search?: string;
  role?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
  format?: ExportFormat;
}

interface ExportPostsRequest {
  search?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
  format?: ExportFormat;
}

interface ExportReportsRequest {
  status?: string;
  targetType?: string;
  fromDate?: string;
  toDate?: string;
  format?: ExportFormat;
}

interface ExportAuditLogsRequest {
  actionType?: string;
  adminId?: string;
  fromDate?: string;
  toDate?: string;
  format?: ExportFormat;
}
```

---

## 9. API Service Implementation

```typescript
// lib/api/admin.ts

import { apiClient } from './client';

export const adminApi = {
  // ===== USER MANAGEMENT =====
  
  // Single actions
  banUser: (profileId: string, request: BanUserRequest) =>
    apiClient.post(`/api/admin/users/${profileId}/ban`, request),
  
  unbanUser: (profileId: string, request?: UnbanUserRequest) =>
    apiClient.delete(`/api/admin/users/${profileId}/ban`, { data: request }),
  
  warnUser: (profileId: string, request: WarnUserRequest) =>
    apiClient.post(`/api/admin/users/${profileId}/warn`, request),
  
  // Bulk actions
  bulkBanUsers: (request: BulkBanRequest) =>
    apiClient.post<BulkActionResponse>('/api/admin/users/bulk/ban', request),
  
  bulkUnbanUsers: (request: BulkUnbanRequest) =>
    apiClient.post<BulkActionResponse>('/api/admin/users/bulk/unban', request),
  
  bulkWarnUsers: (request: BulkWarnRequest) =>
    apiClient.post<BulkActionResponse>('/api/admin/users/bulk/warn', request),

  // ===== CONTENT MANAGEMENT =====
  
  // Single actions
  deletePost: (postId: string, request: AdminDeleteContentRequest) =>
    apiClient.delete(`/api/admin/content/posts/${postId}`, { data: request }),
  
  deleteComment: (commentId: string, request: AdminDeleteContentRequest) =>
    apiClient.delete(`/api/admin/content/comments/${commentId}`, { data: request }),
  
  // Bulk actions
  bulkDeletePosts: (request: BulkDeletePostsRequest) =>
    apiClient.post<BulkActionResponse>('/api/admin/content/posts/bulk/delete', request),
  
  bulkDeleteComments: (request: BulkDeleteCommentsRequest) =>
    apiClient.post<BulkActionResponse>('/api/admin/content/comments/bulk/delete', request),

  // ===== REPORT MANAGEMENT =====
  
  // Query
  getReports: (page = 1, pageSize = 20) =>
    apiClient.get<PagedResult<ReportResponse>>('/api/admin/reports', { params: { page, pageSize } }),
  
  getReportsByStatus: (status: string, page = 1, pageSize = 20) =>
    apiClient.get<PagedResult<ReportResponse>>(`/api/admin/reports/status/${status}`, { params: { page, pageSize } }),
  
  getReportsByTargetType: (targetType: string, page = 1, pageSize = 20) =>
    apiClient.get<PagedResult<ReportResponse>>(`/api/admin/reports/target-type/${targetType}`, { params: { page, pageSize } }),
  
  // Single actions
  resolveReport: (reportId: string) =>
    apiClient.post(`/api/admin/reports/${reportId}/resolve`),
  
  rejectReport: (reportId: string) =>
    apiClient.post(`/api/admin/reports/${reportId}/reject`),
  
  // Bulk actions
  bulkResolveReports: (request: BulkResolveReportsRequest) =>
    apiClient.post<BulkActionResponse>('/api/admin/reports/bulk/resolve', request),
  
  bulkRejectReports: (request: BulkRejectReportsRequest) =>
    apiClient.post<BulkActionResponse>('/api/admin/reports/bulk/reject', request),

  // ===== EXPORT DATA =====
  
  exportUsers: (request: ExportUsersRequest) =>
    apiClient.get('/api/admin/export/users', { params: request, responseType: 'blob' }),
  
  exportPosts: (request: ExportPostsRequest) =>
    apiClient.get('/api/admin/export/posts', { params: request, responseType: 'blob' }),
  
  exportReports: (request: ExportReportsRequest) =>
    apiClient.get('/api/admin/export/reports', { params: request, responseType: 'blob' }),
  
  exportAuditLogs: (request: ExportAuditLogsRequest) =>
    apiClient.get('/api/admin/export/audit-logs', { params: request, responseType: 'blob' }),
  
  exportGrowthChart: () =>
    apiClient.get('/api/admin/export/charts/growth', { responseType: 'blob' }),
  
  exportDashboardSummary: () =>
    apiClient.get('/api/admin/export/dashboard-summary', { responseType: 'blob' }),
};
```

---

## 10. Summary Table

| Category | Single Actions | Bulk Actions | Query | Export | Total |
|----------|---------------|--------------|-------|--------|-------|
| Users | 3 | 3 | 0 | 1 | 7 |
| Content | 2 | 2 | 0 | 1 | 5 |
| Reports | 3 | 2 | 4 | 1 | 10 |
| Audit | 2 | 0 | 2 | 1 | 5 |
| Analytics | 0 | 0 | 13 | 2 | 15 |
| Bulk (Legacy) | 0 | 6 | 0 | 0 | 6 |
| **Total** | **10** | **13** | **19** | **6** | **48** |

---

## Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2024-01-15 | Initial specification |
| 1.1.0 | 2024-01-15 | Added bulk actions to user/content/report controllers |
| 1.2.0 | 2024-01-15 | Added Phase 5: Export Data APIs |
