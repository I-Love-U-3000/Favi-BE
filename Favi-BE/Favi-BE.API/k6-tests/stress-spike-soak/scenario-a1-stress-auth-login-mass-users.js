// STRESS-AUTH-LOGIN-MASS_USERS
// Burst login scenario to find auth bottlenecks under high concurrent load.

import { check } from 'k6';
import { login, seedUsers } from './common.js';

export const options = {
  scenarios: {
    stress_login_mass: {
      executor: 'ramping-vus',
      stages: [
        { duration: '20s', target: 50 },
        { duration: '40s', target: 120 },
        { duration: '30s', target: 160 },
        { duration: '20s', target: 0 },
      ],
      gracefulRampDown: '10s',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<4000'],
    http_req_failed: ['rate<0.35'],
  },
};

export default function () {
  const user = seedUsers[(__VU + __ITER) % seedUsers.length];
  const { res, token } = login(user.username, user.password || '123456');

  check(res, {
    'login status 200': (r) => r.status === 200,
    'login token exists': () => !!token,
  });
}
