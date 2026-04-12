/**
 * LOAD-PROFILE_SCROLL-STEADY
 * User vào profile influencer và scroll nhiều page.
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, seedUsers } from './common.js';

const profileIds = seedUsers.slice(0, 100).map((u) => u.profile_id);

export const options = {
  scenarios: {
    profile_scroll: {
      executor: 'constant-vus',
      vus: 20,
      duration: '3m',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<2500', 'p(99)<4500'],
    http_req_failed: ['rate<0.15'],
  },
};

export default function () {
  const profileId = profileIds[(__VU + __ITER) % profileIds.length];
  const page = (__ITER % 10) + 1;

  const res = http.get(`${BASE_URL}/api/posts/profile/${profileId}?page=${page}&pageSize=20`);

  check(res, {
    'profile scroll status is 200': (r) => r.status === 200,
    'profile scroll has items': (r) => Array.isArray(r.json('items')),
  });

  sleep(1);
}
