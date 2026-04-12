// SPIKE-FEED-TRAFFIC-SUDDEN_JUMP
// Sudden feed traffic spike to verify burst handling and recovery.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, pickToken, authHeaders } from './common.js';

export const options = {
  scenarios: {
    spike_feed_jump: {
      executor: 'ramping-vus',
      stages: [
        { duration: '20s', target: 20 },
        { duration: '15s', target: 180 },
        { duration: '30s', target: 180 },
        { duration: '20s', target: 20 },
        { duration: '20s', target: 0 },
      ],
      gracefulRampDown: '10s',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<6000'],
    http_req_failed: ['rate<0.4'],
  },
};

export default function () {
  const res = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, {
    headers: authHeaders(pickToken()),
  });

  check(res, {
    'spike feed status 200': (r) => r.status === 200,
  });

  sleep(0.2);
}
