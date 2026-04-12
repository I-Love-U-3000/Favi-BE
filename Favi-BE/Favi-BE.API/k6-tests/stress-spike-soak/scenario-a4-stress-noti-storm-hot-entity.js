// STRESS-NOTI-STORM-HOT_ENTITY
// Notification storm scenario driven by a hot post/creator to test notification fan-out behavior.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, seedPosts, pickToken, authHeaders } from './common.js';

const hotPostId = seedPosts[1]?.post_id || seedPosts[0]?.post_id;

export const options = {
  scenarios: {
    stress_noti_storm: {
      executor: 'constant-vus',
      vus: 100,
      duration: '3m',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<5000'],
    http_req_failed: ['rate<0.4'],
  },
};

export default function () {
  const headers = authHeaders(pickToken());

  const reactRes = http.post(`${BASE_URL}/api/posts/${hotPostId}/reactions?type=Like`, null, { headers });
  check(reactRes, {
    'noti storm reaction accepted': (r) => r.status === 200 || r.status === 409,
  });

  const shareRes = http.post(`${BASE_URL}/api/posts/${hotPostId}/share`, JSON.stringify({ caption: null }), { headers });
  check(shareRes, {
    'noti storm share accepted': (r) => r.status === 200 || r.status === 409,
  });

  sleep(0.25);
}
