/**
 * LOAD-SEARCH_RELATED-MODERATE
 * Semantic search + related ở mức vừa, tách khỏi baseline feed.
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, seedTokens, seedPosts, authHeaders } from './common.js';

export const options = {
  scenarios: {
    search_related_moderate: {
      executor: 'constant-vus',
      vus: 12,
      duration: '3m',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<3500', 'p(99)<5000'],
    http_req_failed: ['rate<0.2'],
  },
};

export default function () {
  const token = seedTokens[(__VU + __ITER) % seedTokens.length].token;
  const postId = seedPosts[(__VU + __ITER) % seedPosts.length].post_id;
  const headers = authHeaders(token);

  const semanticRes = http.post(
    `${BASE_URL}/api/search/semantic`,
    JSON.stringify({ query: 'seed', page: 1, pageSize: 10, k: 50 }),
    { headers }
  );

  check(semanticRes, {
    'semantic status 200': (r) => r.status === 200,
    'semantic has posts array': (r) => Array.isArray(r.json('posts')),
  });

  const relatedRes = http.get(`${BASE_URL}/api/posts/${postId}/related?page=1&pageSize=10`, { headers });

  check(relatedRes, {
    'related status 200': (r) => r.status === 200,
    'related has items array': (r) => Array.isArray(r.json('items')),
  });

  sleep(1.2);
}
