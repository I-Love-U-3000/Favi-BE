/**
 * LOAD-FEED-FANOUT_WORSTCASE-POWER_USERS
 * Luồng: nhóm power users (follow graph dày) đọc feed liên tục.
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, authHeaders, getPowerUserProfileIds, getTokenForProfile } from './common.js';

const powerProfiles = getPowerUserProfileIds(30);
const powerTokens = powerProfiles.map((id) => getTokenForProfile(id)).filter(Boolean);

export const options = {
  scenarios: {
    fanout_worstcase: {
      executor: 'constant-vus',
      vus: 15,
      duration: '3m',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<3000', 'p(99)<5000'],
    http_req_failed: ['rate<0.2'],
  },
};

export default function () {
  if (powerTokens.length === 0) {
    throw new Error('No power user tokens available from seed data');
  }

  const token = powerTokens[(__VU + __ITER) % powerTokens.length];

  const res = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=30`, {
    headers: authHeaders(token),
  });

  check(res, {
    'power feed status is 200': (r) => r.status === 200,
    'power feed has items': (r) => Array.isArray(r.json('items')),
  });

  sleep(1);
}
