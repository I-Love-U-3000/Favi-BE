// STRESS-REACTION-HOTSPOT-MASS_USERS
// Hotspot scenario where many users react/comment against the same post to expose contention.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, seedPosts, pickToken, authHeaders } from './common.js';

const hotPostId = seedPosts[0]?.post_id;

export const options = {
  scenarios: {
    stress_reaction_hotspot: {
      executor: 'constant-vus',
      vus: 140,
      duration: '3m',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<5000'],
    http_req_failed: ['rate<0.45'],
  },
};

export default function () {
  const headers = authHeaders(pickToken());

  const reactRes = http.post(`${BASE_URL}/api/posts/${hotPostId}/reactions?type=Like`, null, { headers });
  check(reactRes, {
    'hotspot reaction accepted': (r) => r.status === 200 || r.status === 409,
  });

  if (__ITER % 3 === 0) {
    const cmtRes = http.post(
      `${BASE_URL}/api/comments`,
      JSON.stringify({ postId: hotPostId, content: `stress-hotspot-${__VU}-${__ITER}` }),
      { headers }
    );
    check(cmtRes, {
      'hotspot comment accepted': (r) => r.status === 200 || r.status === 429,
    });
  }

  sleep(0.15);
}
