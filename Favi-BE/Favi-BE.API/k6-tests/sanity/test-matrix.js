/**
 * Test Execution Matrix for SANITY Tests
 * 
 * Định nghĩa tất cả các scenario sanity test với metadata đầy đủ
 */

export const SANITY_TEST_MATRIX = {
  // Scenario 1: Like/Unlike Loop
  {
    id: 'SANITY-REACTION-LIKE_UNLIKE_LOOP-SINGLE_USER',
    name: 'Reaction Like/Unlike Loop',
    file: 'scenario-1-reaction-like-unlike-loop.js',
    module: 'reaction',
    behavior: 'like_unlike_loop',
    scale: 'single_user',
    duration: '10s',
    vus: 1,
    target: 'posts',
    
    // Pre-condition: seed data must have posts and users
    preconditions: [
      'posts.csv has data',
      'tokens.csv has valid tokens',
      'users.csv has at least 1 user',
    ],

    // Flow steps
    steps: [
      '1. Load token from tokens.csv',
      '2. Select random post from posts.csv',
      '3. Loop 10 times: like -> unlike -> like -> unlike',
      '4. After each action, GET post detail',
      '5. Verify count changes correctly',
      '6. Verify no duplicate reactions',
      '7. Final verification: state reflects last action',
    ],

    // KPI to monitor
    kpis: {
      'http_req_duration': {
        threshold: 'p(95) < 500',
        unit: 'ms',
      },
      'http_req_failed': {
        threshold: 'rate < 0.1',
        unit: '%',
      },
      'likes_count_consistency': {
        description: 'Like count must not go negative',
        assertion: 'count >= 0',
      },
      'reaction_duplicate_check': {
        description: 'No duplicate reactions for same user+post',
        assertion: 'unique reactions == 1 or 0',
      },
    },

    // Pass/Fail criteria
    passCriteria: [
      '✓ All POST /reactions returns 200/201',
      '✓ All DELETE /reactions returns 200/204',
      '✓ Reaction count never goes negative',
      '✓ No duplicate reactions for same (user, post)',
      '✓ Final state matches last action',
      '✓ Response time < 500ms (p95)',
    ],

    failSignals: [
      '✗ Count becomes negative',
      '✗ Duplicate reactions found',
      '✗ Inconsistent count between endpoints',
      '✗ 500 error responses',
      '✗ Response time > 1000ms consistently',
    ],
  },

  // Scenario 2: Login/Logout/Relogin
  {
    id: 'SANITY-AUTH-LOGIN_LOGOUT_RELOGIN-SINGLE_USER',
    name: 'Auth Login/Logout/Relogin',
    file: 'scenario-2-auth-login-logout-relogin.js',
    module: 'auth',
    behavior: 'login_logout_relogin',
    scale: 'single_user',
    duration: '10s',
    vus: 1,
    target: 'auth',

    preconditions: [
      'users.csv has valid credentials (username, password)',
      'Auth endpoint implemented',
    ],

    steps: [
      '1. POST /auth/login with valid credentials',
      '2. Get access_token from response',
      '3. GET /profiles/me with token (should work)',
      '4. POST /auth/logout with token',
      '5. Try GET /profiles/me with old token (should fail 401)',
      '6. POST /auth/login again with same credentials',
      '7. GET /profiles/me with new token (should work)',
      '8. Verify tokens are different',
    ],

    kpis: {
      'login_status': {
        threshold: '== 200',
        description: 'Login must return 200',
      },
      'token_revocation': {
        threshold: '== 401',
        description: 'Old token must return 401 after logout',
      },
      'new_token_validity': {
        threshold: '== 200',
        description: 'New token must work for protected endpoints',
      },
      'token_uniqueness': {
        threshold: 'token1 !== token2',
        description: 'Each login generates new token',
      },
    },

    passCriteria: [
      '✓ First login returns 200 with access_token',
      '✓ GET profile with first token returns 200',
      '✓ Logout returns 200/204',
      '✓ Old token returns 401 after logout',
      '✓ Relogin returns 200 with new access_token',
      '✓ GET profile with new token returns 200',
      '✓ Tokens are different',
    ],

    failSignals: [
      '✗ Login fails (not 200)',
      '✗ Old token still works after logout',
      '✗ New token invalid',
      '✗ Tokens are identical',
    ],
  },

  // Scenario 3: Feed Refresh
  {
    id: 'SANITY-FEED-REFRESH-REPEAT-SINGLE_USER',
    name: 'Feed Refresh Repeat',
    file: 'scenario-3-feed-refresh-repeat.js',
    module: 'feed',
    behavior: 'refresh_repeat',
    scale: 'single_user',
    duration: '30s',
    vus: 1,
    target: 'feed',

    preconditions: [
      'posts.csv has data',
      'tokens.csv has valid tokens',
      'Feed endpoint implemented',
    ],

    steps: [
      '1. Load token from tokens.csv',
      '2. Loop 50 times with 0.1s interval:',
      '   - GET /feeds?limit=20&offset=0',
      '   - Verify response has pagination and items',
      '   - Check response shape consistency',
      '3. Calculate success rate and error count',
      '4. Verify no error spike over time',
    ],

    kpis: {
      'http_req_duration': {
        threshold: 'p(95) < 1000',
        unit: 'ms',
      },
      'http_req_failed': {
        threshold: 'rate < 0.1',
        unit: '%',
      },
      'success_rate': {
        threshold: '> 95%',
        unit: '%',
      },
      'error_accumulation': {
        threshold: '<= 3 failures in 50 requests',
        description: 'Errors must not accumulate',
      },
    },

    passCriteria: [
      '✓ Success rate > 95% (47/50 success)',
      '✓ All successful responses return 200',
      '✓ Response has items array',
      '✓ Response has pagination (page, total)',
      '✓ Response shape consistent across calls',
      '✓ Response time p95 < 1000ms',
      '✓ No error spike over time',
    ],

    failSignals: [
      '✗ Success rate < 95%',
      '✗ Response shape changes',
      '✗ Timeout errors increase over time',
      '✗ Memory or connection leaks',
    ],
  },

  // Scenario 4: Search & Related Fallback
  {
    id: 'SANITY-SEARCH-RELATED-FALLBACK-SINGLE_USER',
    name: 'Search Related Fallback',
    file: 'scenario-4-search-related-fallback.js',
    module: 'search',
    behavior: 'search_related_fallback',
    scale: 'single_user',
    duration: '10s',
    vus: 1,
    target: 'search',

    preconditions: [
      'posts.csv has data',
      'tokens.csv has valid tokens',
      'Search endpoint implemented',
      'Related posts endpoint implemented',
    ],

    steps: [
      '1. Load token from tokens.csv',
      '2. Load random post from posts.csv',
      '3. GET /search/semantic?q="seed"&limit=10',
      '4. Check if results is array',
      '5. GET /posts/{postId}/related?limit=10',
      '6. Verify related has items (fallback)',
      '7. If semantic weak, verify fallback works',
      '8. Verify results have post_id, caption, created_at',
    ],

    kpis: {
      'semantic_search_status': {
        threshold: '== 200',
        description: 'Semantic search must return 200',
      },
      'related_posts_status': {
        threshold: '== 200',
        description: 'Related posts must return 200',
      },
      'fallback_availability': {
        threshold: 'items.length > 0',
        description: 'Related posts provides fallback',
      },
      'results_validity': {
        threshold: 'all have post_id, caption, created_at',
        description: 'Results have expected fields',
      },
    },

    passCriteria: [
      '✓ Semantic search returns 200',
      '✓ Related posts returns 200',
      '✓ Related posts always has items (fallback)',
      '✓ All results have post_id field',
      '✓ All results have caption field',
      '✓ All results have created_at field',
      '✓ Fallback works when semantic is weak',
    ],

    failSignals: [
      '✗ Search endpoints return error',
      '✗ Results missing required fields',
      '✗ Fallback not triggered when needed',
    ],
  },

  // Scenario 5: Share/Unshare
  {
    id: 'SANITY-SHARE-UNSHARE-SINGLE_USER',
    name: 'Share Unshare',
    file: 'scenario-5-share-unshare.js',
    module: 'share',
    behavior: 'share_unshare',
    scale: 'single_user',
    duration: '10s',
    vus: 1,
    target: 'reposts',

    preconditions: [
      'posts.csv has data',
      'tokens.csv has valid tokens',
      'Share/repost endpoint implemented',
    ],

    steps: [
      '1. Load token and random post',
      '2. GET /posts/{postId} -> get initial shares_count',
      '3. POST /reposts with post_id',
      '4. Verify shares_count increased by 1',
      '5. GET /profiles/{profileId}/reposts',
      '6. Verify post appears in user shares',
      '7. DELETE /reposts/{repostId}',
      '8. GET /posts/{postId} -> verify count restored',
      '9. GET /profiles/{profileId}/reposts -> verify removed',
    ],

    kpis: {
      'share_creation': {
        threshold: '200 or 201',
        description: 'Share creation status',
      },
      'share_deletion': {
        threshold: '200 or 204',
        description: 'Share deletion status',
      },
      'count_increment': {
        threshold: 'count == initial + 1',
        description: 'Count must increase by 1',
      },
      'count_restoration': {
        threshold: 'count == initial',
        description: 'Count must restore after unshare',
      },
      'list_consistency': {
        threshold: 'post in list after share, not after unshare',
        description: 'Share list consistency',
      },
    },

    passCriteria: [
      '✓ POST /reposts returns 200/201',
      '✓ Shares count increases by exactly 1',
      '✓ Post appears in user shares list',
      '✓ DELETE /reposts returns 200/204',
      '✓ Shares count restored to initial',
      '✓ Post removed from user shares list',
      '✓ State consistent across endpoints',
    ],

    failSignals: [
      '✗ Share creation fails',
      '✗ Count changes incorrectly',
      '✗ Share list inconsistent',
      '✗ Count not restored',
    ],
  },
};

/**
 * Helper function to get scenario by ID
 */
export function getScenarioById(id) {
  return SANITY_TEST_MATRIX.find(s => s.id === id);
}

/**
 * Helper function to get all scenarios
 */
export function getAllScenarios() {
  return SANITY_TEST_MATRIX;
}

/**
 * Helper function to get scenarios by module
 */
export function getScenariosByModule(module) {
  return SANITY_TEST_MATRIX.filter(s => s.module === module);
}

export default SANITY_TEST_MATRIX;
