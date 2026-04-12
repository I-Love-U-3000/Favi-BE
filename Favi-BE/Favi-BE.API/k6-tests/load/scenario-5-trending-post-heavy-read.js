/**
 * LOAD-TRENDING_POST-HEAVY_READ
 * Đọc liên tục 1 post trending + tương tác vừa phải.
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, seedTokens, seedPosts, authHeaders } from './common.js';

const trendingPostId = seedPosts[0]?.post_id;

export const options = {
  scenarios: {
    trending_heavy_read: {
      executor: 'constant-vus',
      vus: 35,
      duration: '3m',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<2500', 'p(99)<4500'],
    http_req_failed: ['rate<0.15'],
  },
};

export default function () {
  if (!trendingPostId) {
    throw new Error('No seeded post for trending scenario');
  }

  const token = seedTokens[(__VU + __ITER) % seedTokens.length].token;
  const headers = authHeaders(token);

  const detailRes = http.get(`${BASE_URL}/api/posts/${trendingPostId}`, { headers });
  check(detailRes, {
    'trending detail status 200': (r) => r.status === 200,
    'trending detail has reactions': (r) => r.json('reactions.total') !== undefined,
  });

  if (__ITER % 5 === 0) {
    const reactRes = http.post(`${BASE_URL}/api/posts/${trendingPostId}/reactions?type=Like`, null, { headers });
    check(reactRes, { 'trending optional reaction 200': (r) => r.status === 200 });
  }

  sleep(0.5);
}
