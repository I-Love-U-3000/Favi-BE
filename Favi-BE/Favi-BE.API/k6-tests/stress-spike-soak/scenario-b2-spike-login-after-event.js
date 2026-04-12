// SPIKE-LOGIN-AFTER_EVENT
// Login burst scenario that simulates a post-event surge of authentication requests.

import { check, sleep } from 'k6';
import { login, seedUsers } from './common.js';

export const options = {
  scenarios: {
    spike_login_event: {
      executor: 'ramping-vus',
      stages: [
        { duration: '20s', target: 10 },
        { duration: '10s', target: 220 },
        { duration: '20s', target: 220 },
        { duration: '20s', target: 20 },
        { duration: '20s', target: 0 },
      ],
      gracefulRampDown: '10s',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<5000'],
    http_req_failed: ['rate<0.35'],
  },
};

export default function () {
  const user = seedUsers[(__VU + __ITER) % seedUsers.length];
  const { res, token } = login(user.username, user.password || '123456');

  check(res, {
    'spike login status 200': (r) => r.status === 200,
    'spike login token exists': () => !!token,
  });

  sleep(0.3);
}
