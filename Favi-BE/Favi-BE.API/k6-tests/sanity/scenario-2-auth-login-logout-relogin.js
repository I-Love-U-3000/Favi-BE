/**
 * SANITY-AUTH-LOGIN_LOGOUT_RELOGIN-SINGLE_USER
 * 
 * Mục tiêu: Xác nhận token lifecycle ổn định
 * Luồng thực tế:
 * 1. Login lần 1 -> nhận token 1.
 * 2. Dùng token 1 gọi endpoint bảo vệ (should work).
 * 3. Login lần 2 -> nhận token 2.
 * 4. Dùng token 2 gọi endpoint bảo vệ (should work).
 * 5. Kiểm tra token 1 và token 2 khác nhau.
 * Pass: token mới dùng được, token cũ không bị kiểm tra revoke.
 * 
 * Nên chú ý thêm: Nếu có revoke policy, cần thêm bước logout và kiểm tra token cũ bị 401.
 */

import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export const options = {
  stages: [
    { duration: '10s', target: 1 }, // 1 user for 10s
  ],
  thresholds: {
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
    http_req_failed: ['rate<0.1'],
  },
};

export default function () {
  const credentials = {
    emailOrUsername: 'user_00001',
    password: '123456',
  };

  // Step 1: Login
  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({
      emailOrUsername: credentials.emailOrUsername,
      password: credentials.password,
    }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  check(loginRes, {
    'login status is 200': (r) => r.status === 200,
    'login response has accessToken': (r) => r.json('accessToken') !== null && r.json('accessToken') !== undefined,
  });

  let firstToken = null;
  if (loginRes.status === 200) {
    firstToken = loginRes.json('accessToken');
  }

  sleep(0.5);

  // Step 2: Call protected endpoint with first token
  if (firstToken) {
    const profileRes = http.get(
      `${BASE_URL}/api/posts/feed?page=1&pageSize=5`,
      { headers: { 'Authorization': `Bearer ${firstToken}` } }
    );

    check(profileRes, {
      'get protected endpoint with first token is 200': (r) => r.status === 200,
      'protected response has items': (r) => Array.isArray(r.json('items')),
    });
  }

  sleep(0.5);

  // Step 3: Login again
  const reloginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({
      emailOrUsername: credentials.emailOrUsername,
      password: credentials.password,
    }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  check(reloginRes, {
    'relogin status is 200': (r) => r.status === 200,
    'relogin response has accessToken': (r) => r.json('accessToken') !== null && r.json('accessToken') !== undefined,
  });

  let secondToken = null;
  if (reloginRes.status === 200) {
    secondToken = reloginRes.json('accessToken');
  }

  sleep(0.5);

  // Step 4: Call protected endpoint with new token (should work)
  if (secondToken) {
    const profileRes = http.get(
      `${BASE_URL}/api/posts/feed?page=1&pageSize=5`,
      { headers: { 'Authorization': `Bearer ${secondToken}` } }
    );

    check(profileRes, {
      'get protected endpoint with new token is 200': (r) => r.status === 200,
      'new token is valid': (r) => Array.isArray(r.json('items')),
    });
  }

  // Verify tokens are different
  check(firstToken !== secondToken, {
    'new token is different from old token': (result) => result === true,
  });
}
