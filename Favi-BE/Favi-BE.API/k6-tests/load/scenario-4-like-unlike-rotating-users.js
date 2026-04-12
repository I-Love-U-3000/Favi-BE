/**
 * LOAD-LIKE_UNLIKE-ROTATING_USERS
 * Nhiều user thay phiên like/unlike trên tập post hot.
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, seedTokens, seedPosts, authHeaders } from './common.js';

const hotPostIds = seedPosts.slice(0, 20).map((p) => p.post_id);

export const options = {
  scenarios: {
    rotating_like_unlike: {
      executor: 'constant-vus',
      vus: 30,
      duration: '3m',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<2500', 'p(99)<4000'],
    http_req_failed: ['rate<0.15'],
  },
};

export default function () {
  const token = seedTokens[(__VU + __ITER) % seedTokens.length].token;
  const postId = hotPostIds[(__VU + __ITER) % hotPostIds.length];

  const toggleRes = http.post(`${BASE_URL}/api/posts/${postId}/reactions?type=Like`, null, {
    headers: authHeaders(token),
  });

  check(toggleRes, {
    'toggle like/unlike status 200': (r) => r.status === 200,
  });

  sleep(0.4);
}
