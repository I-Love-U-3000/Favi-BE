# ?? Phase 3: Dashboard Charts API - Frontend Specification

## Overview

Phase 3 cung c?p các API endpoints ?? frontend hi?n th? bi?u ?? và th?ng kê tr?c quan cho Admin Dashboard.

**M?c ?ích**: Cho phép Admin xem:
- Bi?u ?? t?ng tr??ng theo th?i gian (users, posts, reports)
- Bi?u ?? ho?t ??ng (new users, active users, content)
- Phân b? d? li?u (roles, status, privacy)
- Top users và top posts
- So sánh gi?a các kho?ng th?i gian

---

## 1. API Endpoints Summary

### Time Series Charts
| Endpoint | Method | Auth | Mô t? |
|----------|--------|------|-------|
| `/api/admin/analytics/charts/growth` | GET | ? Admin | Growth chart (users, posts, reports) |
| `/api/admin/analytics/charts/user-activity` | GET | ? Admin | User activity over time |
| `/api/admin/analytics/charts/content-activity` | GET | ? Admin | Content activity over time |

### Distribution Charts
| Endpoint | Method | Auth | Mô t? |
|----------|--------|------|-------|
| `/api/admin/analytics/charts/user-roles` | GET | ? Admin | User role distribution |
| `/api/admin/analytics/charts/user-status` | GET | ? Admin | User status distribution |
| `/api/admin/analytics/charts/post-privacy` | GET | ? Admin | Post privacy distribution |
| `/api/admin/analytics/charts/report-status` | GET | ? Admin | Report status distribution |

### Top Entities & Comparison
| Endpoint | Method | Auth | Mô t? |
|----------|--------|------|-------|
| `/api/admin/analytics/top-users` | GET | ? Admin | Top users by engagement |
| `/api/admin/analytics/top-posts` | GET | ? Admin | Top posts by engagement |
| `/api/admin/analytics/comparison` | GET | ? Admin | Period comparison |

---

## 2. Time Series Charts

### 2.1 `GET /api/admin/analytics/charts/growth`

**Mô t?**: L?y d? li?u bi?u ?? t?ng tr??ng (users, posts, reports theo th?i gian)

**Query Parameters**:

| Parameter | Type | Required | Default | Mô t? |
|-----------|------|----------|---------|-------|
| `fromDate` | date | ? | 30 days ago | Ngày b?t ??u |
| `toDate` | date | ? | today | Ngày k?t thúc |
| `interval` | string | ? | "day" | Kho?ng nhóm: `day`, `week`, `month` |

**Request Example**:
```
GET /api/admin/analytics/charts/growth?fromDate=2024-01-01&toDate=2024-01-31&interval=day
```

**Response**: `200 OK`
```json
{
  "users": [
    { "date": "2024-01-01T00:00:00Z", "count": 5 },
    { "date": "2024-01-02T00:00:00Z", "count": 8 },
    { "date": "2024-01-03T00:00:00Z", "count": 3 }
  ],
  "posts": [
    { "date": "2024-01-01T00:00:00Z", "count": 12 },
    { "date": "2024-01-02T00:00:00Z", "count": 15 },
    { "date": "2024-01-03T00:00:00Z", "count": 10 }
  ],
  "reports": [
    { "date": "2024-01-01T00:00:00Z", "count": 2 },
    { "date": "2024-01-02T00:00:00Z", "count": 1 },
    { "date": "2024-01-03T00:00:00Z", "count": 3 }
  ],
  "fromDate": "2024-01-01T00:00:00Z",
  "toDate": "2024-01-31T23:59:59Z",
  "interval": "day"
}
```

---

### 2.2 `GET /api/admin/analytics/charts/user-activity`

**Mô t?**: L?y d? li?u ho?t ??ng ng??i dùng theo th?i gian

**Query Parameters**: Gi?ng nh? growth chart

**Response**: `200 OK`
```json
{
  "newUsers": [
    { "date": "2024-01-01T00:00:00Z", "count": 5 },
    { "date": "2024-01-02T00:00:00Z", "count": 8 }
  ],
  "activeUsers": [
    { "date": "2024-01-01T00:00:00Z", "count": 120 },
    { "date": "2024-01-02T00:00:00Z", "count": 135 }
  ],
  "bannedUsers": [
    { "date": "2024-01-01T00:00:00Z", "count": 10 },
    { "date": "2024-01-02T00:00:00Z", "count": 10 }
  ],
  "fromDate": "2024-01-01T00:00:00Z",
  "toDate": "2024-01-31T23:59:59Z",
  "interval": "day"
}
```

---

### 2.3 `GET /api/admin/analytics/charts/content-activity`

**Mô t?**: L?y d? li?u ho?t ??ng n?i dung theo th?i gian

**Response**: `200 OK`
```json
{
  "posts": [
    { "date": "2024-01-01T00:00:00Z", "count": 25 },
    { "date": "2024-01-02T00:00:00Z", "count": 30 }
  ],
  "comments": [
    { "date": "2024-01-01T00:00:00Z", "count": 150 },
    { "date": "2024-01-02T00:00:00Z", "count": 180 }
  ],
  "reactions": [
    { "date": "2024-01-01T00:00:00Z", "count": 500 },
    { "date": "2024-01-02T00:00:00Z", "count": 620 }
  ],
  "fromDate": "2024-01-01T00:00:00Z",
  "toDate": "2024-01-31T23:59:59Z",
  "interval": "day"
}
```

---

## 3. Distribution Charts

### 3.1 `GET /api/admin/analytics/charts/user-roles`

**Mô t?**: Phân b? users theo role (cho pie/donut chart)

**Response**: `200 OK`
```json
{
  "roles": [
    { "label": "User", "count": 9500, "percentage": 95.0 },
    { "label": "Admin", "count": 500, "percentage": 5.0 }
  ],
  "totalUsers": 10000
}
```

---

### 3.2 `GET /api/admin/analytics/charts/user-status`

**Mô t?**: Phân b? users theo status

**Response**: `200 OK`
```json
{
  "activeUsers": 8500,
  "bannedUsers": 200,
  "inactiveUsers": 1300,
  "totalUsers": 10000
}
```

**??nh ngh?a**:
- `activeUsers`: Users không b? ban và có ho?t ??ng trong 30 ngày qua
- `bannedUsers`: Users ?ang b? ban
- `inactiveUsers`: Users không b? ban nh?ng không ho?t ??ng > 30 ngày

---

### 3.3 `GET /api/admin/analytics/charts/post-privacy`

**Mô t?**: Phân b? posts theo privacy level

**Response**: `200 OK`
```json
{
  "privacyLevels": [
    { "label": "Public", "count": 8000, "percentage": 80.0 },
    { "label": "Followers", "count": 1500, "percentage": 15.0 },
    { "label": "Private", "count": 500, "percentage": 5.0 }
  ],
  "totalPosts": 10000
}
```

---

### 3.4 `GET /api/admin/analytics/charts/report-status`

**Mô t?**: Phân b? reports theo status

**Response**: `200 OK`
```json
{
  "pending": 45,
  "resolved": 230,
  "rejected": 25,
  "totalReports": 300
}
```

---

## 4. Top Entities

### 4.1 `GET /api/admin/analytics/top-users`

**Mô t?**: Top users theo engagement (followers, reactions)

**Query Parameters**:

| Parameter | Type | Default | Max | Mô t? |
|-----------|------|---------|-----|-------|
| `limit` | int | 10 | 50 | S? l??ng users tr? v? |

**Response**: `200 OK`
```json
[
  {
    "id": "user-guid-1",
    "username": "popular_user",
    "displayName": "Popular User",
    "avatarUrl": "https://...",
    "postsCount": 150,
    "followersCount": 5000,
    "reactionsReceived": 25000
  },
  {
    "id": "user-guid-2",
    "username": "influencer",
    "displayName": "Top Influencer",
    "avatarUrl": "https://...",
    "postsCount": 80,
    "followersCount": 3500,
    "reactionsReceived": 18000
  }
]
```

---

### 4.2 `GET /api/admin/analytics/top-posts`

**Mô t?**: Top posts theo engagement (reactions, comments)

**Query Parameters**:

| Parameter | Type | Default | Max | Mô t? |
|-----------|------|---------|-----|-------|
| `limit` | int | 10 | 50 | S? l??ng posts tr? v? |

**Response**: `200 OK`
```json
[
  {
    "id": "post-guid-1",
    "authorId": "user-guid-1",
    "authorUsername": "popular_user",
    "caption": "This is a viral post...",
    "createdAt": "2024-01-15T10:30:00Z",
    "reactionsCount": 1500,
    "commentsCount": 320
  },
  {
    "id": "post-guid-2",
    "authorId": "user-guid-2",
    "authorUsername": "influencer",
    "caption": "Another popular post...",
    "createdAt": "2024-01-14T15:45:00Z",
    "reactionsCount": 1200,
    "commentsCount": 250
  }
]
```

---

## 5. Period Comparison

### 5.1 `GET /api/admin/analytics/comparison`

**Mô t?**: So sánh s? li?u gi?a period hi?n t?i và period tr??c ?ó

**Query Parameters**:

| Parameter | Type | Default | Mô t? |
|-----------|------|---------|-------|
| `fromDate` | date | 30 days ago | B?t ??u period hi?n t?i |
| `toDate` | date | today | K?t thúc period hi?n t?i |

**Logic**: Period tr??c ?ó ???c tính t? ??ng b?ng cách l?y kho?ng th?i gian t??ng ???ng tr??c `fromDate`

**Response**: `200 OK`
```json
{
  "currentPeriod": {
    "newUsers": 150,
    "newPosts": 500,
    "newComments": 2500,
    "newReactions": 15000,
    "newReports": 25,
    "fromDate": "2024-01-01T00:00:00Z",
    "toDate": "2024-01-31T23:59:59Z"
  },
  "previousPeriod": {
    "newUsers": 120,
    "newPosts": 450,
    "newComments": 2200,
    "newReactions": 12000,
    "newReports": 30,
    "fromDate": "2023-12-01T00:00:00Z",
    "toDate": "2023-12-31T23:59:59Z"
  },
  "growth": {
    "usersGrowthPercent": 25.0,
    "postsGrowthPercent": 11.11,
    "commentsGrowthPercent": 13.64,
    "reactionsGrowthPercent": 25.0,
    "reportsGrowthPercent": -16.67
  }
}
```

**Growth Calculation**:
- Positive = t?ng so v?i period tr??c
- Negative = gi?m so v?i period tr??c
- Formula: `((current - previous) / previous) * 100`

---

## 6. TypeScript Interfaces

```typescript
// ===== Time Series Types =====

interface TimeSeriesDataPoint {
  date: string; // ISO 8601 format
  count: number;
}

interface LabeledDataPoint {
  label: string;
  count: number;
  percentage?: number;
}

type ChartInterval = 'day' | 'week' | 'month';

// ===== Chart Response Types =====

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

// ===== Distribution Types =====

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

// ===== Top Entity Types =====

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

// ===== Comparison Types =====

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
```

---

## 7. API Service Implementation

```typescript
// lib/api/analytics.ts

import { apiClient } from './client';

interface ChartQueryParams {
  fromDate?: string;
  toDate?: string;
  interval?: ChartInterval;
}

export const analyticsApi = {
  // Dashboard stats
  getDashboardStats: () => 
    apiClient.get<DashboardStatsResponse>('/api/admin/analytics'),

  // Time series charts
  getGrowthChart: (params?: ChartQueryParams) =>
    apiClient.get<GrowthChartResponse>('/api/admin/analytics/charts/growth', { params }),

  getUserActivityChart: (params?: ChartQueryParams) =>
    apiClient.get<UserActivityChartResponse>('/api/admin/analytics/charts/user-activity', { params }),

  getContentActivityChart: (params?: ChartQueryParams) =>
    apiClient.get<ContentActivityChartResponse>('/api/admin/analytics/charts/content-activity', { params }),

  // Distribution charts
  getUserRoleDistribution: () =>
    apiClient.get<UserRoleDistributionResponse>('/api/admin/analytics/charts/user-roles'),

  getUserStatusDistribution: () =>
    apiClient.get<UserStatusDistributionResponse>('/api/admin/analytics/charts/user-status'),

  getPostPrivacyDistribution: () =>
    apiClient.get<PostPrivacyDistributionResponse>('/api/admin/analytics/charts/post-privacy'),

  getReportStatusDistribution: () =>
    apiClient.get<ReportStatusDistributionResponse>('/api/admin/analytics/charts/report-status'),

  // Top entities
  getTopUsers: (limit = 10) =>
    apiClient.get<TopUserDto[]>('/api/admin/analytics/top-users', { params: { limit } }),

  getTopPosts: (limit = 10) =>
    apiClient.get<TopPostDto[]>('/api/admin/analytics/top-posts', { params: { limit } }),

  // Period comparison
  getPeriodComparison: (fromDate?: string, toDate?: string) =>
    apiClient.get<PeriodComparisonResponse>('/api/admin/analytics/comparison', {
      params: { fromDate, toDate }
    }),
};
```

---

## 8. React Hooks Implementation

```typescript
// hooks/useAnalytics.ts

import { useQuery } from '@tanstack/react-query';
import { analyticsApi } from '@/lib/api/analytics';

const QUERY_KEYS = {
  dashboard: ['admin', 'analytics', 'dashboard'],
  growthChart: (params: ChartQueryParams) => ['admin', 'analytics', 'charts', 'growth', params],
  userActivity: (params: ChartQueryParams) => ['admin', 'analytics', 'charts', 'user-activity', params],
  contentActivity: (params: ChartQueryParams) => ['admin', 'analytics', 'charts', 'content-activity', params],
  userRoles: ['admin', 'analytics', 'charts', 'user-roles'],
  userStatus: ['admin', 'analytics', 'charts', 'user-status'],
  postPrivacy: ['admin', 'analytics', 'charts', 'post-privacy'],
  reportStatus: ['admin', 'analytics', 'charts', 'report-status'],
  topUsers: (limit: number) => ['admin', 'analytics', 'top-users', limit],
  topPosts: (limit: number) => ['admin', 'analytics', 'top-posts', limit],
  comparison: (fromDate?: string, toDate?: string) => ['admin', 'analytics', 'comparison', fromDate, toDate],
} as const;

// Dashboard stats
export function useDashboardStats() {
  return useQuery({
    queryKey: QUERY_KEYS.dashboard,
    queryFn: analyticsApi.getDashboardStats,
    staleTime: 60000, // 1 minute
  });
}

// Time series charts
export function useGrowthChart(params: ChartQueryParams = {}) {
  return useQuery({
    queryKey: QUERY_KEYS.growthChart(params),
    queryFn: () => analyticsApi.getGrowthChart(params),
    staleTime: 300000, // 5 minutes
  });
}

export function useUserActivityChart(params: ChartQueryParams = {}) {
  return useQuery({
    queryKey: QUERY_KEYS.userActivity(params),
    queryFn: () => analyticsApi.getUserActivityChart(params),
    staleTime: 300000,
  });
}

export function useContentActivityChart(params: ChartQueryParams = {}) {
  return useQuery({
    queryKey: QUERY_KEYS.contentActivity(params),
    queryFn: () => analyticsApi.getContentActivityChart(params),
    staleTime: 300000,
  });
}

// Distribution charts
export function useUserRoleDistribution() {
  return useQuery({
    queryKey: QUERY_KEYS.userRoles,
    queryFn: analyticsApi.getUserRoleDistribution,
    staleTime: 600000, // 10 minutes
  });
}

export function useUserStatusDistribution() {
  return useQuery({
    queryKey: QUERY_KEYS.userStatus,
    queryFn: analyticsApi.getUserStatusDistribution,
    staleTime: 60000,
  });
}

// Top entities
export function useTopUsers(limit = 10) {
  return useQuery({
    queryKey: QUERY_KEYS.topUsers(limit),
    queryFn: () => analyticsApi.getTopUsers(limit),
    staleTime: 300000,
  });
}

export function useTopPosts(limit = 10) {
  return useQuery({
    queryKey: QUERY_KEYS.topPosts(limit),
    queryFn: () => analyticsApi.getTopPosts(limit),
    staleTime: 300000,
  });
}

// Period comparison
export function usePeriodComparison(fromDate?: string, toDate?: string) {
  return useQuery({
    queryKey: QUERY_KEYS.comparison(fromDate, toDate),
    queryFn: () => analyticsApi.getPeriodComparison(fromDate, toDate),
    staleTime: 300000,
  });
}
```

---

## 9. UI Components Specification

### 9.1 Dashboard Layout

```
???????????????????????????????????????????????????????????????????????????????
?                           Admin Dashboard                                    ?
???????????????????????????????????????????????????????????????????????????????
?                                                                              ?
?  ??????????????? ??????????????? ??????????????? ???????????????            ?
?  ? Total Users ? ? Total Posts ? ? Active Users? ?  Pending    ?            ?
?  ?   10,000    ? ?   25,000    ? ?    8,500    ? ?  Reports    ?            ?
?  ?   ? 25%     ? ?   ? 11%     ? ?   ? 15%     ? ?     45      ?            ?
?  ??????????????? ??????????????? ??????????????? ???????????????            ?
?                                                                              ?
?  ?????????????????????????????????????? ??????????????????????????????????  ?
?  ?       Growth Chart (Line)          ? ?    User Status (Pie)           ?  ?
?  ?  ?? Users  Posts  Reports          ? ?   ?? Active: 85%               ?  ?
?  ?                                     ? ?   ?? Banned: 2%                ?  ?
?  ?    ??    ??                        ? ?   ? Inactive: 13%             ?  ?
?  ?   ?  ?  ?  ?                       ? ?                                ?  ?
?  ?  ?    ??    ?                      ? ?                                ?  ?
?  ?????????????????????????????????????? ??????????????????????????????????  ?
?                                                                              ?
?  ?????????????????????????????????????? ??????????????????????????????????  ?
?  ?      Content Activity (Bar)        ? ?      Top Users                 ?  ?
?  ?  Posts  Comments  Reactions        ? ?   1. @popular_user  5K ??      ?  ?
?  ?   ????    ????????    ???????????? ? ?   2. @influencer    3.5K ??    ?  ?
?  ?                                     ? ?   3. @creator       2.8K ??    ?  ?
?  ?????????????????????????????????????? ??????????????????????????????????  ?
?                                                                              ?
???????????????????????????????????????????????????????????????????????????????
```

### 9.2 Component Structure

```
admin/
??? dashboard/
?   ??? DashboardPage.tsx           # Main dashboard page
?   ??? StatsCards.tsx              # Summary stat cards
?   ??? GrowthChart.tsx             # Line chart for growth
?   ??? UserStatusPieChart.tsx      # Pie chart for user status
?   ??? ContentActivityChart.tsx    # Bar chart for content
?   ??? TopUsersList.tsx            # Top users list
?   ??? TopPostsList.tsx            # Top posts list
?   ??? PeriodComparisonCard.tsx    # Period comparison
?   ??? DateRangeSelector.tsx       # Date range picker
?   ??? IntervalSelector.tsx        # Day/Week/Month selector
```

---

## 10. Chart Library Recommendations

| Library | Best For | Notes |
|---------|----------|-------|
| **Recharts** | General purpose | Easy to use, good React integration |
| **Chart.js + react-chartjs-2** | Standard charts | Popular, well-documented |
| **Nivo** | Beautiful charts | Declarative, animations |
| **Tremor** | Dashboard UI | Tailwind-based, ready-to-use components |

### Example with Recharts:

```tsx
import { LineChart, Line, XAxis, YAxis, Tooltip, Legend, ResponsiveContainer } from 'recharts';

function GrowthChart({ data }: { data: GrowthChartResponse }) {
  // Transform data for Recharts
  const chartData = data.users.map((point, index) => ({
    date: new Date(point.date).toLocaleDateString(),
    users: point.count,
    posts: data.posts[index]?.count ?? 0,
    reports: data.reports[index]?.count ?? 0,
  }));

  return (
    <ResponsiveContainer width="100%" height={300}>
      <LineChart data={chartData}>
        <XAxis dataKey="date" />
        <YAxis />
        <Tooltip />
        <Legend />
        <Line type="monotone" dataKey="users" stroke="#3B82F6" name="Users" />
        <Line type="monotone" dataKey="posts" stroke="#22C55E" name="Posts" />
        <Line type="monotone" dataKey="reports" stroke="#EF4444" name="Reports" />
      </LineChart>
    </ResponsiveContainer>
  );
}
```

---

## 11. Color Palette

### Status Colors
| Status | Color | Hex |
|--------|-------|-----|
| Success/Active | Green | `#22C55E` |
| Warning | Yellow | `#F59E0B` |
| Error/Banned | Red | `#EF4444` |
| Info | Blue | `#3B82F6` |
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

## 12. Caching & Performance

| Data Type | Stale Time | Refetch Interval | Notes |
|-----------|------------|------------------|-------|
| Dashboard Stats | 1 minute | 1 minute | Changes frequently |
| Time Series Charts | 5 minutes | - | Manual refresh |
| Distribution Charts | 10 minutes | - | Changes slowly |
| Top Entities | 5 minutes | - | Manual refresh |

---

## 13. Date Handling

### Frontend Date Formatting

```typescript
// utils/date.ts

export function formatChartDate(dateString: string, interval: ChartInterval): string {
  const date = new Date(dateString);
  
  switch (interval) {
    case 'month':
      return date.toLocaleDateString('vi-VN', { month: 'short', year: 'numeric' });
    case 'week':
      return `W${getWeekNumber(date)}`;
    default:
      return date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' });
  }
}

export function getWeekNumber(date: Date): number {
  const firstDayOfYear = new Date(date.getFullYear(), 0, 1);
  const pastDaysOfYear = (date.getTime() - firstDayOfYear.getTime()) / 86400000;
  return Math.ceil((pastDaysOfYear + firstDayOfYear.getDay() + 1) / 7);
}
```

---

## 14. Testing Checklist

### Unit Tests
- [ ] Chart data transformation functions
- [ ] Date formatting utilities
- [ ] Growth percentage calculations

### Integration Tests
- [ ] API calls return expected data structure
- [ ] Date range filters work correctly
- [ ] Interval grouping works correctly

### Visual Tests
- [ ] Charts render correctly with sample data
- [ ] Empty states display properly
- [ ] Responsive behavior on different screen sizes

---

## 15. Accessibility (A11y)

1. **Chart alternatives**: Provide data tables as alternative to charts
2. **Color blindness**: Use patterns in addition to colors
3. **Screen readers**: Add descriptive labels for chart data
4. **Keyboard navigation**: Ensure chart tooltips are keyboard accessible

---

## 16. Localization Notes

| Key | English | Vietnamese |
|-----|---------|------------|
| `chart.growth` | Growth | T?ng tr??ng |
| `chart.users` | Users | Ng??i dùng |
| `chart.posts` | Posts | Bài vi?t |
| `chart.comments` | Comments | Bình lu?n |
| `chart.reactions` | Reactions | L??t thích |
| `chart.reports` | Reports | Báo cáo |
| `interval.day` | Daily | Theo ngày |
| `interval.week` | Weekly | Theo tu?n |
| `interval.month` | Monthly | Theo tháng |
| `comparison.growth` | Growth | T?ng tr??ng |
| `comparison.vs_previous` | vs previous period | so v?i k? tr??c |

---

## Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2024-01-15 | Initial Phase 3 implementation |
