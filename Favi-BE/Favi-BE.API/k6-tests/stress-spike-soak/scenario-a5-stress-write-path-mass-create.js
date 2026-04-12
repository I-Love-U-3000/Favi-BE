// STRESS-WRITE-PATH-MASS_CREATE
// Mixed write-heavy scenario for post creation, comments, and reactions under concurrency.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, pickToken, pickPostId, authHeaders } from './common.js';

export const options = {
  scenarios: {
    stress_write_mass_create: {
      executor: 'constant-vus',
      vus: 90,
      duration: '3m',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<5000'],
    http_req_failed: ['rate<0.4'],
  },
};

export default function () {
  const token = pickToken();
  const headers = authHeaders(token);

  const createPostRes = http.post(
    `${BASE_URL}/api/posts`,
    { caption: `stress-write-post-${__VU}-${__ITER}`, privacyLevel: 'Public' },
    { headers: { Authorization: `Bearer ${token}` } }
  );
  check(createPostRes, {
    'mass create post accepted': (r) => r.status === 201 || r.status === 200 || r.status === 400,
  });

  const targetPostId = pickPostId();
  const cmtRes = http.post(
    `${BASE_URL}/api/comments`,
    JSON.stringify({ postId: targetPostId, content: `stress-write-comment-${__VU}-${__ITER}` }),
    { headers }
  );
  check(cmtRes, {
    'mass create comment accepted': (r) => r.status === 200 || r.status === 429,
  });

  const reactRes = http.post(`${BASE_URL}/api/posts/${targetPostId}/reactions?type=Like`, null, { headers });
  check(reactRes, {
    'mass create reaction accepted': (r) => r.status === 200 || r.status === 409,
  });

  sleep(0.35);
}
