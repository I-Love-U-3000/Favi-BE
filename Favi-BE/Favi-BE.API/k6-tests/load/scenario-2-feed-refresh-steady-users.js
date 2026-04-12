/**
 * LOAD-FEED-REFRESH-STEADY_USERS
 * Luồng: nhiều user refresh feed đều đặn (2-5s).
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { seedTokens, BASE_URL, authHeaders } from './common.js';
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';

export const options = {
  scenarios: {
    feed_steady: {
      executor: 'constant-vus',
      vus: 25,
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

  const res = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, {
    headers: authHeaders(token),
  });

  check(res, {
    'feed status is 200': (r) => r.status === 200,
    'feed has items': (r) => Array.isArray(r.json('items')),
  });

  sleep(randomIntBetween(2, 5));
}
