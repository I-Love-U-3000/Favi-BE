/**
 * LOAD-AUTH-LOGIN-BULK_USERS
 * Luồng: nhiều user login đồng thời, đo bottleneck auth.
 */

import { check } from 'k6';
import { BASE_URL, seedUsers, login } from './common.js';

export const options = {
  scenarios: {
    login_bulk: {
      executor: 'ramping-vus',
      stages: [
        { duration: '30s', target: 20 },
        { duration: '1m', target: 40 },
        { duration: '30s', target: 0 },
      ],
      gracefulRampDown: '10s',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<2000', 'p(99)<3500'],
    http_req_failed: ['rate<0.2'],
  },
};

export default function () {
  const idx = (__VU + __ITER) % seedUsers.length;
  const user = seedUsers[idx];

  const { res, token } = login(user.username, user.password || '123456');

  check(res, {
    'login status is 200': (r) => r.status === 200,
    'login has accessToken': () => token !== null,
  });
}
