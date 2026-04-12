// SOAK-FEED-REACTION-MIX-LONG_RUN
// Long-running mixed workload to detect memory leaks, latency drift, and connection leaks.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, pickToken, pickPostId, authHeaders } from './common.js';

const soakDuration = __ENV.SOAK_DURATION || '30m';

export const options = {
  scenarios: {
    soak_feed_reaction_mix: {
      executor: 'constant-vus',
      vus: 20,
      duration: soakDuration,
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<4000'],
    http_req_failed: ['rate<0.25'],
  },
};

export default function () {
  const headers = authHeaders(pickToken());
  const postId = pickPostId();

  const readRes = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, { headers });
  check(readRes, { 'soak feed 200': (r) => r.status === 200 });

  if (__ITER % 5 === 0) {
    const reactRes = http.post(`${BASE_URL}/api/posts/${postId}/reactions?type=Like`, null, { headers });
    check(reactRes, { 'soak reaction accepted': (r) => r.status === 200 || r.status === 409 });
  }

  sleep(1.5);
}
