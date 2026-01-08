# ?? Phase 4: Bulk Actions API - Frontend Specification

## Overview

Phase 4 cung c?p các API endpoints ?? th?c hi?n hành ??ng hàng lo?t (bulk actions) cho Admin Portal.

**M?c ?ích**: Cho phép Admin th?c hi?n các thao tác trên nhi?u ??i t??ng cùng lúc:
- Ban/Unban/Warn nhi?u users
- Xóa nhi?u posts/comments
- Resolve/Reject nhi?u reports

---

## 1. API Endpoints Summary

### Primary Endpoints (Recommended)

Bulk actions ?ã ???c tích h?p tr?c ti?p vào các controller t??ng ?ng:

#### User Management (`/api/admin/users`)
| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/api/admin/users/bulk/ban` | POST | Ban nhi?u users |
| `/api/admin/users/bulk/unban` | POST | Unban nhi?u users |
| `/api/admin/users/bulk/warn` | POST | Warn nhi?u users |

#### Content Management (`/api/admin/content`)
| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/api/admin/content/posts/bulk/delete` | POST | Xóa nhi?u posts |
| `/api/admin/content/comments/bulk/delete` | POST | Xóa nhi?u comments |

#### Report Management (`/api/admin/reports`)
| Endpoint | Method | Mô t? |
|----------|--------|-------|
| `/api/admin/reports/bulk/resolve` | POST | Resolve nhi?u reports |
| `/api/admin/reports/bulk/reject` | POST | Reject nhi?u reports |

### Legacy Endpoints (Backward Compatible)

Các endpoints d??i ?ây v?n ho?t ??ng nh?ng khuy?n khích s? d?ng primary endpoints:

| Endpoint | Method | Redirect To |
|----------|--------|-------------|
| `/api/admin/bulk/users/ban` | POST | `/api/admin/users/bulk/ban` |
| `/api/admin/bulk/users/unban` | POST | `/api/admin/users/bulk/unban` |
| `/api/admin/bulk/users/warn` | POST | `/api/admin/users/bulk/warn` |
| `/api/admin/bulk/posts/delete` | POST | `/api/admin/content/posts/bulk/delete` |
| `/api/admin/bulk/comments/delete` | POST | `/api/admin/content/comments/bulk/delete` |
| `/api/admin/bulk/reports/resolve` | POST | `/api/admin/reports/bulk/resolve` |

---

## 2. Common Response Format

T?t c? bulk actions tr? v? cùng format response:

```json
{
  "totalRequested": 10,
  "successCount": 8,
  "failedCount": 2,
  "results": [
    { "id": "guid-1", "success": true, "error": null },
    { "id": "guid-2", "success": true, "error": null },
    { "id": "guid-3", "success": false, "error": "User is already banned" },
    { "id": "guid-4", "success": false, "error": "Profile not found" }
  ]
}
```

---

## 3. User Moderation Endpoints

### 3.1 `POST /api/admin/users/bulk/ban`

**Mô t?**: Ban nhi?u users cùng lúc (t?i ?a 100)

**Request Body**:
```json
{
  "profileIds": [
    "guid-1",
    "guid-2",
    "guid-3"
  ],
  "reason": "Spam content across multiple posts",
  "durationDays": 30
}
```

| Field | Type | Required | Mô t? |
|-------|------|----------|-------|
| `profileIds` | guid[] | ? | Danh sách profile IDs (max 100) |
| `reason` | string | ? | Lý do ban |
| `durationDays` | int? | ? | S? ngày ban (null = v?nh vi?n) |

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

**Possible Errors per Item**:
- `"Profile not found"` - User không t?n t?i
- `"User is already banned"` - User ?ã b? ban

---

### 3.2 `POST /api/admin/users/bulk/unban`

**Mô t?**: Unban nhi?u users cùng lúc

**Request Body**:
```json
{
  "profileIds": [
    "guid-1",
    "guid-2"
  ],
  "reason": "Ban appeal approved"
}
```

| Field | Type | Required | Mô t? |
|-------|------|----------|-------|
| `profileIds` | guid[] | ? | Danh sách profile IDs |
| `reason` | string | ? | Lý do unban |

**Possible Errors per Item**:
- `"Profile not found"` - User không t?n t?i
- `"User is not banned"` - User không b? ban

---

### 3.3 `POST /api/admin/users/bulk/warn`

**Mô t?**: G?i c?nh cáo ??n nhi?u users

**Request Body**:
```json
{
  "profileIds": [
    "guid-1",
    "guid-2"
  ],
  "reason": "Inappropriate comments"
}
```

| Field | Type | Required | Mô t? |
|-------|------|----------|-------|
| `profileIds` | guid[] | ? | Danh sách profile IDs |
| `reason` | string | ? | Lý do c?nh cáo |

---

## 4. Content Moderation Endpoints

### 4.1 `POST /api/admin/content/posts/bulk/delete`

**Mô t?**: Xóa nhi?u posts (soft delete)

**Request Body**:
```json
{
  "postIds": [
    "post-guid-1",
    "post-guid-2",
    "post-guid-3"
  ],
  "reason": "Violates community guidelines"
}
```

| Field | Type | Required | Mô t? |
|-------|------|----------|-------|
| `postIds` | guid[] | ? | Danh sách post IDs (max 100) |
| `reason` | string | ? | Lý do xóa |

**Possible Errors per Item**:
- `"Post not found"` - Post không t?n t?i
- `"Post is already deleted"` - Post ?ã b? xóa

---

### 4.2 `POST /api/admin/content/comments/bulk/delete`

**Mô t?**: Xóa nhi?u comments (hard delete)

**Request Body**:
```json
{
  "commentIds": [
    "comment-guid-1",
    "comment-guid-2"
  ],
  "reason": "Harassment"
}
```

| Field | Type | Required | Mô t? |
|-------|------|----------|-------|
| `commentIds` | guid[] | ? | Danh sách comment IDs |
| `reason` | string | ? | Lý do xóa |

---

## 5. Report Management Endpoints

### 5.1 `POST /api/admin/reports/bulk/resolve`

**Mô t?**: Resolve ho?c Reject nhi?u reports

**Request Body**:
```json
{
  "reportIds": [
    "report-guid-1",
    "report-guid-2",
    "report-guid-3"
  ],
  "newStatus": "Resolved"
}
```

| Field | Type | Required | Values | Mô t? |
|-------|------|----------|--------|-------|
| `reportIds` | guid[] | ? | - | Danh sách report IDs |
| `newStatus` | enum | ? | `Resolved`, `Rejected` | Tr?ng thái m?i |

**Possible Errors per Item**:
- `"Report not found"` - Report không t?n t?i
- `"Report is already Resolved"` - Report không còn pending

---

## 6. TypeScript Interfaces

```typescript
// ===== Request Types =====

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

// ===== Response Types =====

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
```

---

## 7. API Service Implementation

```typescript
// lib/api/bulkActions.ts

import { apiClient } from './client';

export const bulkActionsApi = {
  // User moderation
  bulkBan: (request: BulkBanRequest) =>
    apiClient.post<BulkActionResponse>('/api/admin/users/bulk/ban', request),

  bulkUnban: (request: BulkUnbanRequest) =>
    apiClient.post<BulkActionResponse>('/api/admin/users/bulk/unban', request),

  bulkWarn: (request: BulkWarnRequest) =>
    apiClient.post<BulkActionResponse>('/api/admin/users/bulk/warn', request),

  // Content moderation
  bulkDeletePosts: (request: BulkDeletePostsRequest) =>
    apiClient.post<BulkActionResponse>('/api/admin/content/posts/bulk/delete', request),

  bulkDeleteComments: (request: BulkDeleteCommentsRequest) =>
    apiClient.post<BulkActionResponse>('/api/admin/content/comments/bulk/delete', request),

  // Report management
  bulkResolveReports: (request: BulkResolveReportsRequest) =>
    apiClient.post<BulkActionResponse>('/api/admin/reports/bulk/resolve', request),
};
```

---

## 8. React Hooks Implementation

```typescript
// hooks/useBulkActions.ts

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { bulkActionsApi } from '@/lib/api/bulkActions';
import { toast } from 'sonner';

export function useBulkBan() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: bulkActionsApi.bulkBan,
    onSuccess: (data) => {
      if (data.failedCount === 0) {
        toast.success(`Successfully banned ${data.successCount} users`);
      } else {
        toast.warning(
          `Banned ${data.successCount} users, ${data.failedCount} failed`
        );
      }
      queryClient.invalidateQueries({ queryKey: ['admin', 'analytics', 'users'] });
    },
    onError: () => {
      toast.error('Failed to perform bulk ban');
    },
  });
}

export function useBulkUnban() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: bulkActionsApi.bulkUnban,
    onSuccess: (data) => {
      if (data.failedCount === 0) {
        toast.success(`Successfully unbanned ${data.successCount} users`);
      } else {
        toast.warning(
          `Unbanned ${data.successCount} users, ${data.failedCount} failed`
        );
      }
      queryClient.invalidateQueries({ queryKey: ['admin', 'analytics', 'users'] });
    },
  });
}

export function useBulkWarn() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: bulkActionsApi.bulkWarn,
    onSuccess: (data) => {
      toast.success(`Successfully warned ${data.successCount} users`);
      queryClient.invalidateQueries({ queryKey: ['admin', 'analytics', 'users'] });
    },
  });
}

export function useBulkDeletePosts() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: bulkActionsApi.bulkDeletePosts,
    onSuccess: (data) => {
      if (data.failedCount === 0) {
        toast.success(`Successfully deleted ${data.successCount} posts`);
      } else {
        toast.warning(
          `Deleted ${data.successCount} posts, ${data.failedCount} failed`
        );
      }
      queryClient.invalidateQueries({ queryKey: ['admin', 'analytics', 'posts'] });
    },
  });
}

export function useBulkDeleteComments() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: bulkActionsApi.bulkDeleteComments,
    onSuccess: (data) => {
      toast.success(`Successfully deleted ${data.successCount} comments`);
    },
  });
}

export function useBulkResolveReports() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: bulkActionsApi.bulkResolveReports,
    onSuccess: (data, variables) => {
      const action = variables.newStatus === 'Resolved' ? 'resolved' : 'rejected';
      toast.success(`Successfully ${action} ${data.successCount} reports`);
      queryClient.invalidateQueries({ queryKey: ['admin', 'reports'] });
    },
  });
}
```

---

## 9. UI Components Specification

### 9.1 Bulk Action Toolbar

```
???????????????????????????????????????????????????????????????????????????????
? ? Select All                                     Selected: 5 users          ?
???????????????????????????????????????????????????????????????????????????????
?                                                                              ?
?  ???????????????? ???????????????? ???????????????? ????????????????       ?
?  ? ?? Ban All   ? ? ? Unban All ? ? ?? Warn All  ? ? ? Clear     ?       ?
?  ???????????????? ???????????????? ???????????????? ????????????????       ?
?                                                                              ?
???????????????????????????????????????????????????????????????????????????????
```

### 9.2 Bulk Action Confirmation Modal

```
???????????????????????????????????????????????????????????????????
?                    Bulk Ban Confirmation                  [X]   ?
???????????????????????????????????????????????????????????????????
?                                                                  ?
?  You are about to ban 5 users.                                  ?
?                                                                  ?
?  Reason: *                                                       ?
?  ?????????????????????????????????????????????????????????????  ?
?  ? Enter reason for ban...                                   ?  ?
?  ?????????????????????????????????????????????????????????????  ?
?                                                                  ?
?  Duration:                                                       ?
?  ?????????????????                                              ?
?  ? 30 days     ? ?  ? Permanent                                ?
?  ?????????????????                                              ?
?                                                                  ?
?  ?? This action will be logged and cannot be easily undone.     ?
?                                                                  ?
?                            [Cancel]  [Confirm Ban]              ?
???????????????????????????????????????????????????????????????????
```

### 9.3 Bulk Action Results Dialog

```
???????????????????????????????????????????????????????????????????
?                    Bulk Action Results                    [X]   ?
???????????????????????????????????????????????????????????????????
?                                                                  ?
?  ???????????????????????????????????????????????????????????   ?
?  ?  ? Success: 8 / 10                                      ?   ?
?  ?  ? Failed: 2 / 10                                       ?   ?
?  ???????????????????????????????????????????????????????????   ?
?                                                                  ?
?  Failed Items:                                                   ?
?  ?????????????????????????????????????????????????????????????  ?
?  ? • user123 - User is already banned                        ?  ?
?  ? • user456 - Profile not found                             ?  ?
?  ?????????????????????????????????????????????????????????????  ?
?                                                                  ?
?                                              [Close]            ?
???????????????????????????????????????????????????????????????????
```

### 9.4 Component Structure

```
admin/
??? components/
?   ??? BulkActionToolbar.tsx       # Toolbar with bulk action buttons
?   ??? BulkActionModal.tsx         # Confirmation modal
?   ??? BulkActionResultsDialog.tsx # Results dialog
?   ??? SelectableTable.tsx         # Table with selection
?   ??? SelectAllCheckbox.tsx       # Select all header checkbox
??? hooks/
?   ??? useBulkSelection.ts         # Selection state management
?   ??? useBulkActions.ts           # Mutation hooks
```

---

## 10. Selection State Management

```typescript
// hooks/useBulkSelection.ts

import { useState, useCallback, useMemo } from 'react';

interface UseBulkSelectionOptions<T> {
  items: T[];
  getItemId: (item: T) => string;
}

export function useBulkSelection<T>({ items, getItemId }: UseBulkSelectionOptions<T>) {
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());

  const isSelected = useCallback(
    (item: T) => selectedIds.has(getItemId(item)),
    [selectedIds, getItemId]
  );

  const toggle = useCallback(
    (item: T) => {
      const id = getItemId(item);
      setSelectedIds((prev) => {
        const next = new Set(prev);
        if (next.has(id)) {
          next.delete(id);
        } else {
          next.add(id);
        }
        return next;
      });
    },
    [getItemId]
  );

  const selectAll = useCallback(() => {
    setSelectedIds(new Set(items.map(getItemId)));
  }, [items, getItemId]);

  const clearSelection = useCallback(() => {
    setSelectedIds(new Set());
  }, []);

  const isAllSelected = useMemo(
    () => items.length > 0 && selectedIds.size === items.length,
    [items, selectedIds]
  );

  const isSomeSelected = useMemo(
    () => selectedIds.size > 0 && selectedIds.size < items.length,
    [items, selectedIds]
  );

  const selectedCount = selectedIds.size;
  const selectedIdsArray = Array.from(selectedIds);

  return {
    selectedIds: selectedIdsArray,
    selectedCount,
    isSelected,
    toggle,
    selectAll,
    clearSelection,
    isAllSelected,
    isSomeSelected,
  };
}
```

---

## 11. Best Practices

### 11.1 Confirmation Before Action

```typescript
// Always show confirmation modal before bulk actions
const handleBulkBan = () => {
  if (selectedIds.length === 0) {
    toast.error('No users selected');
    return;
  }
  setShowConfirmModal(true);
};
```

### 11.2 Optimistic Updates with Rollback

```typescript
// For better UX, update UI optimistically
const handleBulkDelete = async () => {
  // Optimistically remove from UI
  setItems(items.filter(item => !selectedIds.includes(item.id)));
  
  try {
    const result = await bulkDeletePosts({ postIds: selectedIds, reason });
    
    if (result.failedCount > 0) {
      // Rollback failed items
      const failedIds = result.results
        .filter(r => !r.success)
        .map(r => r.id);
      
      // Refetch to restore failed items
      await refetch();
    }
  } catch (error) {
    // Full rollback on error
    await refetch();
  }
};
```

### 11.3 Rate Limiting Awareness

```typescript
// Show warning if selecting too many items
const MAX_BULK_ITEMS = 100;

{selectedCount > MAX_BULK_ITEMS && (
  <Alert variant="warning">
    Maximum {MAX_BULK_ITEMS} items per request. 
    Only the first {MAX_BULK_ITEMS} will be processed.
  </Alert>
)}
```

---

## 12. Error Handling

### 12.1 Request-Level Errors

| Status | Meaning | Action |
|--------|---------|--------|
| 400 | Invalid request | Show validation errors |
| 401 | Unauthorized | Redirect to login |
| 403 | Forbidden | Show permission error |
| 500 | Server error | Show generic error |

### 12.2 Item-Level Errors

Display item-level errors in results dialog:

```typescript
const failedItems = result.results.filter(r => !r.success);

if (failedItems.length > 0) {
  setFailedItems(failedItems);
  setShowResultsDialog(true);
}
```

---

## 13. Audit Trail

All bulk actions are logged with `[Bulk]` prefix:

| Action | Audit Log Entry |
|--------|-----------------|
| Bulk Ban | `[Bulk] Spam content` |
| Bulk Delete | `[Bulk] Violates guidelines` |
| Bulk Resolve | `[Bulk] Resolved report {id}` |

Admin có th? filter audit logs v?i search `[Bulk]` ?? xem t?t c? bulk actions.

---

## 14. Accessibility (A11y)

1. **Keyboard Navigation**: Support Shift+Click for range selection
2. **Screen Readers**: Announce selection count changes
3. **Focus Management**: Return focus to table after modal closes
4. **Confirmation**: Require explicit confirmation before destructive actions

---

## 15. Localization Notes

| Key | English | Vietnamese |
|-----|---------|------------|
| `bulk.ban` | Ban Selected | Khóa ?ã ch?n |
| `bulk.unban` | Unban Selected | M? khóa ?ã ch?n |
| `bulk.warn` | Warn Selected | C?nh cáo ?ã ch?n |
| `bulk.delete` | Delete Selected | Xóa ?ã ch?n |
| `bulk.resolve` | Resolve Selected | X? lý ?ã ch?n |
| `bulk.success` | {count} items processed | ?ã x? lý {count} m?c |
| `bulk.failed` | {count} items failed | {count} m?c th?t b?i |
| `bulk.confirm` | Confirm Action | Xác nh?n thao tác |

---

## 16. Testing Checklist

### Unit Tests
- [ ] Selection state management
- [ ] Confirmation modal logic
- [ ] Results processing

### Integration Tests
- [ ] Bulk ban with mixed results
- [ ] Bulk delete with some items not found
- [ ] Rate limiting (100 item max)

### E2E Tests
- [ ] Select multiple items and ban
- [ ] Verify audit logs created
- [ ] Verify UI updates after action

---

## Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2024-01-15 | Initial Phase 4 implementation |
