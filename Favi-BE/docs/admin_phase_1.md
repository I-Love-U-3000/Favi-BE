# ?? Phase 1: System Health Monitoring - Frontend Specification

## Overview

Phase 1 cung c?p các API endpoints ?? frontend hi?n th? tr?ng thái s?c kh?e h? th?ng và metrics cho Admin Portal.

**M?c ?ích**: Cho phép Admin giám sát real-time tr?ng thái c?a h? th?ng bao g?m:
- Database connectivity
- Memory usage
- CPU usage
- Process information
- Garbage Collection statistics

---

## 1. API Endpoints Summary

| Endpoint | Method | Auth | Mô t? |
|----------|--------|------|-------|
| `/health` | GET | ? Public | Basic health check |
| `/health/live` | GET | ? Public | Liveness probe |
| `/health/ready` | GET | ? Public | Readiness probe (checks DB) |
| `/health/details` | GET | ? Public | Detailed health v?i UI format |
| `/api/admin/health` | GET | ? Admin | Health status t?t c? checks |
| `/api/admin/health/metrics` | GET | ? Admin | System metrics |
| `/api/admin/health/detailed` | GET | ? Admin | Full health + metrics |

---

## 2. Public Health Endpoints (Không c?n Authentication)

### 2.1 `GET /health`

**Mô t?**: Basic health check - ki?m tra app có ?ang ch?y không

**Use case**: Load balancer health check, container orchestration

**Request**: Không có parameters

**Response**: `200 OK`
```json
{
  "status": "ok",
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

**L?u ý cho Frontend**:
- Endpoint này luôn tr? v? `200 OK` n?u app ?ang ch?y
- Không c?n hi?n th? cho user, ch? y?u dùng cho infrastructure

---

### 2.2 `GET /health/live`

**Mô t?**: Liveness probe - app có s?ng không

**Response**: `200 OK`
```json
{
  "status": "alive",
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

---

### 2.3 `GET /health/ready`

**Mô t?**: Readiness probe - app có s?n sàng nh?n traffic không (ki?m tra database)

**Response**: `200 OK` (n?u healthy)
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "database": {
      "data": {
        "ResponseTimeMs": 15,
        "Database": "PostgreSQL"
      },
      "description": "Database connection is healthy (15ms)",
      "duration": "00:00:00.0150000",
      "status": "Healthy",
      "tags": ["db", "postgresql", "ready"]
    }
  }
}
```

**Response**: `503 Service Unavailable` (n?u unhealthy)
```json
{
  "status": "Unhealthy",
  "totalDuration": "00:00:05.0000000",
  "entries": {
    "database": {
      "data": {
        "ResponseTimeMs": 5000,
        "Error": "Connection timeout"
      },
      "description": "Database health check failed",
      "duration": "00:00:05.0000000",
      "status": "Unhealthy",
      "tags": ["db", "postgresql", "ready"]
    }
  }
}
```

---

### 2.4 `GET /health/details`

**Mô t?**: Chi ti?t t?t c? health checks v?i format UI-friendly

**Response**: `200 OK`
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0500000",
  "entries": {
    "database": {
      "data": {
        "ResponseTimeMs": 15,
        "Database": "PostgreSQL"
      },
      "description": "Database connection is healthy (15ms)",
      "duration": "00:00:00.0150000",
      "status": "Healthy",
      "tags": ["db", "postgresql", "ready"]
    },
    "memory": {
      "data": {
        "AllocatedMB": 125.45,
        "WorkingSetMB": 180.32,
        "ThresholdMB": 1024,
        "DegradedThresholdMB": 512,
        "Gen0Collections": 45,
        "Gen1Collections": 12,
        "Gen2Collections": 3
      },
      "description": "Memory usage is normal: 125.45MB",
      "duration": "00:00:00.0010000",
      "status": "Healthy",
      "tags": ["system", "memory"]
    }
  }
}
```

---

## 3. Admin-Only Endpoints (Yêu c?u Admin Authentication)

### 3.1 `GET /api/admin/health`

**Mô t?**: Health status c?a t?t c? registered health checks

**Headers**:
```
Authorization: Bearer <admin_jwt_token>
```

**Response**: `200 OK`
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00.000Z",
  "totalDurationMs": 50.5,
  "entries": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Database connection is healthy (15ms)",
      "durationMs": 15.0,
      "data": {
        "ResponseTimeMs": 15,
        "Database": "PostgreSQL"
      },
      "exception": null,
      "tags": ["db", "postgresql", "ready"]
    },
    {
      "name": "memory",
      "status": "Healthy",
      "description": "Memory usage is normal: 125.45MB",
      "durationMs": 1.0,
      "data": {
        "AllocatedMB": 125.45,
        "WorkingSetMB": 180.32,
        "ThresholdMB": 1024,
        "DegradedThresholdMB": 512,
        "Gen0Collections": 45,
        "Gen1Collections": 12,
        "Gen2Collections": 3
      },
      "exception": null,
      "tags": ["system", "memory"]
    }
  ]
}
```

**Error Response**: `401 Unauthorized`
```json
{
  "code": "UNAUTHORIZED",
  "message": "Authentication required"
}
```

**Error Response**: `403 Forbidden`
```json
{
  "code": "FORBIDDEN",
  "message": "Admin access required"
}
```

---

### 3.2 `GET /api/admin/health/metrics`

**Mô t?**: System metrics chi ti?t (CPU, Memory, GC, Process info)

**Headers**:
```
Authorization: Bearer <admin_jwt_token>
```

**Response**: `200 OK`
```json
{
  "memory": {
    "workingSetMB": 180.32,
    "privateMemoryMB": 195.45,
    "gcMemoryMB": 125.45
  },
  "cpu": {
    "usagePercent": 12.5
  },
  "process": {
    "threadCount": 45,
    "handleCount": 320,
    "uptimeSeconds": 86400,
    "uptimeFormatted": "1d 0h 0m"
  },
  "garbageCollection": {
    "gen0Collections": 45,
    "gen1Collections": 12,
    "gen2Collections": 3
  },
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

---

### 3.3 `GET /api/admin/health/detailed`

**Mô t?**: Full health check k?t h?p v?i system metrics

**Headers**:
```
Authorization: Bearer <admin_jwt_token>
```

**Response**: `200 OK`
```json
{
  "overallStatus": "Healthy",
  "timestamp": "2024-01-15T10:30:00.000Z",
  "totalCheckDurationMs": 55.5,
  "metrics": {
    "memory": {
      "workingSetMB": 180.32,
      "privateMemoryMB": 195.45,
      "gcMemoryMB": 125.45
    },
    "cpu": {
      "usagePercent": 12.5
    },
    "process": {
      "threadCount": 45,
      "handleCount": 320,
      "uptimeSeconds": 86400,
      "uptimeFormatted": "1d 0h 0m"
    },
    "garbageCollection": {
      "gen0Collections": 45,
      "gen1Collections": 12,
      "gen2Collections": 3
    },
    "timestamp": "2024-01-15T10:30:00.000Z"
  },
  "services": [
    {
      "name": "memory",
      "status": "Healthy",
      "message": "Memory usage is normal: 125.45MB",
      "responseTimeMs": 1,
      "data": {
        "AllocatedMB": 125.45,
        "WorkingSetMB": 180.32
      }
    }
  ],
  "database": {
    "status": "Healthy",
    "message": "Database connection is healthy (15ms)",
    "responseTimeMs": 15
  }
}
```

---

## 4. TypeScript Interfaces

```typescript
// ===== Health Status Types =====

type HealthStatus = 'Healthy' | 'Degraded' | 'Unhealthy';

interface HealthCheckEntry {
  name: string;
  status: HealthStatus;
  description: string | null;
  durationMs: number;
  data: Record<string, unknown> | null;
  exception: string | null;
  tags: string[];
}

interface HealthStatusResponse {
  status: HealthStatus;
  timestamp: string; // ISO 8601 format
  totalDurationMs: number;
  entries: HealthCheckEntry[];
}

// ===== System Metrics Types =====

interface MemoryMetrics {
  workingSetMB: number;
  privateMemoryMB: number;
  gcMemoryMB: number;
}

interface CpuMetrics {
  usagePercent: number;
}

interface ProcessMetrics {
  threadCount: number;
  handleCount: number;
  uptimeSeconds: number;
  uptimeFormatted: string;
}

interface GCMetrics {
  gen0Collections: number;
  gen1Collections: number;
  gen2Collections: number;
}

interface SystemMetricsResponse {
  memory: MemoryMetrics;
  cpu: CpuMetrics;
  process: ProcessMetrics;
  garbageCollection: GCMetrics;
  timestamp: string;
}

// ===== Detailed Health Types =====

interface ServiceHealth {
  name: string;
  status: HealthStatus;
  message: string | null;
  responseTimeMs: number;
  data: Record<string, unknown> | null;
}

interface DatabaseHealth {
  status: HealthStatus;
  message: string | null;
  responseTimeMs: number;
}

interface DetailedHealthResponse {
  overallStatus: HealthStatus;
  timestamp: string;
  totalCheckDurationMs: number;
  metrics: SystemMetricsResponse;
  services: ServiceHealth[];
  database: DatabaseHealth;
}

// ===== Simple Health Types =====

interface SimpleHealthResponse {
  status: string;
  timestamp: string;
}
```

---

## 5. API Service Implementation (React/Next.js)

```typescript
// lib/api/health.ts

import { apiClient } from './client';

const HEALTH_ENDPOINTS = {
  basic: '/health',
  live: '/health/live',
  ready: '/health/ready',
  details: '/health/details',
  adminHealth: '/api/admin/health',
  adminMetrics: '/api/admin/health/metrics',
  adminDetailed: '/api/admin/health/detailed',
} as const;

// Public endpoints (no auth required)
export const healthApi = {
  /**
   * Basic health check
   */
  getBasicHealth: async (): Promise<SimpleHealthResponse> => {
    const response = await fetch(HEALTH_ENDPOINTS.basic);
    return response.json();
  },

  /**
   * Liveness probe
   */
  getLiveness: async (): Promise<SimpleHealthResponse> => {
    const response = await fetch(HEALTH_ENDPOINTS.live);
    return response.json();
  },

  /**
   * Readiness probe (checks database)
   */
  getReadiness: async (): Promise<HealthStatusResponse> => {
    const response = await fetch(HEALTH_ENDPOINTS.ready);
    return response.json();
  },

  /**
   * Detailed health check (public)
   */
  getDetails: async (): Promise<HealthStatusResponse> => {
    const response = await fetch(HEALTH_ENDPOINTS.details);
    return response.json();
  },
};

// Admin-only endpoints (requires authentication)
export const adminHealthApi = {
  /**
   * Get health status of all checks (Admin only)
   */
  getHealthStatus: async (): Promise<HealthStatusResponse> => {
    return apiClient.get(HEALTH_ENDPOINTS.adminHealth);
  },

  /**
   * Get system metrics (Admin only)
   */
  getMetrics: async (): Promise<SystemMetricsResponse> => {
    return apiClient.get(HEALTH_ENDPOINTS.adminMetrics);
  },

  /**
   * Get detailed health with metrics (Admin only)
   */
  getDetailedHealth: async (): Promise<DetailedHealthResponse> => {
    return apiClient.get(HEALTH_ENDPOINTS.adminDetailed);
  },
};
```

---

## 6. React Hooks Implementation

```typescript
// hooks/useSystemHealth.ts

import { useQuery } from '@tanstack/react-query';
import { adminHealthApi } from '@/lib/api/health';

const QUERY_KEYS = {
  healthStatus: ['admin', 'health', 'status'],
  metrics: ['admin', 'health', 'metrics'],
  detailed: ['admin', 'health', 'detailed'],
} as const;

/**
 * Hook ?? l?y health status
 * @param refetchInterval - Interval ?? auto-refresh (ms), default 30s
 */
export function useHealthStatus(refetchInterval = 30000) {
  return useQuery({
    queryKey: QUERY_KEYS.healthStatus,
    queryFn: adminHealthApi.getHealthStatus,
    refetchInterval,
    staleTime: 10000, // Consider data stale after 10s
  });
}

/**
 * Hook ?? l?y system metrics
 * @param refetchInterval - Interval ?? auto-refresh (ms), default 5s
 */
export function useSystemMetrics(refetchInterval = 5000) {
  return useQuery({
    queryKey: QUERY_KEYS.metrics,
    queryFn: adminHealthApi.getMetrics,
    refetchInterval,
    staleTime: 2000, // Metrics change frequently
  });
}

/**
 * Hook ?? l?y detailed health (health + metrics combined)
 * @param refetchInterval - Interval ?? auto-refresh (ms), default 15s
 */
export function useDetailedHealth(refetchInterval = 15000) {
  return useQuery({
    queryKey: QUERY_KEYS.detailed,
    queryFn: adminHealthApi.getDetailedHealth,
    refetchInterval,
    staleTime: 5000,
  });
}
```

---

## 7. UI Components Specification

### 7.1 Health Status Badge

```typescript
// components/admin/HealthStatusBadge.tsx

interface HealthStatusBadgeProps {
  status: HealthStatus;
  size?: 'sm' | 'md' | 'lg';
}

/**
 * Badge hi?n th? tr?ng thái health
 * 
 * Colors:
 * - Healthy: Green (#22C55E)
 * - Degraded: Yellow/Orange (#F59E0B)
 * - Unhealthy: Red (#EF4444)
 */
```

### 7.2 System Metrics Dashboard

```
???????????????????????????????????????????????????????????????
?                    System Health Monitor                      ?
???????????????????????????????????????????????????????????????
?                                                               ?
?  ????????????????  ????????????????  ????????????????        ?
?  ?   Overall    ?  ?   Database   ?  ?    Memory    ?        ?
?  ?   Status     ?  ?    Status    ?  ?    Status    ?        ?
?  ?  ?? Healthy  ?  ?  ?? Healthy  ?  ?  ?? Healthy  ?        ?
?  ?              ?  ?    15ms      ?  ?   125.45MB   ?        ?
?  ????????????????  ????????????????  ????????????????        ?
?                                                               ?
?  ??????????????????????????????????????????????????????????? ?
?  ?                    System Metrics                        ? ?
?  ??????????????????????????????????????????????????????????? ?
?  ?                                                          ? ?
?  ?  CPU Usage        Memory Usage       Uptime              ? ?
?  ?  ????????????     ??????????????     1d 2h 30m           ? ?
?  ?     12.5%            180MB / 1GB                         ? ?
?  ?                                                          ? ?
?  ?  Threads: 45      Handles: 320      GC Gen0: 45          ? ?
?  ?                                     GC Gen1: 12          ? ?
?  ?                                     GC Gen2: 3           ? ?
?  ??????????????????????????????????????????????????????????? ?
?                                                               ?
?  ??????????????????????????????????????????????????????????? ?
?  ?                   Health Checks                          ? ?
?  ??????????????????????????????????????????????????????????? ?
?  ?  ? database     Healthy    15ms    PostgreSQL           ? ?
?  ?  ? memory       Healthy     1ms    125.45MB used        ? ?
?  ??????????????????????????????????????????????????????????? ?
?                                                               ?
?  Last updated: 10:30:00 AM          [?? Refresh]             ?
???????????????????????????????????????????????????????????????
```

### 7.3 Component Structure

```
admin/
??? health/
?   ??? SystemHealthPage.tsx        # Main page component
?   ??? HealthStatusBadge.tsx       # Status indicator badge
?   ??? HealthCheckCard.tsx         # Individual health check display
?   ??? SystemMetricsPanel.tsx      # Metrics display panel
?   ??? MemoryUsageChart.tsx        # Memory usage visualization
?   ??? CpuUsageGauge.tsx           # CPU usage gauge
?   ??? UptimeDisplay.tsx           # Uptime formatted display
```

---

## 8. Recommended Polling Intervals

| Data Type | Recommended Interval | Lý do |
|-----------|---------------------|-------|
| System Metrics | 5 seconds | CPU/Memory thay ??i nhanh |
| Health Status | 30 seconds | Database check có th? ch?m |
| Detailed Health | 15 seconds | Balance gi?a freshness và load |

---

## 9. Error Handling

### 9.1 Network Errors

```typescript
// Khi API không th? k?t n?i
{
  isError: true,
  error: {
    message: "Network Error",
    code: "ERR_NETWORK"
  }
}
```

**UI Behavior**: Hi?n th? warning banner, gi? data c?, retry v?i exponential backoff

### 9.2 Authentication Errors

```typescript
// 401 Unauthorized
{
  code: "UNAUTHORIZED",
  message: "Authentication required"
}

// 403 Forbidden  
{
  code: "FORBIDDEN",
  message: "Admin access required"
}
```

**UI Behavior**: Redirect v? login page ho?c hi?n th? access denied message

### 9.3 Server Errors

```typescript
// 500 Internal Server Error
{
  code: "INTERNAL_ERROR",
  message: "An unexpected error occurred"
}
```

**UI Behavior**: Hi?n th? error message, cho phép retry

---

## 10. Testing Checklist

### 10.1 Unit Tests
- [ ] HealthStatusBadge renders correct colors
- [ ] Metrics display formats numbers correctly
- [ ] Uptime formats correctly (seconds ? "Xd Xh Xm")

### 10.2 Integration Tests
- [ ] API calls with valid admin token succeed
- [ ] API calls without token return 401
- [ ] API calls with non-admin token return 403

### 10.3 E2E Tests
- [ ] Health dashboard loads correctly
- [ ] Auto-refresh updates data
- [ ] Error states display correctly

---

## 11. Performance Considerations

1. **Debounce manual refresh**: Prevent spam clicking refresh button
2. **Stale-while-revalidate**: Show cached data while fetching new data
3. **Error retry**: Use exponential backoff (1s, 2s, 4s, 8s, max 30s)
4. **Pause polling**: Stop polling when tab is not visible (use `visibilitychange` event)

---

## 12. Accessibility (A11y)

1. **Color contrast**: Ensure status colors meet WCAG AA standards
2. **Screen readers**: Add aria-labels cho status badges
3. **Keyboard navigation**: All interactive elements ph?i focusable
4. **Status announcements**: Use `aria-live` regions cho status updates

---

## 13. Localization Notes

| Key | English | Vietnamese |
|-----|---------|------------|
| `health.status.healthy` | Healthy | Bình th??ng |
| `health.status.degraded` | Degraded | Suy gi?m |
| `health.status.unhealthy` | Unhealthy | L?i |
| `health.database` | Database | C? s? d? li?u |
| `health.memory` | Memory | B? nh? |
| `health.uptime` | Uptime | Th?i gian ho?t ??ng |

---

## 14. Security Notes

1. **Admin endpoints require `RequireAdmin` policy**
2. **JWT token ph?i có claim `account_role: admin`**
3. **Public health endpoints không expose sensitive information**
4. **Không log sensitive data trong health check responses**

---

## Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2024-01-15 | Initial Phase 1 implementation |
