# ??? Admin Portal - Ultimate Implementation Plan

## ?? T?ng quan

Tài li?u này t?ng h?p toàn b? k? ho?ch tri?n khai Admin Portal cho h? th?ng Favi, bao g?m Backend API ?ã hoàn thành và h??ng d?n tri?n khai Frontend.

---

## ?? T?ng k?t Implementation Status

### ? Backend API - HOÀN THÀNH 100%

| Phase | Mô t? | Status | Endpoints |
|-------|-------|--------|-----------|
| Phase 1 | Dashboard Stats & User/Post Management | ? Done | 9 |
| Phase 2 | Audit Logs | ? Done | 4 |
| Phase 3 | Dashboard Charts | ? Done | 10 |
| Phase 4 | Bulk Actions | ? Done | 13 |
| Phase 5 | Export Data | ? Done | 6 |

**T?ng c?ng: 42 API endpoints**

---

## ??? C?u trúc API Endpoints

### 1. User Management (`/api/admin/users`)

| Endpoint | Method | Mô t? | Type |
|----------|--------|-------|------|
| `/{profileId}/ban` | POST | Ban user | Single |
| `/{profileId}/ban` | DELETE | Unban user | Single |
| `/{profileId}/warn` | POST | Warn user | Single |
| `/bulk/ban` | POST | Ban nhi?u users (max 100) | Bulk |
| `/bulk/unban` | POST | Unban nhi?u users | Bulk |
| `/bulk/warn` | POST | Warn nhi?u users | Bulk |

**Controller**: `AdminUsersController.cs`
**Service**: `UserModerationService.cs`, `BulkActionService.cs`

---

### 2. Content Management (`/api/admin/content`)

| Endpoint | Method | Mô t? | Type |
|----------|--------|-------|------|
| `/posts/{id}` | DELETE | Xóa post (soft delete) | Single |
| `/comments/{id}` | DELETE | Xóa comment (hard delete) | Single |
| `/posts/bulk/delete` | POST | Xóa nhi?u posts | Bulk |
| `/comments/bulk/delete` | POST | Xóa nhi?u comments | Bulk |

**Controller**: `AdminContentController.cs`
**Service**: `PostService.cs`, `CommentService.cs`, `BulkActionService.cs`

---

### 3. Report Management (`/api/admin/reports`)

| Endpoint | Method | Mô t? | Type |
|----------|--------|-------|------|
| `/` | GET | Get all reports (paginated) | Query |
| `/status/{status}` | GET | Get reports by status | Query |
| `/target-type/{type}` | GET | Get reports by target type | Query |
| `/target/{targetId}` | GET | Get reports for a target | Query |
| `/{id}/status` | PUT | Update report status | Single |
| `/{id}/resolve` | POST | Resolve report | Single |
| `/{id}/reject` | POST | Reject report | Single |
| `/bulk/resolve` | POST | Resolve nhi?u reports | Bulk |
| `/bulk/reject` | POST | Reject nhi?u reports | Bulk |

**Controller**: `AdminReportsController.cs`
**Service**: `ReportService.cs`, `BulkActionService.cs`

---

### 4. Audit Logs (`/api/admin/audit`)

| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/` | GET | Get paginated audit logs with filters |
| `/{id}` | GET | Get specific audit log |
| `/action-types` | GET | Get all action types |
| `/summary` | GET | Get action type summary |

**Controller**: `AdminAuditController.cs`
**Service**: `AuditService.cs`

---

### 5. Analytics & Charts (`/api/admin/analytics`)

| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/` | GET | Dashboard overview stats |
| `/users` | GET | Paginated users list |
| `/posts` | GET | Paginated posts list |
| `/charts/growth` | GET | Growth chart data |
| `/charts/user-activity` | GET | User activity chart |
| `/charts/content-activity` | GET | Content activity chart |
| `/charts/user-roles` | GET | User roles distribution |
| `/charts/user-status` | GET | User status distribution |
| `/charts/post-privacy` | GET | Post privacy distribution |
| `/charts/report-status` | GET | Report status distribution |
| `/top-users` | GET | Top users by engagement |
| `/top-posts` | GET | Top posts by engagement |
| `/comparison` | GET | Period comparison |

**Controller**: `AdminAnalyticsController.cs`
**Service**: `AnalyticsService.cs`

---

### 6. Export Data (`/api/admin/export`)

| Endpoint | Method | Formats | Mô t? |
|----------|--------|---------|-------|
| `/users` | GET | CSV/JSON/Excel | Export users |
| `/posts` | GET | CSV/JSON/Excel | Export posts |
| `/reports` | GET | CSV/JSON/Excel | Export reports |
| `/audit-logs` | GET | CSV/JSON/Excel | Export audit logs |
| `/charts/growth` | GET | CSV/JSON/Excel | Export growth data |
| `/dashboard-summary` | GET | JSON | Export summary |

**Controller**: `AdminExportController.cs`
**Service**: `ExportService.cs`

---

### 7. Legacy Bulk Endpoints (`/api/admin/bulk`)

> ?? Kept for backward compatibility. Recommend using primary endpoints.

| Endpoint | Method | Redirect To |
|----------|--------|-------------|
| `/users/ban` | POST | `/api/admin/users/bulk/ban` |
| `/users/unban` | POST | `/api/admin/users/bulk/unban` |
| `/users/warn` | POST | `/api/admin/users/bulk/warn` |
| `/posts/delete` | POST | `/api/admin/content/posts/bulk/delete` |
| `/comments/delete` | POST | `/api/admin/content/comments/bulk/delete` |
| `/reports/resolve` | POST | `/api/admin/reports/bulk/resolve` |

**Controller**: `AdminBulkController.cs`

---

## ??? Backend Architecture

### Controllers (7 files)

```
Favi-BE.API/Controllers/
??? AdminUsersController.cs       # User moderation
??? AdminContentController.cs     # Content moderation
??? AdminReportsController.cs     # Report management
??? AdminAuditController.cs       # Audit logs
??? AdminAnalyticsController.cs   # Analytics & charts
??? AdminExportController.cs      # Data export
??? AdminBulkController.cs        # Legacy bulk endpoints
```

### Services (6 files)

```
Favi-BE.API/Services/
??? UserModerationService.cs      # Ban/Unban/Warn logic
??? BulkActionService.cs          # Bulk operations
??? AuditService.cs               # Audit logging
??? AnalyticsService.cs           # Stats & charts
??? ExportService.cs              # File generation
??? ReportService.cs              # Report handling
```

### Interfaces (6 files)

```
Favi-BE.API/Interfaces/Services/
??? IUserModerationService.cs
??? IBulkActionService.cs
??? IAuditService.cs
??? IAnalyticsService.cs
??? IExportService.cs
??? IReportService.cs
```

### DTOs (6 files)

```
Favi-BE.API/Models/Dtos/
??? AdminModerationDtos.cs        # Ban/Warn DTOs
??? BulkActionDtos.cs             # Bulk operation DTOs
??? AuditLogDtos.cs               # Audit log DTOs
??? AnalyticsDtos.cs              # Chart & stats DTOs
??? ExportDtos.cs                 # Export DTOs
??? ReportDtos.cs                 # Report DTOs
```

### Enums (2 files updated)

```
Favi-BE.API/Models/Enums/
??? AdminActionType.cs            # BanUser, UnbanUser, WarnUser, ResolveReport, DeleteContent, ExportData
??? ModerationActionType.cs       # Ban, Warn
```

---

## ?? Authorization

### Policy: `RequireAdmin`

```csharp
[Authorize(Policy = AdminPolicies.RequireAdmin)]
```

- Ki?m tra user có role `admin` trong JWT token
- Implemented via `RequireAdminHandler.cs`

---

## ?? Frontend Implementation Plan

### Tech Stack Recommendation

| Component | Technology |
|-----------|------------|
| Framework | Next.js 14+ (App Router) |
| UI Library | shadcn/ui |
| State Management | TanStack Query (React Query) |
| Charts | Recharts ho?c Tremor |
| Forms | React Hook Form + Zod |
| HTTP Client | Axios |
| Auth | NextAuth.js ho?c custom JWT |

---

### Frontend Folder Structure

```
admin-portal/
??? app/
?   ??? (auth)/
?   ?   ??? login/
?   ??? (dashboard)/
?   ?   ??? layout.tsx              # Admin layout with sidebar
?   ?   ??? page.tsx                # Dashboard overview
?   ?   ??? users/
?   ?   ?   ??? page.tsx            # Users list
?   ?   ?   ??? [id]/page.tsx       # User detail
?   ?   ??? posts/
?   ?   ?   ??? page.tsx            # Posts list
?   ?   ?   ??? [id]/page.tsx       # Post detail
?   ?   ??? reports/
?   ?   ?   ??? page.tsx            # Reports list
?   ?   ?   ??? [id]/page.tsx       # Report detail
?   ?   ??? audit/
?   ?   ?   ??? page.tsx            # Audit logs
?   ?   ??? settings/
?   ?       ??? page.tsx            # Admin settings
?   ??? api/                        # API routes (if needed)
??? components/
?   ??? ui/                         # shadcn components
?   ??? charts/
?   ?   ??? GrowthChart.tsx
?   ?   ??? UserStatusPieChart.tsx
?   ?   ??? ContentActivityChart.tsx
?   ?   ??? ...
?   ??? tables/
?   ?   ??? UsersTable.tsx
?   ?   ??? PostsTable.tsx
?   ?   ??? ReportsTable.tsx
?   ?   ??? AuditLogsTable.tsx
?   ??? modals/
?   ?   ??? BanUserModal.tsx
?   ?   ??? DeleteContentModal.tsx
?   ?   ??? BulkActionModal.tsx
?   ?   ??? ExportModal.tsx
?   ??? layout/
?       ??? Sidebar.tsx
?       ??? Header.tsx
?       ??? StatsCard.tsx
??? lib/
?   ??? api/
?   ?   ??? client.ts               # Axios instance
?   ?   ??? admin.ts                # Admin API calls
?   ?   ??? analytics.ts            # Analytics API
?   ?   ??? export.ts               # Export API
?   ?   ??? types.ts                # TypeScript interfaces
?   ??? utils/
?       ??? date.ts
?       ??? format.ts
??? hooks/
?   ??? useAuth.ts
?   ??? useBulkSelection.ts
?   ??? useExport.ts
?   ??? queries/
?       ??? useUsers.ts
?       ??? usePosts.ts
?       ??? useReports.ts
?       ??? useAuditLogs.ts
?       ??? useAnalytics.ts
??? types/
    ??? admin.d.ts
```

---

### Page Components Mapping

| Page | API Endpoints Used | Key Features |
|------|-------------------|--------------|
| Dashboard | `/analytics`, `/analytics/charts/*` | Stats cards, Charts, Top lists |
| Users | `/analytics/users`, `/users/*` | Table, Search, Filter, Ban/Unban |
| Posts | `/analytics/posts`, `/content/*` | Table, Search, Delete |
| Reports | `/reports/*` | Table, Filter by status, Resolve/Reject |
| Audit Logs | `/audit/*` | Table, Filter, Search |

---

### React Query Keys Structure

```typescript
const QUERY_KEYS = {
  // Dashboard
  dashboard: ['admin', 'dashboard'],
  dashboardStats: ['admin', 'analytics', 'stats'],
  
  // Users
  users: ['admin', 'users'],
  usersList: (filters) => ['admin', 'users', 'list', filters],
  
  // Posts
  posts: ['admin', 'posts'],
  postsList: (filters) => ['admin', 'posts', 'list', filters],
  
  // Reports
  reports: ['admin', 'reports'],
  reportsList: (filters) => ['admin', 'reports', 'list', filters],
  
  // Audit
  auditLogs: ['admin', 'audit'],
  auditLogsList: (filters) => ['admin', 'audit', 'list', filters],
  
  // Charts
  growthChart: (params) => ['admin', 'charts', 'growth', params],
  userActivityChart: (params) => ['admin', 'charts', 'user-activity', params],
  contentActivityChart: (params) => ['admin', 'charts', 'content-activity', params],
  
  // Distributions
  userRoles: ['admin', 'charts', 'user-roles'],
  userStatus: ['admin', 'charts', 'user-status'],
  postPrivacy: ['admin', 'charts', 'post-privacy'],
  reportStatus: ['admin', 'charts', 'report-status'],
  
  // Top entities
  topUsers: (limit) => ['admin', 'top-users', limit],
  topPosts: (limit) => ['admin', 'top-posts', limit],
};
```

---

## ?? TypeScript Interfaces

### Core Types

```typescript
// ===== Common =====

interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// ===== User Management =====

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

// ===== Analytics =====

interface DashboardStatsResponse {
  totalUsers: number;
  totalPosts: number;
  activeUsers: number;
  bannedUsers: number;
  pendingReports: number;
  todayPosts: number;
  todayUsers: number;
}

interface AnalyticsUserDto {
  id: string;
  username: string | null;
  displayName: string | null;
  avatarUrl: string | null;
  createdAt: string;
  lastActiveAt: string;
  isBanned: boolean;
  bannedUntil: string | null;
  role: string;
  postsCount: number;
  followersCount: number;
}

interface AnalyticsPostDto {
  id: string;
  profileId: string;
  authorUsername: string | null;
  authorDisplayName: string | null;
  caption: string | null;
  createdAt: string;
  privacy: string;
  commentsCount: number;
  reactionsCount: number;
  isDeleted: boolean;
}

// ===== Charts =====

interface TimeSeriesDataPoint {
  date: string;
  count: number;
}

interface LabeledDataPoint {
  label: string;
  count: number;
  percentage?: number;
}

type ChartInterval = 'day' | 'week' | 'month';

interface GrowthChartResponse {
  users: TimeSeriesDataPoint[];
  posts: TimeSeriesDataPoint[];
  reports: TimeSeriesDataPoint[];
  fromDate: string;
  toDate: string;
  interval: ChartInterval;
}

interface UserActivityChartResponse {
  newUsers: TimeSeriesDataPoint[];
  activeUsers: TimeSeriesDataPoint[];
  bannedUsers: TimeSeriesDataPoint[];
  fromDate: string;
  toDate: string;
  interval: ChartInterval;
}

interface ContentActivityChartResponse {
  posts: TimeSeriesDataPoint[];
  comments: TimeSeriesDataPoint[];
  reactions: TimeSeriesDataPoint[];
  fromDate: string;
  toDate: string;
  interval: ChartInterval;
}

interface UserRoleDistributionResponse {
  roles: LabeledDataPoint[];
  totalUsers: number;
}

interface UserStatusDistributionResponse {
  activeUsers: number;
  bannedUsers: number;
  inactiveUsers: number;
  totalUsers: number;
}

interface PostPrivacyDistributionResponse {
  privacyLevels: LabeledDataPoint[];
  totalPosts: number;
}

interface ReportStatusDistributionResponse {
  pending: number;
  resolved: number;
  rejected: number;
  totalReports: number;
}

// ===== Top Entities =====

interface TopUserDto {
  id: string;
  username: string | null;
  displayName: string | null;
  avatarUrl: string | null;
  postsCount: number;
  followersCount: number;
  reactionsReceived: number;
}

interface TopPostDto {
  id: string;
  authorId: string;
  authorUsername: string | null;
  caption: string | null;
  createdAt: string;
  reactionsCount: number;
  commentsCount: number;
}

// ===== Period Comparison =====

interface PeriodStats {
  newUsers: number;
  newPosts: number;
  newComments: number;
  newReactions: number;
  newReports: number;
  fromDate: string;
  toDate: string;
}

interface GrowthComparison {
  usersGrowthPercent: number;
  postsGrowthPercent: number;
  commentsGrowthPercent: number;
  reactionsGrowthPercent: number;
  reportsGrowthPercent: number;
}

interface PeriodComparisonResponse {
  currentPeriod: PeriodStats;
  previousPeriod: PeriodStats;
  growth: GrowthComparison;
}

// ===== Audit Logs =====

interface AuditLogDto {
  id: string;
  adminId: string;
  adminUsername: string | null;
  adminDisplayName: string | null;
  actionType: string;
  actionTypeDisplayName: string;
  targetProfileId: string | null;
  targetUsername: string | null;
  targetDisplayName: string | null;
  targetEntityId: string | null;
  targetEntityType: string | null;
  reportId: string | null;
  notes: string | null;
  createdAt: string;
}

interface AuditLogFilterRequest {
  actionType?: string;
  adminId?: string;
  targetProfileId?: string;
  fromDate?: string;
  toDate?: string;
  search?: string;
  page?: number;
  pageSize?: number;
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

// ===== Export =====

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

## ?? UI/UX Guidelines

### Color Palette

| Purpose | Color | Hex |
|---------|-------|-----|
| Primary | Blue | `#3B82F6` |
| Success/Active | Green | `#22C55E` |
| Warning | Yellow | `#F59E0B` |
| Error/Banned | Red | `#EF4444` |
| Neutral/Inactive | Gray | `#6B7280` |

### Chart Colors

| Series | Color | Hex |
|--------|-------|-----|
| Users | Blue | `#3B82F6` |
| Posts | Green | `#22C55E` |
| Comments | Purple | `#8B5CF6` |
| Reactions | Orange | `#F59E0B` |
| Reports | Red | `#EF4444` |

---

## ?? Documentation Files

| File | Mô t? |
|------|-------|
| `docs/admin_phase_2.md` | Audit Logs API specification |
| `docs/admin_phase_3.md` | Dashboard Charts API specification |
| `docs/admin_phase_4.md` | Bulk Actions API specification |
| `docs/admin_phase_5.md` | Export Data API specification |
| `docs/admin_api_complete.md` | Complete API summary |

---

## ?? Testing Checklist

### Backend Tests

- [ ] Unit tests for services
- [ ] Integration tests for controllers
- [ ] Authorization tests
- [ ] Bulk action limits (max 100)
- [ ] Export file generation
- [ ] Audit logging

### Frontend Tests

- [ ] Component unit tests
- [ ] API integration tests
- [ ] E2E tests for critical flows
- [ ] Accessibility tests

---

## ?? Deployment Checklist

### Backend

- [x] All controllers implemented
- [x] All services implemented
- [x] DTOs defined
- [x] Authorization configured
- [x] Build successful
- [ ] Unit tests passing
- [ ] Integration tests passing

### Frontend

- [ ] Project setup
- [ ] Auth flow implemented
- [ ] Dashboard page
- [ ] Users management page
- [ ] Posts management page
- [ ] Reports management page
- [ ] Audit logs page
- [ ] Export functionality
- [ ] Bulk actions UI
- [ ] Responsive design
- [ ] Error handling

---

## ?? Performance Considerations

| Feature | Limit | Notes |
|---------|-------|-------|
| Bulk actions | 100 items/request | Prevent timeout |
| Export data | 10,000 rows | Memory limit |
| Pagination | 100 items/page max | Performance |
| Chart data | 365 days max | Reasonable range |

---

## ?? Future Enhancements

1. **Real-time Updates**: WebSocket for live dashboard updates
2. **Advanced Filters**: Save filter presets
3. **Scheduled Reports**: Auto-generate reports
4. **Role-based Access**: Granular admin permissions
5. **Audit Log Retention**: Auto-archive old logs
6. **Dashboard Customization**: Drag-drop widgets

---

## ?? Support

- **Backend Issues**: Check `Favi-BE.API/` codebase
- **API Docs**: Scalar UI at `/scalar`
- **OpenAPI**: `/openapi/v1.json`

---

*Last Updated: 2024*
*Version: 1.0.0*
