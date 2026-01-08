# ?? Phase 2: Audit Logs API - Frontend Specification

## Overview

Phase 2 cung c?p các API endpoints ?? frontend hi?n th? nh?t ký hành ??ng c?a Admin (Audit Logs) trong Admin Portal.

**M?c ?ích**: Cho phép Admin xem l?ch s? các hành ??ng qu?n tr? ?ã th?c hi?n bao g?m:
- Ban/Unban/Warn users
- Delete content (posts, comments)
- Resolve reports
- T?t c? các admin actions khác

---

## 0. Ngu?n d? li?u Audit Logs

### Các Admin Actions ???c ghi log t? ??ng

Khi admin th?c hi?n các thao tác sau, h? th?ng s? t? ??ng ghi log vào database:

| Service | API Endpoint | Action Type | Mô t? |
|---------|--------------|-------------|-------|
| `UserModerationService` | `POST /api/admin/users/{id}/ban` | `BanUser` | Khóa tài kho?n user |
| `UserModerationService` | `DELETE /api/admin/users/{id}/ban` | `UnbanUser` | M? khóa tài kho?n user |
| `UserModerationService` | `POST /api/admin/users/{id}/warn` | `WarnUser` | C?nh cáo user |
| `PostService` | `DELETE /api/admin/content/posts/{id}` | `DeleteContent` | Xóa bài vi?t (Post) |
| `CommentService` | `DELETE /api/admin/content/comments/{id}` | `DeleteContent` | Xóa bình lu?n (Comment) |
| `ReportService` | `PUT /api/admin/reports/{id}/status` | `ResolveReport` | X? lý report |

### C?u trúc d? li?u AdminAction trong Database

```
AdminActions Table:
??? Id (Guid)                 - Primary key
??? AdminId (Guid)            - ID c?a admin th?c hi?n
??? ActionType (Enum)         - Lo?i action
??? TargetProfileId (Guid?)   - User b? tác ??ng
??? TargetEntityId (Guid?)    - Entity b? tác ??ng (Post/Comment ID)
??? TargetEntityType (string?)- "Post", "Comment"
??? ReportId (Guid?)          - Report liên quan (n?u có)
??? Notes (string?)           - Lý do/ghi chú
??? CreatedAt (DateTime)      - Th?i gian th?c hi?n
```

---

## 1. API Endpoints Summary

| Endpoint | Method | Auth | Mô t? |
|----------|--------|------|-------|
| `/api/admin/audit` | GET | ? Admin | L?y danh sách audit logs v?i filters |
| `/api/admin/audit/{id}` | GET | ? Admin | L?y chi ti?t m?t audit log |
| `/api/admin/audit/action-types` | GET | ? Admin | L?y danh sách action types |
| `/api/admin/audit/summary` | GET | ? Admin | L?y th?ng kê action types |

---

## 2. API Endpoints Chi Ti?t

### 2.1 `GET /api/admin/audit`

**Mô t?**: L?y danh sách audit logs v?i phân trang và b? l?c

**Headers**:
```
Authorization: Bearer <admin_jwt_token>
```

**Query Parameters**:

| Parameter | Type | Required | Default | Mô t? |
|-----------|------|----------|---------|-------|
| `actionType` | string | ? | - | Filter theo lo?i action (enum name) |
| `adminId` | guid | ? | - | Filter theo admin th?c hi?n |
| `targetProfileId` | guid | ? | - | Filter theo profile b? tác ??ng |
| `fromDate` | date | ? | - | Filter t? ngày (inclusive) |
| `toDate` | date | ? | - | Filter ??n ngày (inclusive) |
| `search` | string | ? | - | Tìm ki?m trong notes |
| `page` | int | ? | 1 | S? trang |
| `pageSize` | int | ? | 20 | S? items/trang (max: 100) |

**Request Example**:
```
GET /api/admin/audit?actionType=BanUser&fromDate=2024-01-01&page=1&pageSize=20
```

**Response**: `200 OK`
```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "adminId": "123e4567-e89b-12d3-a456-426614174000",
      "adminUsername": "admin_user",
      "adminDisplayName": "System Admin",
      "actionType": "BanUser",
      "actionTypeDisplayName": "Ban User",
      "targetProfileId": "987fcdeb-51a2-3bc4-d567-890123456789",
      "targetUsername": "banned_user",
      "targetDisplayName": "Banned User Name",
      "targetEntityId": null,
      "targetEntityType": null,
      "reportId": null,
      "notes": "Spam content, multiple violations",
      "createdAt": "2024-01-15T10:30:00.000Z"
    },
    {
      "id": "660e8400-e29b-41d4-a716-446655440001",
      "adminId": "123e4567-e89b-12d3-a456-426614174000",
      "adminUsername": "admin_user",
      "adminDisplayName": "System Admin",
      "actionType": "DeleteContent",
      "actionTypeDisplayName": "Delete Content",
      "targetProfileId": "abc12345-51a2-3bc4-d567-890123456789",
      "targetUsername": "content_owner",
      "targetDisplayName": "Content Owner",
      "targetEntityId": "post-id-12345",
      "targetEntityType": "Post",
      "reportId": "report-id-67890",
      "notes": "Inappropriate content reported by multiple users",
      "createdAt": "2024-01-14T15:45:00.000Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 150
}
```

**Error Responses**:

`401 Unauthorized`:
```json
{
  "code": "UNAUTHORIZED",
  "message": "Authentication required"
}
```

`403 Forbidden`:
```json
{
  "code": "FORBIDDEN",
  "message": "Admin access required"
}
```

---

### 2.2 `GET /api/admin/audit/{id}`

**Mô t?**: L?y chi ti?t m?t audit log theo ID

**Headers**:
```
Authorization: Bearer <admin_jwt_token>
```

**Path Parameters**:

| Parameter | Type | Mô t? |
|-----------|------|-------|
| `id` | guid | ID c?a audit log |

**Response**: `200 OK`
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "adminId": "123e4567-e89b-12d3-a456-426614174000",
  "adminUsername": "admin_user",
  "adminDisplayName": "System Admin",
  "actionType": "BanUser",
  "actionTypeDisplayName": "Ban User",
  "targetProfileId": "987fcdeb-51a2-3bc4-d567-890123456789",
  "targetUsername": "banned_user",
  "targetDisplayName": "Banned User Name",
  "targetEntityId": null,
  "targetEntityType": null,
  "reportId": null,
  "notes": "Spam content, multiple violations",
  "createdAt": "2024-01-15T10:30:00.000Z"
}
```

**Error Response**: `404 Not Found`
```json
{
  "code": "AUDIT_LOG_NOT_FOUND",
  "message": "Audit log not found."
}
```

---

### 2.3 `GET /api/admin/audit/action-types`

**Mô t?**: L?y danh sách t?t c? action types có s?n

**Headers**:
```
Authorization: Bearer <admin_jwt_token>
```

**Response**: `200 OK`
```json
[
  {
    "value": "BanUser",
    "name": "BanUser",
    "displayName": "Ban User"
  },
  {
    "value": "UnbanUser",
    "name": "UnbanUser",
    "displayName": "Unban User"
  },
  {
    "value": "WarnUser",
    "name": "WarnUser",
    "displayName": "Warn User"
  },
  {
    "value": "ResolveReport",
    "name": "ResolveReport",
    "displayName": "Resolve Report"
  },
  {
    "value": "DeleteContent",
    "name": "DeleteContent",
    "displayName": "Delete Content"
  }
]
```

---

### 2.4 `GET /api/admin/audit/summary`

**Mô t?**: L?y th?ng kê s? l??ng theo action type

**Headers**:
```
Authorization: Bearer <admin_jwt_token>
```

**Query Parameters**:

| Parameter | Type | Required | Mô t? |
|-----------|------|----------|-------|
| `fromDate` | date | ? | Filter t? ngày |
| `toDate` | date | ? | Filter ??n ngày |

**Request Example**:
```
GET /api/admin/audit/summary?fromDate=2024-01-01&toDate=2024-01-31
```

**Response**: `200 OK`
```json
[
  {
    "actionType": "BanUser",
    "displayName": "Ban User",
    "count": 45
  },
  {
    "actionType": "DeleteContent",
    "displayName": "Delete Content",
    "count": 32
  },
  {
    "actionType": "WarnUser",
    "displayName": "Warn User",
    "count": 28
  },
  {
    "actionType": "ResolveReport",
    "displayName": "Resolve Report",
    "count": 15
  },
  {
    "actionType": "UnbanUser",
    "displayName": "Unban User",
    "count": 5
  }
]
```

---

## 3. Enum Values

### AdminActionType

| Value | Name | Display Name | Mô t? |
|-------|------|--------------|-------|
| 0 | Unknown | Unknown | Không xác ??nh |
| 1 | BanUser | Ban User | Khóa tài kho?n user |
| 2 | UnbanUser | Unban User | M? khóa tài kho?n user |
| 3 | WarnUser | Warn User | C?nh cáo user |
| 4 | ResolveReport | Resolve Report | X? lý report |
| 5 | DeleteContent | Delete Content | Xóa n?i dung (post/comment) |

---

## 4. TypeScript Interfaces

```typescript
// ===== Enum =====

type AdminActionType = 
  | 'Unknown'
  | 'BanUser'
  | 'UnbanUser'
  | 'WarnUser'
  | 'ResolveReport'
  | 'DeleteContent';

// ===== Audit Log Types =====

interface AuditLogDto {
  id: string;
  adminId: string;
  adminUsername: string | null;
  adminDisplayName: string | null;
  actionType: AdminActionType;
  actionTypeDisplayName: string;
  targetProfileId: string | null;
  targetUsername: string | null;
  targetDisplayName: string | null;
  targetEntityId: string | null;
  targetEntityType: string | null;
  reportId: string | null;
  notes: string | null;
  createdAt: string; // ISO 8601 format
}

interface AuditLogFilterRequest {
  actionType?: AdminActionType;
  adminId?: string;
  targetProfileId?: string;
  fromDate?: string; // YYYY-MM-DD
  toDate?: string;   // YYYY-MM-DD
  search?: string;
  page?: number;
  pageSize?: number;
}

interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

// ===== Action Type Types =====

interface ActionTypeInfo {
  value: AdminActionType;
  name: string;
  displayName: string;
}

interface AuditActionTypeSummary {
  actionType: AdminActionType;
  displayName: string;
  count: number;
}
```

---

## 5. API Service Implementation (React/Next.js)

```typescript
// lib/api/audit.ts

import { apiClient } from './client';

const AUDIT_ENDPOINTS = {
  logs: '/api/admin/audit',
  logById: (id: string) => `/api/admin/audit/${id}`,
  actionTypes: '/api/admin/audit/action-types',
  summary: '/api/admin/audit/summary',
} as const;

export const auditApi = {
  /**
   * Get paginated audit logs with filters
   */
  getLogs: async (filter: AuditLogFilterRequest = {}): Promise<PagedResult<AuditLogDto>> => {
    const params = new URLSearchParams();
    
    if (filter.actionType) params.append('actionType', filter.actionType);
    if (filter.adminId) params.append('adminId', filter.adminId);
    if (filter.targetProfileId) params.append('targetProfileId', filter.targetProfileId);
    if (filter.fromDate) params.append('fromDate', filter.fromDate);
    if (filter.toDate) params.append('toDate', filter.toDate);
    if (filter.search) params.append('search', filter.search);
    if (filter.page) params.append('page', filter.page.toString());
    if (filter.pageSize) params.append('pageSize', filter.pageSize.toString());

    const queryString = params.toString();
    const url = queryString ? `${AUDIT_ENDPOINTS.logs}?${queryString}` : AUDIT_ENDPOINTS.logs;
    
    return apiClient.get(url);
  },

  /**
   * Get a specific audit log by ID
   */
  getLogById: async (id: string): Promise<AuditLogDto> => {
    return apiClient.get(AUDIT_ENDPOINTS.logById(id));
  },

  /**
   * Get all available action types
   */
  getActionTypes: async (): Promise<ActionTypeInfo[]> => {
    return apiClient.get(AUDIT_ENDPOINTS.actionTypes);
  },

  /**
   * Get action type summary with counts
   */
  getSummary: async (fromDate?: string, toDate?: string): Promise<AuditActionTypeSummary[]> => {
    const params = new URLSearchParams();
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);

    const queryString = params.toString();
    const url = queryString ? `${AUDIT_ENDPOINTS.summary}?${queryString}` : AUDIT_ENDPOINTS.summary;
    
    return apiClient.get(url);
  },
};
```

---

## 6. React Hooks Implementation

```typescript
// hooks/useAuditLogs.ts

import { useQuery, useInfiniteQuery } from '@tanstack/react-query';
import { auditApi } from '@/lib/api/audit';

const QUERY_KEYS = {
  logs: (filter: AuditLogFilterRequest) => ['admin', 'audit', 'logs', filter],
  logById: (id: string) => ['admin', 'audit', 'log', id],
  actionTypes: ['admin', 'audit', 'action-types'],
  summary: (fromDate?: string, toDate?: string) => ['admin', 'audit', 'summary', fromDate, toDate],
} as const;

/**
 * Hook ?? l?y audit logs v?i filters
 */
export function useAuditLogs(filter: AuditLogFilterRequest = {}) {
  return useQuery({
    queryKey: QUERY_KEYS.logs(filter),
    queryFn: () => auditApi.getLogs(filter),
    staleTime: 30000, // 30 seconds
  });
}

/**
 * Hook ?? l?y audit logs v?i infinite scroll
 */
export function useInfiniteAuditLogs(filter: Omit<AuditLogFilterRequest, 'page'>) {
  return useInfiniteQuery({
    queryKey: ['admin', 'audit', 'logs', 'infinite', filter],
    queryFn: ({ pageParam = 1 }) => auditApi.getLogs({ ...filter, page: pageParam }),
    getNextPageParam: (lastPage) => {
      const totalPages = Math.ceil(lastPage.totalCount / lastPage.pageSize);
      return lastPage.page < totalPages ? lastPage.page + 1 : undefined;
    },
    initialPageParam: 1,
  });
}

/**
 * Hook ?? l?y chi ti?t m?t audit log
 */
export function useAuditLog(id: string) {
  return useQuery({
    queryKey: QUERY_KEYS.logById(id),
    queryFn: () => auditApi.getLogById(id),
    enabled: !!id,
  });
}

/**
 * Hook ?? l?y action types (cached lâu vì ít thay ??i)
 */
export function useActionTypes() {
  return useQuery({
    queryKey: QUERY_KEYS.actionTypes,
    queryFn: auditApi.getActionTypes,
    staleTime: Infinity, // Never stale - action types don't change
    gcTime: Infinity,
  });
}

/**
 * Hook ?? l?y summary statistics
 */
export function useAuditSummary(fromDate?: string, toDate?: string) {
  return useQuery({
    queryKey: QUERY_KEYS.summary(fromDate, toDate),
    queryFn: () => auditApi.getSummary(fromDate, toDate),
    staleTime: 60000, // 1 minute
  });
}
```

---

## 7. UI Components Specification

### 7.1 Action Type Badge

```typescript
// components/admin/ActionTypeBadge.tsx

interface ActionTypeBadgeProps {
  actionType: AdminActionType;
  displayName?: string;
}

/**
 * Badge hi?n th? lo?i action v?i màu t??ng ?ng
 * 
 * Colors:
 * - BanUser: Red (#EF4444)
 * - UnbanUser: Green (#22C55E)
 * - WarnUser: Yellow (#F59E0B)
 * - ResolveReport: Blue (#3B82F6)
 * - DeleteContent: Purple (#8B5CF6)
 * - Unknown: Gray (#6B7280)
 */
```

### 7.2 Audit Logs Table

```
???????????????????????????????????????????????????????????????????????????????
?                           Audit Logs                                         ?
???????????????????????????????????????????????????????????????????????????????
?                                                                               ?
?  Filters:                                                                     ?
?  ?????????????? ?????????????? ?????????????? ?????????????? ????????????????
?  ?Action Type?? ? From Date  ? ?  To Date   ? ?  Search... ? ? ?? Filter  ??
?  ?????????????? ?????????????? ?????????????? ?????????????? ????????????????
?                                                                               ?
?  ?????????????????????????????????????????????????????????????????????????  ?
?  ? Action        ? Admin       ? Target      ? Notes          ? Date     ?  ?
?  ?????????????????????????????????????????????????????????????????????????  ?
?  ? ?? Ban User   ? admin_user  ? bad_user    ? Spam content   ? Jan 15   ?  ?
?  ? ?? Delete...  ? admin_user  ? user123     ? Inappropriate  ? Jan 14   ?  ?
?  ? ?? Warn User  ? mod_user    ? newbie      ? First warning  ? Jan 13   ?  ?
?  ? ?? Unban User ? admin_user  ? reformed    ? Appeal accepted? Jan 12   ?  ?
?  ? ?? Resolve... ? admin_user  ? -           ? False report   ? Jan 11   ?  ?
?  ?????????????????????????????????????????????????????????????????????????  ?
?                                                                               ?
?  Showing 1-20 of 150                    [< Prev] [1] [2] [3] ... [Next >]    ?
???????????????????????????????????????????????????????????????????????????????
```

### 7.3 Audit Summary Cards

```
???????????????????????????????????????????????????????????????????????????????
?                        Action Summary (Last 30 days)                         ?
???????????????????????????????????????????????????????????????????????????????
?                                                                               ?
?  ????????????????  ????????????????  ????????????????  ????????????????     ?
?  ?  ?? Ban      ?  ?  ?? Delete   ?  ?  ?? Warn     ?  ?  ?? Resolve  ?     ?
?  ?     45       ?  ?     32       ?  ?     28       ?  ?     15       ?     ?
?  ?   users      ?  ?   content    ?  ?   warnings   ?  ?   reports    ?     ?
?  ????????????????  ????????????????  ????????????????  ????????????????     ?
?                                                                               ?
???????????????????????????????????????????????????????????????????????????????
```

### 7.4 Audit Log Detail Modal

```
???????????????????????????????????????????????????????????????????
?                    Audit Log Details                     [X]    ?
???????????????????????????????????????????????????????????????????
?                                                                  ?
?  Action:        ?? Ban User                                     ?
?  Date:          January 15, 2024 at 10:30 AM                    ?
?                                                                  ?
?  ?????????????????????????????????????????????????????????????  ?
?                                                                  ?
?  Admin:         @admin_user (System Admin)                      ?
?  Target User:   @banned_user (Banned User Name)                 ?
?                                                                  ?
?  ?????????????????????????????????????????????????????????????  ?
?                                                                  ?
?  Notes:                                                          ?
?  ?????????????????????????????????????????????????????????????  ?
?  ? Spam content, multiple violations. User has been warned   ?  ?
?  ? twice before. Permanent ban applied.                      ?  ?
?  ?????????????????????????????????????????????????????????????  ?
?                                                                  ?
?  Related Report: #67890                                         ?
?                                                                  ?
?                                              [Close]            ?
???????????????????????????????????????????????????????????????????
```

### 7.5 Component Structure

```
admin/
??? audit/
?   ??? AuditLogsPage.tsx           # Main page component
?   ??? AuditLogsTable.tsx          # Table with pagination
?   ??? AuditLogRow.tsx             # Individual row component
?   ??? AuditLogDetailModal.tsx     # Detail modal
?   ??? AuditFilters.tsx            # Filter controls
?   ??? AuditSummaryCards.tsx       # Summary statistics cards
?   ??? ActionTypeBadge.tsx         # Action type badge
?   ??? ActionTypeSelect.tsx        # Dropdown for action type filter
?   ??? DateRangePicker.tsx         # Date range picker for filters
```

---

## 8. Filter State Management

```typescript
// hooks/useAuditFilters.ts

import { useState, useCallback } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';

interface AuditFiltersState {
  actionType: AdminActionType | null;
  adminId: string | null;
  targetProfileId: string | null;
  fromDate: string | null;
  toDate: string | null;
  search: string;
  page: number;
  pageSize: number;
}

export function useAuditFilters() {
  const router = useRouter();
  const searchParams = useSearchParams();

  // Parse filters from URL
  const filters: AuditFiltersState = {
    actionType: searchParams.get('actionType') as AdminActionType | null,
    adminId: searchParams.get('adminId'),
    targetProfileId: searchParams.get('targetProfileId'),
    fromDate: searchParams.get('fromDate'),
    toDate: searchParams.get('toDate'),
    search: searchParams.get('search') || '',
    page: parseInt(searchParams.get('page') || '1'),
    pageSize: parseInt(searchParams.get('pageSize') || '20'),
  };

  // Update URL when filters change
  const setFilters = useCallback((newFilters: Partial<AuditFiltersState>) => {
    const params = new URLSearchParams();
    const merged = { ...filters, ...newFilters };

    if (merged.actionType) params.set('actionType', merged.actionType);
    if (merged.adminId) params.set('adminId', merged.adminId);
    if (merged.targetProfileId) params.set('targetProfileId', merged.targetProfileId);
    if (merged.fromDate) params.set('fromDate', merged.fromDate);
    if (merged.toDate) params.set('toDate', merged.toDate);
    if (merged.search) params.set('search', merged.search);
    if (merged.page > 1) params.set('page', merged.page.toString());
    if (merged.pageSize !== 20) params.set('pageSize', merged.pageSize.toString());

    router.push(`?${params.toString()}`);
  }, [filters, router]);

  const resetFilters = useCallback(() => {
    router.push('');
  }, [router]);

  return { filters, setFilters, resetFilters };
}
```

---

## 9. Date Formatting Utilities

```typescript
// lib/utils/date.ts

/**
 * Format date for display in audit logs
 */
export function formatAuditDate(dateString: string): string {
  const date = new Date(dateString);
  return new Intl.DateTimeFormat('vi-VN', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
}

/**
 * Format date for API filter (YYYY-MM-DD)
 */
export function formatDateForApi(date: Date): string {
  return date.toISOString().split('T')[0];
}

/**
 * Get relative time string
 */
export function getRelativeTime(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMs / 3600000);
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffMins < 1) return 'V?a xong';
  if (diffMins < 60) return `${diffMins} phút tr??c`;
  if (diffHours < 24) return `${diffHours} gi? tr??c`;
  if (diffDays < 7) return `${diffDays} ngày tr??c`;
  
  return formatAuditDate(dateString);
}
```

---

## 10. Action Type Styling

```typescript
// lib/constants/actionTypes.ts

export const ACTION_TYPE_CONFIG: Record<AdminActionType, {
  color: string;
  bgColor: string;
  icon: string;
  label: string;
}> = {
  Unknown: {
    color: '#6B7280',
    bgColor: '#F3F4F6',
    icon: '?',
    label: 'Unknown',
  },
  BanUser: {
    color: '#EF4444',
    bgColor: '#FEE2E2',
    icon: '??',
    label: 'Ban User',
  },
  UnbanUser: {
    color: '#22C55E',
    bgColor: '#DCFCE7',
    icon: '?',
    label: 'Unban User',
  },
  WarnUser: {
    color: '#F59E0B',
    bgColor: '#FEF3C7',
    icon: '??',
    label: 'Warn User',
  },
  ResolveReport: {
    color: '#3B82F6',
    bgColor: '#DBEAFE',
    icon: '??',
    label: 'Resolve Report',
  },
  DeleteContent: {
    color: '#8B5CF6',
    bgColor: '#EDE9FE',
    icon: '???',
    label: 'Delete Content',
  },
};
```

---

## 11. Error Handling

### 11.1 API Errors

```typescript
// Không tìm th?y audit log
{
  code: "AUDIT_LOG_NOT_FOUND",
  message: "Audit log not found."
}
```

**UI Behavior**: Hi?n th? "Not found" message, redirect v? list page

### 11.2 Filter Validation

```typescript
// Invalid date range
if (fromDate && toDate && new Date(fromDate) > new Date(toDate)) {
  // Show error: "From date must be before to date"
}

// Page out of range
if (page > totalPages) {
  // Redirect to last page
}
```

---

## 12. Testing Checklist

### 12.1 Unit Tests
- [ ] ActionTypeBadge renders correct colors for each type
- [ ] Date formatting functions work correctly
- [ ] Filter state management works correctly
- [ ] Pagination calculates correctly

### 12.2 Integration Tests
- [ ] API calls with valid admin token succeed
- [ ] Filters apply correctly to API requests
- [ ] Pagination updates URL and fetches correct data
- [ ] Search debounces correctly (300ms)

### 12.3 E2E Tests
- [ ] Audit logs page loads correctly
- [ ] Clicking on a row opens detail modal
- [ ] Filters persist in URL on page refresh
- [ ] Export functionality works (if implemented)

---

## 13. Performance Considerations

1. **Debounce search input**: 300ms delay before API call
2. **Prefetch next page**: Load next page data in background
3. **Cache action types**: Store indefinitely (doesn't change)
4. **Virtual scrolling**: Consider for large datasets (>1000 rows)
5. **Memoize filter components**: Prevent unnecessary re-renders

---

## 14. Accessibility (A11y)

1. **Table semantics**: Use proper `<table>`, `<thead>`, `<tbody>` elements
2. **Sort indicators**: Add aria-sort attributes to sortable columns
3. **Pagination**: Use aria-label for page numbers
4. **Modal focus trap**: Lock focus within detail modal when open
5. **Date picker**: Ensure keyboard navigation works

---

## 15. Localization Notes

| Key | English | Vietnamese |
|-----|---------|------------|
| `audit.title` | Audit Logs | Nh?t ký h? th?ng |
| `audit.filter.actionType` | Action Type | Lo?i hành ??ng |
| `audit.filter.dateRange` | Date Range | Kho?ng th?i gian |
| `audit.filter.search` | Search notes | Tìm trong ghi chú |
| `audit.column.action` | Action | Hành ??ng |
| `audit.column.admin` | Admin | Qu?n tr? viên |
| `audit.column.target` | Target | ??i t??ng |
| `audit.column.date` | Date | Ngày |
| `audit.action.BanUser` | Ban User | Khóa tài kho?n |
| `audit.action.UnbanUser` | Unban User | M? khóa tài kho?n |
| `audit.action.WarnUser` | Warn User | C?nh cáo |
| `audit.action.ResolveReport` | Resolve Report | X? lý báo cáo |
| `audit.action.DeleteContent` | Delete Content | Xóa n?i dung |

---

## 16. Security Notes

1. **Admin-only access**: All endpoints require `RequireAdmin` policy
2. **No sensitive data in notes**: Avoid logging passwords, tokens
3. **Audit log immutability**: Logs cannot be edited or deleted via API
4. **Rate limiting**: Consider limiting requests to prevent abuse

---

## 17. Future Enhancements

| Feature | Priority | Description |
|---------|----------|-------------|
| Export to CSV | Medium | Allow admins to export logs |
| Real-time updates | Low | WebSocket for live log streaming |
| Advanced filters | Low | Filter by multiple action types |
| Log retention | Medium | Auto-delete logs older than X days |

---

## Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2024-01-15 | Initial Phase 2 implementation |
