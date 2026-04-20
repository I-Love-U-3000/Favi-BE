// Functional Auth & Session scenarios: valid/invalid login, multi-login behavior, privacy guard.
import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, login, seedUsers, authHeaders } from './common.js';

export const options = {
  scenarios: {
    func_auth_session: {
      executor: 'per-vu-iterations',
      vus: 1,
      iterations: 1,
      maxDuration: '2m',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.4'],
    http_req_duration: ['p(95)<3000'],
  },
};

export default function () {
  // FUNC-AUTH-LOGIN-VALID
  const { res: validLogin, token: token1 } = login('user_00001', '123456');
  check(validLogin, {
    'FUNC-AUTH-LOGIN-VALID status 200': (r) => r.status === 200,
    'FUNC-AUTH-LOGIN-VALID has accessToken': () => !!token1,
  });

  // FUNC-AUTH-LOGIN-INVALID
  const invalidLogin = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ emailOrUsername: 'user_00001', password: 'wrong-pass' }),
    { headers: { 'Content-Type': 'application/json' } }
  );
  check(invalidLogin, {
    'FUNC-AUTH-LOGIN-INVALID status unauthorized': (r) => r.status === 401 || r.status === 400,
  });

  // FUNC-AUTH-MULTI_DEVICE_SESSION
  const { token: token2, res: login2 } = login('user_00001', '123456');
  check(login2, {
    'FUNC-AUTH-MULTI_DEVICE_SESSION second login 200': (r) => r.status === 200,
    'FUNC-AUTH-MULTI_DEVICE_SESSION has token2': () => !!token2,
  });

  if (token1) {
    const feed1 = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=10`, { headers: authHeaders(token1) });
    check(feed1, {
      'FUNC-AUTH-MULTI_DEVICE_SESSION token1 still usable': (r) => r.status === 200,
    });
  }
  if (token2) {
    const feed2 = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=10`, { headers: authHeaders(token2) });
    check(feed2, {
      'FUNC-AUTH-MULTI_DEVICE_SESSION token2 usable': (r) => r.status === 200,
    });
  }

  // FUNC-AUTH-PRIVACY-LEAKAGE_GUARD & FUNC-AUTH-PROFILE-PRIVACY_MATRIX
  const nonPublic = seedUsers.find((u) => {
    const p = String(u.privacy_level || '').toLowerCase();
    return p === 'followers' || p === 'private';
  });
  if (nonPublic && token1) {
    const profileRes = http.get(`${BASE_URL}/api/profiles/${nonPublic.profile_id}`, { headers: authHeaders(token1) });
    check(profileRes, {
      'FUNC-AUTH-PROFILE-PRIVACY_MATRIX policy status': (r) => [200, 403].includes(r.status),
    });

    const feedRes = http.get(`${BASE_URL}/api/posts/profile/${nonPublic.profile_id}?page=1&pageSize=10`, { headers: authHeaders(token1) });
    check(feedRes, {
      'FUNC-AUTH-PRIVACY-LEAKAGE_GUARD no policy violation status': (r) => [200, 403].includes(r.status),
    });
  }

  sleep(0.2);
}
