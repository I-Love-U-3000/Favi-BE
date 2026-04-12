// STRESS-MEDIA-PIPELINE-PARTIAL_FAILURES
// Media pipeline resilience scenario that mixes invalid uploads with normal reads.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, pickToken, pickPostId, authHeaders } from './common.js';

export const options = {
  scenarios: {
    stress_media_partial_failures: {
      executor: 'constant-vus',
      vus: 70,
      duration: '3m',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<6000'],
    http_req_failed: ['rate<0.55'],
  },
};

export default function () {
  const token = pickToken();
  const headers = authHeaders(token);
  const postId = pickPostId();

  const invalidUploadRes = http.post(
    `${BASE_URL}/api/posts/${postId}/media`,
    JSON.stringify({ fake: 'invalid-payload' }),
    { headers }
  );

  check(invalidUploadRes, {
    'invalid media request handled': (r) => [400, 415, 422].includes(r.status),
  });

  const fallbackReadRes = http.get(`${BASE_URL}/api/posts/${postId}`, { headers });
  check(fallbackReadRes, {
    'post still readable after media failure': (r) => r.status === 200,
  });

  sleep(0.4);
}
