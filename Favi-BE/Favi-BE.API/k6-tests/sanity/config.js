/**
 * SANITY Test Suite Configuration
 * 
 * Cấu hình chung cho tất cả các scenario sanity test
 * 
 * Mục tiêu:
 * - Kiểm tra hệ thống còn sống sau thay đổi nhỏ
 * - Đảm bảo các thao tác cơ bản ổn định
 * - Không race condition ở mức single user
 * - Dữ liệu nhất quán giữa các endpoint
 */

export const SANITY_CONFIG = {
  // Base Configuration
  BASE_URL: __ENV.BASE_URL || 'https://localhost:7138',
  SEED_OUTPUT_DIR: __ENV.SEED_OUTPUT_DIR || '../../seed-output',

  // Common Headers
  getAuthHeaders: (token) => ({
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json',
  }),

  // Threshold Configuration
  thresholds: {
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
    http_req_failed: ['rate<0.1'],
  },

  // Stages for single user
  stages: [
    { duration: '10s', target: 1 },
  ],

  // Test Data Configuration
  testDefaults: {
    loopCount: 10,
    refreshCount: 50,
    sleepDuration: 0.1,
    maxRetries: 3,
  },

  // KPI Thresholds
  kpis: {
    errorRateThreshold: 0.1, // 10% error rate
    latencyP95: 500, // 500ms
    latencyP99: 1000, // 1000ms
  },

  // Response validation helpers
  validators: {
    hasData: (response) => response !== null && response !== undefined,
    isArray: (value) => Array.isArray(value),
    hasId: (obj) => obj.id !== undefined || obj.post_id !== undefined || obj.profile_id !== undefined,
    hasTimestamp: (obj) => obj.created_at !== undefined || obj.timestamp !== undefined,
  },

  // Common API endpoints
  endpoints: {
    auth: {
      login: '/api/v1/auth/login',
      logout: '/api/v1/auth/logout',
      refresh: '/api/v1/auth/refresh',
    },
    posts: {
      list: '/api/v1/feeds',
      detail: (postId) => `/api/v1/posts/${postId}`,
      create: '/api/v1/posts',
      delete: (postId) => `/api/v1/posts/${postId}`,
    },
    reactions: {
      create: '/api/v1/reactions',
      delete: (reactionId) => `/api/v1/reactions/${reactionId}`,
      list: (postId) => `/api/v1/posts/${postId}/reactions`,
    },
    profile: {
      me: '/api/v1/profiles/me',
      detail: (profileId) => `/api/v1/profiles/${profileId}`,
    },
    search: {
      semantic: '/api/v1/search/semantic',
      keyword: '/api/v1/search/keyword',
    },
    share: {
      create: '/api/v1/reposts',
      delete: (repostId) => `/api/v1/reposts/${repostId}`,
      list: (profileId) => `/api/v1/profiles/${profileId}/reposts`,
    },
  },
};

/**
 * Utility function to load CSV data
 * @param {string} filePath - Path to CSV file
 * @returns {Array} Parsed CSV data
 */
export function loadCSVData(filePath) {
  // This would be implemented with the parse function from papaparse
  // Example usage in individual test files
  return [];
}

/**
 * Utility function for validation checks
 * @param {Object} data - Data to validate
 * @param {string} type - Type of validation (response, array, etc.)
 * @returns {boolean}
 */
export function validateResponse(data, type = 'response') {
  switch (type) {
    case 'response':
      return data && data.status && data.body;
    case 'array':
      return Array.isArray(data);
    case 'object':
      return data && typeof data === 'object';
    default:
      return !!data;
  }
}

export default SANITY_CONFIG;
