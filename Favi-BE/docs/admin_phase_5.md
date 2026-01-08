# ?? Phase 5: Export Data API - Frontend Specification

## Overview

Phase 5 cung c?p các API endpoints ?? Admin export d? li?u ra các ??nh d?ng file khác nhau (CSV, JSON, Excel).

**M?c ?ích**: Cho phép Admin:
- Export danh sách users v?i filters
- Export danh sách posts v?i filters
- Export danh sách reports v?i filters
- Export audit logs v?i filters
- Export chart data và dashboard summary

---

## 1. API Endpoints Summary

| Endpoint | Method | Auth | Mô t? |
|----------|--------|------|-------|
| `/api/admin/export/users` | GET | ? Admin | Export users data |
| `/api/admin/export/posts` | GET | ? Admin | Export posts data |
| `/api/admin/export/reports` | GET | ? Admin | Export reports data |
| `/api/admin/export/audit-logs` | GET | ? Admin | Export audit logs |
| `/api/admin/export/charts/growth` | GET | ? Admin | Export growth chart data |
| `/api/admin/export/dashboard-summary` | GET | ? Admin | Export dashboard summary |

---

## 2. Export Formats

| Format | Content-Type | Extension | Notes |
|--------|--------------|-----------|-------|
| CSV | `text/csv; charset=utf-8` | `.csv` | UTF-8 with BOM for Excel compatibility |
| JSON | `application/json` | `.json` | Pretty-printed JSON |
| Excel | `application/vnd.ms-excel` | `.xml` | SpreadsheetML format (opens in Excel) |

---

## 3. Export Users

### `GET /api/admin/export/users`

**Mô t?**: Export danh sách users v?i các filter options

**Query Parameters**:

| Parameter | Type | Required | Mô t? |
|-----------|------|----------|-------|
| `search` | string | ? | Search by username or display name |
| `role` | string | ? | Filter by role: `User`, `Admin` |
| `status` | string | ? | Filter by status: `active`, `banned` |
| `fromDate` | date | ? | Filter users created after this date |
| `toDate` | date | ? | Filter users created before this date |
| `format` | string | ? | Export format: `csv`, `json`, `excel` (default: csv) |

**Request Example**:
```
GET /api/admin/export/users?status=banned&format=csv
```

**Response**: File download with appropriate Content-Type

**CSV Output Example**:
```csv
ID,Username,DisplayName,Email,Role,IsBanned,BannedUntil,CreatedAt,LastActiveAt,PostsCount,FollowersCount,FollowingCount
550e8400-e29b-41d4-a716-446655440000,john_doe,John Doe,,User,No,,2024-01-15 10:30:00,2024-01-20 15:45:00,25,150,80
```

---

## 4. Export Posts

### `GET /api/admin/export/posts`

**Mô t?**: Export danh sách posts

**Query Parameters**:

| Parameter | Type | Required | Mô t? |
|-----------|------|----------|-------|
| `search` | string | ? | Search in caption |
| `status` | string | ? | Filter: `active`, `deleted` |
| `fromDate` | date | ? | Filter posts created after this date |
| `toDate` | date | ? | Filter posts created before this date |
| `format` | string | ? | Export format (default: csv) |

**CSV Columns**:
- ID, AuthorID, AuthorUsername, Caption, Privacy
- CreatedAt, IsDeleted, ReactionsCount, CommentsCount, MediaCount

---

## 5. Export Reports

### `GET /api/admin/export/reports`

**Mô t?**: Export danh sách reports

**Query Parameters**:

| Parameter | Type | Required | Mô t? |
|-----------|------|----------|-------|
| `status` | string | ? | Filter: `Pending`, `Resolved`, `Rejected` |
| `targetType` | string | ? | Filter: `Post`, `Comment`, `User` |
| `fromDate` | date | ? | Filter reports created after this date |
| `toDate` | date | ? | Filter reports created before this date |
| `format` | string | ? | Export format (default: csv) |

**CSV Columns**:
- ID, ReporterID, ReporterUsername, TargetType, TargetID
- Reason, Status, CreatedAt, ActedAt

---

## 6. Export Audit Logs

### `GET /api/admin/export/audit-logs`

**Mô t?**: Export audit logs

**Query Parameters**:

| Parameter | Type | Required | Mô t? |
|-----------|------|----------|-------|
| `actionType` | string | ? | Filter by action type |
| `adminId` | guid | ? | Filter by admin who performed action |
| `fromDate` | date | ? | Filter logs after this date |
| `toDate` | date | ? | Filter logs before this date |
| `format` | string | ? | Export format (default: csv) |

**CSV Columns**:
- ID, AdminID, AdminUsername, ActionType, TargetProfileID
- TargetUsername, TargetEntityType, TargetEntityID, Notes, CreatedAt

---

## 7. Export Charts Data

### `GET /api/admin/export/charts/growth`

**Mô t?**: Export growth chart data

**Query Parameters**:

| Parameter | Type | Required | Mô t? |
|-----------|------|----------|-------|
| `fromDate` | date | ? | Start date |
| `toDate` | date | ? | End date |
| `interval` | string | ? | `day`, `week`, `month` |
| `format` | string | ? | Export format |

**CSV Output**:
```csv
Date,Users,Posts,Reports
2024-01-01,5,12,2
2024-01-02,8,15,1
2024-01-03,3,10,3
```

---

## 8. Export Dashboard Summary

### `GET /api/admin/export/dashboard-summary`

**Mô t?**: Export dashboard summary (JSON only)

**Response**: JSON file containing:
- GeneratedAt timestamp
- Dashboard stats
- User distribution
- Report distribution

---

## 9. Limits & Performance

| Constraint | Value | Notes |
|------------|-------|-------|
| Max rows per export | 10,000 | Prevent memory issues |
| Request timeout | 60s | For large exports |
| Rate limit | 10/minute | Prevent abuse |

---

## 10. TypeScript Interfaces

```typescript
// ===== Export Formats =====

type ExportFormat = 'csv' | 'json' | 'excel';

// ===== Request Types =====

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

// ===== Export Data Types =====

interface ExportUserDto {
  id: string;
  username: string | null;
  displayName: string | null;
  email: string | null;
  role: string;
  isBanned: boolean;
  bannedUntil: string | null;
  createdAt: string;
  lastActiveAt: string | null;
  postsCount: number;
  followersCount: number;
  followingCount: number;
}

interface ExportPostDto {
  id: string;
  authorId: string;
  authorUsername: string | null;
  caption: string | null;
  privacy: string;
  createdAt: string;
  isDeleted: boolean;
  reactionsCount: number;
  commentsCount: number;
  mediaCount: number;
}

interface ExportReportDto {
  id: string;
  reporterId: string;
  reporterUsername: string | null;
  targetType: string;
  targetId: string;
  reason: string;
  status: string;
  createdAt: string;
  actedAt: string | null;
}

interface ExportAuditLogDto {
  id: string;
  adminId: string;
  adminUsername: string | null;
  actionType: string;
  targetProfileId: string | null;
  targetUsername: string | null;
  targetEntityType: string | null;
  targetEntityId: string | null;
  notes: string | null;
  createdAt: string;
}
```

---

## 11. API Service Implementation

```typescript
// lib/api/export.ts

import { apiClient } from './client';

export const exportApi = {
  // Helper to trigger file download
  downloadFile: async (url: string, params: Record<string, any>) => {
    const response = await apiClient.get(url, {
      params,
      responseType: 'blob',
    });

    // Extract filename from Content-Disposition header
    const contentDisposition = response.headers['content-disposition'];
    const filenameMatch = contentDisposition?.match(/filename="?(.+)"?/);
    const filename = filenameMatch?.[1] || 'export.csv';

    // Trigger download
    const blob = new Blob([response.data]);
    const downloadUrl = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = downloadUrl;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(downloadUrl);
  },

  // Export endpoints
  exportUsers: (params: ExportUsersRequest) =>
    exportApi.downloadFile('/api/admin/export/users', params),

  exportPosts: (params: ExportPostsRequest) =>
    exportApi.downloadFile('/api/admin/export/posts', params),

  exportReports: (params: ExportReportsRequest) =>
    exportApi.downloadFile('/api/admin/export/reports', params),

  exportAuditLogs: (params: ExportAuditLogsRequest) =>
    exportApi.downloadFile('/api/admin/export/audit-logs', params),

  exportGrowthChart: (params: { fromDate?: string; toDate?: string; interval?: string; format?: string }) =>
    exportApi.downloadFile('/api/admin/export/charts/growth', params),

  exportDashboardSummary: () =>
    exportApi.downloadFile('/api/admin/export/dashboard-summary', { format: 'json' }),
};
```

---

## 12. React Hooks Implementation

```typescript
// hooks/useExport.ts

import { useState } from 'react';
import { exportApi } from '@/lib/api/export';
import { toast } from 'sonner';

interface UseExportOptions {
  onSuccess?: () => void;
  onError?: (error: Error) => void;
}

export function useExport(options: UseExportOptions = {}) {
  const [isExporting, setIsExporting] = useState(false);

  const handleExport = async (
    exportFn: () => Promise<void>,
    entityName: string
  ) => {
    setIsExporting(true);
    try {
      await exportFn();
      toast.success(`${entityName} exported successfully`);
      options.onSuccess?.();
    } catch (error) {
      toast.error(`Failed to export ${entityName}`);
      options.onError?.(error as Error);
    } finally {
      setIsExporting(false);
    }
  };

  return {
    isExporting,
    exportUsers: (params: ExportUsersRequest) =>
      handleExport(() => exportApi.exportUsers(params), 'Users'),
    exportPosts: (params: ExportPostsRequest) =>
      handleExport(() => exportApi.exportPosts(params), 'Posts'),
    exportReports: (params: ExportReportsRequest) =>
      handleExport(() => exportApi.exportReports(params), 'Reports'),
    exportAuditLogs: (params: ExportAuditLogsRequest) =>
      handleExport(() => exportApi.exportAuditLogs(params), 'Audit Logs'),
    exportGrowthChart: (params: any) =>
      handleExport(() => exportApi.exportGrowthChart(params), 'Growth Chart'),
    exportDashboardSummary: () =>
      handleExport(() => exportApi.exportDashboardSummary(), 'Dashboard Summary'),
  };
}
```

---

## 13. UI Components Specification

### 13.1 Export Button with Format Selector

```
???????????????????????????????????????????
?  ?? Export ?                            ?
???????????????????????????????????????????
?  ? CSV (Spreadsheet compatible)         ?
?  ? JSON (Raw data)                      ?
?  ? Excel (XML format)                   ?
???????????????????????????????????????????
```

### 13.2 Export Dialog with Filters

```
???????????????????????????????????????????????????????????????????
?                      Export Users                         [X]   ?
???????????????????????????????????????????????????????????????????
?                                                                  ?
?  Filters (optional):                                            ?
?                                                                  ?
?  Role:        ???????????????????                               ?
?               ? All            ??                               ?
?               ???????????????????                               ?
?                                                                  ?
?  Status:      ???????????????????                               ?
?               ? All            ??                               ?
?               ???????????????????                               ?
?                                                                  ?
?  Date Range:  ????????????  to  ????????????                   ?
?               ? From     ?      ? To       ?                    ?
?               ????????????      ????????????                    ?
?                                                                  ?
?  Format:      ? CSV  ? JSON  ? Excel                            ?
?                                                                  ?
?  ?? Maximum 10,000 records will be exported                     ?
?                                                                  ?
?                            [Cancel]  [?? Export]                ?
???????????????????????????????????????????????????????????????????
```

### 13.3 Component Structure

```
admin/
??? components/
?   ??? ExportButton.tsx           # Dropdown button for export
?   ??? ExportDialog.tsx           # Dialog with filters
?   ??? FormatSelector.tsx         # Radio group for format
?   ??? ExportProgress.tsx         # Loading indicator
??? hooks/
?   ??? useExport.ts               # Export mutation hook
```

---

## 14. Best Practices

### 14.1 Show Loading State

```tsx
<Button disabled={isExporting}>
  {isExporting ? (
    <>
      <Spinner className="mr-2" />
      Exporting...
    </>
  ) : (
    <>
      <Download className="mr-2" />
      Export
    </>
  )}
</Button>
```

### 14.2 Apply Current Filters

```tsx
// Export with same filters as current view
const handleExport = () => {
  exportUsers({
    search: currentSearch,
    role: currentRoleFilter,
    status: currentStatusFilter,
    fromDate: dateRange.from,
    toDate: dateRange.to,
    format: selectedFormat,
  });
};
```

### 14.3 Confirm Large Exports

```tsx
if (estimatedRows > 5000) {
  const confirmed = await confirm(
    `You are about to export approximately ${estimatedRows} records. This may take a while. Continue?`
  );
  if (!confirmed) return;
}
```

---

## 15. Audit Trail

All exports are logged with:
- Admin who performed the export
- Data type exported (Users, Posts, Reports, etc.)
- Number of records exported
- Format used

**Audit Log Entry Example**:
```
ActionType: ExportData
Notes: "Exported 1,234 Users records in CSV format"
```

---

## 16. Error Handling

| Error | Status | Message |
|-------|--------|---------|
| Unauthorized | 401 | Redirect to login |
| Forbidden | 403 | "You don't have permission to export data" |
| Timeout | 408 | "Export timed out. Try with narrower filters." |
| Server Error | 500 | "Export failed. Please try again." |

---

## 17. Localization Notes

| Key | English | Vietnamese |
|-----|---------|------------|
| `export.title` | Export | Xu?t d? li?u |
| `export.format` | Format | ??nh d?ng |
| `export.csv` | CSV (Spreadsheet) | CSV (B?ng tính) |
| `export.json` | JSON (Raw data) | JSON (D? li?u thô) |
| `export.excel` | Excel | Excel |
| `export.downloading` | Exporting... | ?ang xu?t... |
| `export.success` | Export completed | Xu?t thành công |
| `export.failed` | Export failed | Xu?t th?t b?i |
| `export.limit_warning` | Max 10,000 records | T?i ?a 10.000 b?n ghi |

---

## 18. Testing Checklist

### Unit Tests
- [ ] CSV generation with special characters
- [ ] JSON serialization with null values
- [ ] Excel XML generation
- [ ] Date formatting in exports

### Integration Tests
- [ ] Export with filters returns correct data
- [ ] Large export (10,000 rows) completes successfully
- [ ] Export is logged in audit trail

### E2E Tests
- [ ] Click export ? file downloads
- [ ] Exported file opens correctly in Excel
- [ ] JSON file is valid

---

## Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2024-01-15 | Initial Phase 5 implementation |
