// SOAK-CONSISTENCY-LONG_RUN
// Long-running consistency check that mixes reads and writes to surface drift over time.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, pickToken, pickPostId, authHeaders } from './common.js';

const soakDuration = __ENV.SOAK_DURATION || '30m';

export const options = {
  scenarios: {
    soak_consistency: {
      executor: 'constant-vus',
      vus: 12,
      duration: soakDuration,
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<4500'],
    http_req_failed: ['rate<0.3'],
  },
};

export default function () {
  const headers = authHeaders(pickToken());
  const postId = pickPostId();

  const cmtRes = http.post(
    `${BASE_URL}/api/comments`,
    JSON.stringify({ postId, content: `soak-consistency-${__VU}-${__ITER}` }),
    { headers }
  );
  check(cmtRes, { 'soak create comment accepted': (r) => r.status === 200 || r.status === 429 });

  const reactRes = http.post(`${BASE_URL}/api/posts/${postId}/reactions?type=Like`, null, { headers });
  check(reactRes, { 'soak toggle reaction accepted': (r) => r.status === 200 || r.status === 409 });

  const readRes = http.get(`${BASE_URL}/api/posts/${postId}`, { headers });
  check(readRes, {
    'soak read-back status 200': (r) => r.status === 200,
    'soak read-back has reactions': (r) => r.json('reactions.total') !== undefined,
  });

  sleep(2);
}
