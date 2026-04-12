// STRESS-FEED-REFRESH-CONTINUOUS_MASS
// Continuous feed refresh scenario with minimal pauses to stress read path and DB pressure.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, pickToken, authHeaders } from './common.js';

export const options = {
  scenarios: {
    stress_feed_continuous: {
      executor: 'constant-vus',
      vus: 120,
      duration: '3m',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<5000'],
    http_req_failed: ['rate<0.35'],
  },
};

export default function () {
  const res = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, {
    headers: authHeaders(pickToken()),
  });

  check(res, {
    'feed status 200': (r) => r.status === 200,
    'feed has items': (r) => Array.isArray(r.json('items')),
  });

  sleep(0.2);
}
