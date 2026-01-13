# Admin Portal - Next.js App Router Architecture

## Tổng quan cấu trúc

```
admin-portal/
├── app/
│   ├── (auth)/
│   │   └── login/
│   │       └── page.tsx              # Login page
│   ├── (dashboard)/
│   │   ├── layout.tsx                # Admin layout (sidebar + header)
│   │   ├── page.tsx                  # Dashboard overview
│   │   ├── users/
│   │   │   ├── page.tsx              # Users list
│   │   │   └── [id]/
│   │   │       └── page.tsx          # User detail
│   │   ├── posts/
│   │   │   ├── page.tsx              # Posts list
│   │   │   └── [id]/
│   │   │       └── page.tsx          # Post detail
│   │   ├── reports/
│   │   │   ├── page.tsx              # Reports list
│   │   │   └── [id]/
│   │   │       └── page.tsx          # Report detail
│   │   ├── audit/
│   │   │   └── page.tsx              # Audit logs
│   │   └── analytics/
│   │       └── page.tsx              # Analytics charts
│   └── api/                          # API routes (nếu cần proxy)
├── components/
│   ├── ui/                           # shadcn/ui components
│   ├── layout/
│   │   ├── AdminSidebar.tsx
│   │   ├── AdminHeader.tsx
│   │   └── StatsCard.tsx
│   ├── charts/
│   │   ├── GrowthChart.tsx
│   │   ├── UserActivityChart.tsx
│   │   ├── UserRolePieChart.tsx
│   │   └── UserStatusPieChart.tsx
│   ├── tables/
│   │   ├── UsersTable.tsx
│   │   ├── PostsTable.tsx
│   │   ├── ReportsTable.tsx
│   │   └── AuditLogsTable.tsx
│   ├── modals/
│   │   ├── BanUserModal.tsx
│   │   ├── WarnUserModal.tsx
│   │   ├── DeleteContentModal.tsx
│   │   ├── ResolveReportModal.tsx
│   │   └── BulkActionModal.tsx
│   └── filters/
│       ├── DateRangePicker.tsx
│       ├── StatusFilter.tsx
│       └── SearchInput.tsx
├── lib/
│   ├── api/
│   │   ├── client.ts                 # Axios instance
│   │   ├── admin.ts                  # Admin API calls
│   │   ├── analytics.ts              # Analytics API
│   │   └── export.ts                 # Export API
│   └── utils/
│       ├── date.ts
│       ├── format.ts
│       └── constants.ts
├── hooks/
│   ├── useAuth.ts
│   ├── useDebounce.ts
│   ├── useExport.ts
│   └── queries/
│       ├── useUsers.ts
│       ├── usePosts.ts
│       ├── useReports.ts
│       ├── useAuditLogs.ts
│       └── useAnalytics.ts
└── types/
    └── admin.d.ts
```

---

## Chi tiết từng trang

### 1. Dashboard Overview - `/admin`

**Mục đích:** Tổng quan nhanh về hệ thống

**API sử dụng:**
| Endpoint | Method | Mục đích |
|----------|--------|----------|
| `/api/admin/analytics` | GET | Dashboard stats |
| `/api/admin/analytics/charts/growth` | GET | Growth chart data |
| `/api/admin/analytics/charts/user-status` | GET | User status pie chart |
| `/api/admin/analytics/top-users?limit=5` | GET | Top 5 users |
| `/api/admin/analytics/top-posts?limit=5` | GET | Top 5 posts |

**UI Components:**
```
┌─────────────────────────────────────────────────────────────┐
│  Header: "Dashboard" + Date Range Picker                    │
├──────────────┬──────────────┬──────────────┬──────────────┤
│  Stats Card  │  Stats Card  │  Stats Card  │  Stats Card  │
│  👥 Users    │  📝 Posts    │  ⚠️ Reports  │  🚫 Banned   │
│  1,234       │  5,678       │  12 pending  │  23          │
├──────────────┴──────────────┴──────────────┴──────────────┤
│  Growth Chart (Line)                                      │
│  ┌──────────────────────────────────────────────┐         │
│  │    📈 Users vs Posts over time               │         │
│  └──────────────────────────────────────────────┘         │
├──────────────────────────┬───────────────────────────────┤
│  User Status (Pie)       │  Top Users (List)             │
│  ┌──────────────┐        │  1. @john +200 reactions      │
│  │  Active 85%  │        │  2. @jane +150 reactions      │
│  │  Banned 5%   │        │  3. @bob +120 reactions       │
│  │  Inactive10% │        │                               │
│  └──────────────┘        └───────────────────────────────┘
├──────────────────────────┴───────────────────────────────┤
│  Top Posts (List)                                        │
│  1. "Amazing sunset..." ❤️ 500  💬 45                    │
│  2. "My new project..." ❤️ 320  💬 28                    │
└─────────────────────────────────────────────────────────────┘
```

**UX Features:**
- Auto-refresh mỗi 30 giây
- Date range picker với presets (Today, 7d, 30d, 90d)
- Hover tooltips cho chart data
- Animation khi load dữ liệu

---

### 2. Users Management - `/admin/users`

**Mục đích:** Quản lý người dùng, ban/unban, warn

**API sử dụng:**
| Endpoint | Method | Mục đích |
|----------|--------|----------|
| `/api/admin/analytics/users` | GET | Users list (paginated) |
| `/api/admin/analytics/charts/user-roles` | GET | User roles distribution |
| `/api/admin/users/{profileId}/ban` | POST | Ban user |
| `/api/admin/users/{profileId}/ban` | DELETE | Unban user |
| `/api/admin/users/{profileId}/warn` | POST | Warn user |
| `/api/admin/users/bulk/ban` | POST | Bulk ban |
| `/api/admin/users/bulk/unban` | POST | Bulk unban |
| `/api/admin/users/bulk/warn` | POST | Bulk warn |

**UI Components:**
```
┌─────────────────────────────────────────────────────────────┐
│  Header: "Users"                                            │
├─────────────────────────────────────────────────────────────┤
│  Filters:                                                   │
│  [Search 🔍] [Role ▼] [Status ▼] [Date Range 📅] [Reset]   │
├─────────────────────────────────────────────────────────────┤
│  ┌── Select All ─────────────────────────────────────────┐  │
│  │  [✓] Username     │  Email  │  Role │ Status │ Actions│  │
│  │  ────────────────┼─────────┼───────┼────────┼────────│  │
│  │  [✓] @john_doe   │ john@.. │ User  │ Active │ ⋮      │  │
│  │  [✓] @jane_smith │ jane@.. │ Admin │ Active │ ⋮      │  │
│  │  [ ] @bob_wilson │ bob@..  │ User  │ Banned │ ⋮      │  │
│  │  ...                                                    │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                             │
│  [✓] 3 selected  →  [Ban]  [Unban]  [Warn]  [Export 📥]    │
├─────────────────────────────────────────────────────────────┤
│  [◀ Prev] Page 1 of 50 [Next ▶]   Showing 1-20 of 1,234    │
└─────────────────────────────────────────────────────────────┘
```

**Actions Menu (⋮):**
- View Profile → chuyển đến `/admin/users/{id}`
- Ban User → mở BanUserModal
- Unban User → confirm dialog
- Warn User → mở WarnUserModal
- View Activity → show popover với stats

**UX Features:**
- Search debounced (300ms)
- Bulk selection với "Select All" checkbox
- Confirmation dialog cho destructive actions
- Toast notifications cho thành công/lỗi
- Keyboard shortcuts (Ctrl+F focus search, Esc close modal)

---

### 3. User Detail - `/admin/users/[id]`

**Mục đích:** Xem chi tiết user và lịch sử hoạt động

**API sử dụng:**
| Endpoint | Method | Mục đích |
|----------|--------|----------|
| `/api/profiles/{profileId}` | GET | User profile |
| `/api/admin/users/{profileId}/ban` | GET/DELETE | Ban status |
| `/api/admin/users/{profileId}/warn` | GET/POST | Warn history |
| `/api/admin/audit?targetProfileId={id}` | GET | User's audit logs |

**UI Components:**
```
┌─────────────────────────────────────────────────────────────┐
│  ← Back to Users                                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────┐  ┌─────────────────────────────────┐   │
│  │  Avatar         │  │  Username: @john_doe            │   │
│  │  (large)        │  │  Display: John Doe              │   │
│  │                 │  │  Email: john@example.com        │   │
│  │  [Ban] [Warn]   │  │  Role: User                     │   │
│  └─────────────────┘  │  Status: ● Active               │   │
│                       │  Created: Jan 1, 2024            │   │
│                       │  Last Active: 2 hours ago        │   │
│                       └─────────────────────────────────┘   │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│  Tabs: [Posts] [Comments] [Followers] [Moderation History]  │
├─────────────────────────────────────────────────────────────┤
│  Posts Tab:                                                │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  📝 "My first post..."                               │   │
│  │  ❤️ 120  💬 15  📅 Jan 5, 2024  [View] [Delete]      │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Moderation History Tab:                                   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  🚫 Banned  │  Reason: Spam  │  Jan 10, 2024       │   │
│  │  ⚠️ Warned  │  Reason: Harassment │ Jan 8, 2024    │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

**UX Features:**
- Quick actions với button colors (Red cho ban, Yellow cho warn)
- Tabs navigation với lazy loading
- Inline delete cho posts
- Copy to clipboard cho username/email

---

### 4. Posts Management - `/admin/posts`

**Mục đích:** Quản lý và xóa nội dung

**API sử dụng:**
| Endpoint | Method | Mục đích |
|----------|--------|----------|
| `/api/admin/analytics/posts` | GET | Posts list (paginated) |
| `/api/admin/content/posts/{id}` | DELETE | Delete post |
| `/api/admin/content/posts/bulk/delete` | POST | Bulk delete posts |
| `/api/admin/analytics/charts/content-activity` | GET | Content activity chart |

**UI Components:**
```
┌─────────────────────────────────────────────────────────────┐
│  Header: "Posts"                                            │
├─────────────────────────────────────────────────────────────┤
│  Filters:                                                   │
│  [Search 🔍] [Privacy ▼] [Date Range 📅] [Status ▼]        │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────┐│
│  │  [✓]  Content    │  Author  │  Privacy │ Stats │ Actions││
│  │  ────────────────┼──────────┼──────────┼───────┼────────││
│  │  [✓] "Sunset..." │ @john    │ Public   │ ❤️ 500│ ⋮      ││
│  │  [✓] "My project"│ @jane    │ Private  │ ❤️ 50 │ ⋮      ││
│  └─────────────────────────────────────────────────────────┘│
│                                                             │
│  [✓] 2 selected  →  [Delete 🗑️]  [Export 📥]               │
├─────────────────────────────────────────────────────────────┤
│  [◀ Prev] Page 1 of 100 [Next ▶]                            │
└─────────────────────────────────────────────────────────────┘
```

**Preview Modal:**
Khi click vào content, show preview modal:
- Image/Video preview nếu có
- Full caption
- Author info
- Engagement stats
- Created date

**UX Features:**
- Content preview trong modal thay vì expand row
- Lazy load images
- Skeleton loading
- Undo delete (5 giây)

---

### 5. Reports Management - `/admin/reports`

**Mục đích:** Xử lý báo cáo vi phạm

**API sử dụng:**
| Endpoint | Method | Mục đích |
|----------|--------|----------|
| `/api/admin/reports` | GET | Reports list |
| `/api/admin/reports/status/{status}` | GET | Filter by status |
| `/api/admin/reports/{id}/resolve` | POST | Resolve report |
| `/api/admin/reports/{id}/reject` | POST | Reject report |
| `/api/admin/reports/{id}/status` | PUT | Update status |
| `/api/admin/reports/bulk/resolve` | POST | Bulk resolve |
| `/api/admin/reports/bulk/reject` | POST | Bulk reject |
| `/api/admin/analytics/charts/report-status` | GET | Report status chart |

**UI Components:**
```
┌─────────────────────────────────────────────────────────────┐
│  Header: "Reports"                                          │
├─────────────────────────────────────────────────────────────┤
│  Stats:  ⚠️ 12 Pending  ✅ 45 Resolved  ❌ 5 Rejected       │
├─────────────────────────────────────────────────────────────┤
│  Filters:                                                   │
│  [Search 🔍] [Status ▼] [Target Type ▼] [Date Range 📅]    │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────┐│
│  │  [✓] Target    │  Reporter  │  Reason │ Status │ Action││
│  │  ──────────────┼────────────┼─────────┼────────┼────────││
│  │  [✓] Post:...  │ @jane      │ Spam    │ Pending│ ⋮     ││
│  │  [✓] User:...  │ @bob       │ Harassment │ Pending│ ⋮  ││
│  └─────────────────────────────────────────────────────────┘│
│                                                             │
│  [✓] 2 selected  →  [Resolve ✅]  [Reject ❌]               │
├─────────────────────────────────────────────────────────────┤
│  Pagination                                                 │
└─────────────────────────────────────────────────────────────┘
```

**Report Detail Modal:**
```
┌─────────────────────────────────────────────────────────────┐
│  Report #12345                                              │
├─────────────────────────────────────────────────────────────┤
│  Target: Post "Spam content..."                             │
│  Reported by: @jane  •  Jan 10, 2024 10:30 AM               │
│  Reason: Spam                                               │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  [Post Preview với nội dung bị report]              │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                             │
│  Actions:                                                   │
│  [Resolve with Delete Content ✅]  [Resolve Only 📝]        │
│  [Reject Report ❌]                                         │
│                                                             │
│  Notes: [Textarea cho ghi chú admin...]                     │
└─────────────────────────────────────────────────────────────┘
```

**UX Features:**
- Badge colors: Pending (Yellow), Resolved (Green), Rejected (Red)
- Priority sorting (Pending trước)
- Bulk actions với checkboxes
- Keyboard: `R` = Resolve, `X` = Reject

---

### 6. Report Detail - `/admin/reports/[id]`

**Mục đích:** Xử lý chi tiết một report

**API sử dụng:**
| Endpoint | Method | Mục đích |
|----------|--------|----------|
| `/api/admin/reports/{id}` | GET | Report details |
| `/api/admin/reports/{id}/resolve` | POST | Resolve |
| `/api/admin/reports/{id}/reject` | POST | Reject |

**UI Components:**
Full page với:
- Report info panel
- Target preview
- Reporter info
- Action buttons
- History timeline

---

### 7. Audit Logs - `/admin/audit`

**Mục đích:** Xem lịch sử hành động admin

**API sử dụng:**
| Endpoint | Method | Mục đích |
|----------|--------|----------|
| `/api/admin/audit` | GET | Audit logs (paginated) |
| `/api/admin/audit/{id}` | GET | Single audit log |
| `/api/admin/audit/action-types` | GET | Action types filter |
| `/api/admin/audit/summary` | GET | Action summary |
| `/api/admin/export/audit-logs` | GET | Export logs |

**UI Components:**
```
┌─────────────────────────────────────────────────────────────┐
│  Header: "Audit Logs"                                       │
├─────────────────────────────────────────────────────────────┤
│  Filters:                                                   │
│  [Search 🔍] [Action Type ▼] [Admin ▼] [Date Range 📅]     │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────┐│
│  │  Timestamp      │  Admin  │  Action   │  Target │ Details││
│  │  ───────────────┼─────────┼───────────┼─────────┼────────││
│  │  Jan 10, 14:30  │ @admin1 │ Ban User  │ @john   │ Spam   ││
│  │  Jan 10, 13:15  │ @admin1 │ Resolve   │ Report# │ ...    ││
│  │  Jan 10, 12:00  │ @admin2 │ Delete    │ Post    │ Spam   ││
│  │  Jan 10, 11:45  │ @admin2 │ Warn User │ @jane   │ ...    ││
│  └─────────────────────────────────────────────────────────┘│
│                                                             │
│  [Export CSV] [Export JSON]                                 │
├─────────────────────────────────────────────────────────────┤
│  Pagination                                                 │
└─────────────────────────────────────────────────────────────┘
```

**Action Type Colors:**
- Ban User: 🔴 Red
- Unban User: 🟢 Green
- Warn User: 🟡 Yellow
- Delete Content: 🔴 Red
- Resolve Report: 🟢 Green
- Reject Report: 🟠 Orange
- Export Data: 🔵 Blue

**UX Features:**
- Click vào admin name để filter
- Click vào target để xem chi tiết
- Relative time (5 minutes ago)
- Export dropdown

---

### 8. Analytics - `/admin/analytics`

**Mục đích:** Charts và visualizations chi tiết

**API sử dụng:**
| Endpoint | Method | Mục đích |
|----------|--------|----------|
| `/api/admin/analytics/charts/growth` | GET | Growth data |
| `/api/admin/analytics/charts/user-activity` | GET | User activity |
| `/api/admin/analytics/charts/content-activity` | GET | Content activity |
| `/api/admin/analytics/charts/user-roles` | GET | Roles pie |
| `/api/admin/analytics/charts/user-status` | GET | Status pie |
| `/api/admin/analytics/charts/post-privacy` | GET | Privacy pie |
| `/api/admin/analytics/charts/report-status` | GET | Reports pie |
| `/api/admin/analytics/comparison` | GET | Period comparison |

**UI Components:**
```
┌─────────────────────────────────────────────────────────────┐
│  Header: "Analytics" + Date Range Picker                    │
├─────────────────────────────────────────────────────────────┤
│  [Growth] [User Activity] [Content Activity] [Distributions]│
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Row 1:                                                     │
│  ┌─────────────────────┐  ┌─────────────────────┐          │
│  │  Growth Chart       │  │  User Activity      │          │
│  │  (Line Chart)       │  │  (Area Chart)       │          │
│  └─────────────────────┘  └─────────────────────┘          │
│                                                             │
│  Row 2:                                                     │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐            │
│  │  User      │  │  User      │  │  Post      │            │
│  │  Roles     │  │  Status    │  │  Privacy   │            │
│  │  (Pie)     │  │  (Pie)     │  │  (Pie)     │            │
│  └────────────┘  └────────────┘  └────────────┘            │
│                                                             │
│  Row 3:                                                     │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  Period Comparison: This Week vs Last Week          │    │
│  │  📈 Users: +12%  📈 Posts: +8%  📉 Reports: -5%     │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

**UX Features:**
- Chart legends tương tác (click để ẩn/hiện series)
- Tooltip chi tiết khi hover
- Export chart as PNG
- Responsive grid layout

---

## Layout Components

### AdminSidebar

```tsx
<nav>
  <Logo />
  <NavItem href="/admin" icon="dashboard" />
  <NavItem href="/admin/users" icon="users" />
  <NavItem href="/admin/posts" icon="posts" />
  <NavItem href="/admin/reports" icon="reports" badge={12} />
  <NavItem href="/admin/audit" icon="audit" />
  <NavItem href="/admin/analytics" icon="analytics" />
</nav>
```

### AdminHeader

```tsx
<header>
  <Breadcrumb />
  <Search global />
  <Notifications dropdown>
    <NotificationItem type="report" count={3} />
  </Notifications>
  <UserMenu avatar />
</header>
```

---

## API Integration Layer

### admin.ts

```typescript
// Users
export const getUsers = (params: UserFilter) =>
  client.get<PagedResult<UserDto>>('/admin/analytics/users', { params });

export const banUser = (profileId: string, reason: BanRequest) =>
  client.post(`/admin/users/${profileId}/ban`, reason);

export const unbanUser = (profileId: string) =>
  client.delete(`/admin/users/${profileId}/ban`);

export const warnUser = (profileId: string, reason: WarnRequest) =>
  client.post(`/admin/users/${profileId}/warn`, reason);

export const bulkBan = (request: BulkBanRequest) =>
  client.post('/admin/users/bulk/ban', request);

// Posts
export const deletePost = (postId: string, reason?: string) =>
  client.delete(`/admin/content/posts/${postId}`, { data: { reason } });

export const bulkDeletePosts = (request: BulkDeleteRequest) =>
  client.post('/admin/content/posts/bulk/delete', request);

// Reports
export const getReports = (params: ReportFilter) =>
  client.get<PagedResult<ReportDto>>('/admin/reports', { params });

export const resolveReport = (id: string, data: ResolveRequest) =>
  client.post(`/admin/reports/${id}/resolve`, data);

export const rejectReport = (id: string, reason?: string) =>
  client.post(`/admin/reports/${id}/reject`, { reason });

export const bulkResolve = (request: BulkResolveRequest) =>
  client.post('/admin/reports/bulk/resolve', request);

// Audit
export const getAuditLogs = (params: AuditFilter) =>
  client.get<PagedResult<AuditLogDto>>('/admin/audit', { params });

export const exportAuditLogs = (params: ExportRequest) =>
  client.get('/admin/export/audit-logs', { params, responseType: 'blob' });
```

---

## React Query Keys

```typescript
export const queryKeys = {
  // Dashboard
  dashboardStats: ['admin', 'dashboard', 'stats'],
  dashboardCharts: (params) => ['admin', 'dashboard', 'charts', params],

  // Users
  users: (filters) => ['admin', 'users', filters],
  user: (id) => ['admin', 'users', id],
  userBanStatus: (id) => ['admin', 'users', id, 'ban'],
  userWarns: (id) => ['admin', 'users', id, 'warns'],

  // Posts
  posts: (filters) => ['admin', 'posts', filters],
  post: (id) => ['admin', 'posts', id],

  // Reports
  reports: (filters) => ['admin', 'reports', filters],
  report: (id) => ['admin', 'reports', id],

  // Audit
  auditLogs: (filters) => ['admin', 'audit', filters],

  // Analytics
  analytics: (type, params) => ['admin', 'analytics', type, params],
  growthChart: (params) => ['admin', 'charts', 'growth', params],
  activityChart: (params) => ['admin', 'charts', 'activity', params],
  topUsers: (limit) => ['admin', 'top-users', limit],
  topPosts: (limit) => ['admin', 'top-posts', limit],
};
```

---

## UX Best Practices Applied

### 1. Loading States
- Skeleton loaders cho tables
- Shimmer effects cho charts
- Button loading state

### 2. Error Handling
- Toast notifications (top-right)
- Inline error messages
- Retry buttons
- Empty states với illustrations

### 3. Feedback
- Success toasts cho mọi action
- Confirmation dialogs cho destructive actions
- Progress indicators cho bulk operations
- Undo capability (5 giây) cho delete

### 4. Keyboard Shortcuts
| Key | Action |
|-----|--------|
| `Ctrl/Cmd + K` | Global search |
| `Ctrl/Cmd + F` | Focus table search |
| `Esc` | Close modal |
| `R` | Resolve selected report |
| `X` | Reject selected report |

### 5. Accessibility
- ARIA labels
- Keyboard navigation
- Focus management
- Color contrast (WCAG AA)

### 6. Performance
- Virtual scrolling cho large tables
- Debounced search (300ms)
- Lazy loading cho modals
- Code splitting theo routes
