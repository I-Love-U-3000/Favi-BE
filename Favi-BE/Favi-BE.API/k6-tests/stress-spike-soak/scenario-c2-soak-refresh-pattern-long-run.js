// SOAK-REFRESH-PATTERN-LONG_RUN
// Extended feed refresh workload with stable periodic access to detect slow degradation.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, pickToken, authHeaders } from './common.js';
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';

const soakDuration = __ENV.SOAK_DURATION || '30m';

export const options = {
  scenarios: {
    soak_refresh_pattern: {
      executor: 'constant-vus',
      vus: 15,
      duration: soakDuration,
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<4000'],
    http_req_failed: ['rate<0.25'],
  },
};

export default function () {
  const res = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, {
    headers: authHeaders(pickToken()),
  });

  check(res, {
    'soak refresh status 200': (r) => r.status === 200,
  });

  sleep(randomIntBetween(2, 5));
}
