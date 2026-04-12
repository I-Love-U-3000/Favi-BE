import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, login, authHeaders } from './common.js';

export const options = {
  thresholds: {
    http_req_duration: ['p(95)<3000'],
    http_req_failed: ['rate<0.6'],
  },
};

export default function () {
  const { res: loginRes, token } = login('user_00001', '123456');

  check(loginRes, {
    'login status is 200': (r) => r.status === 200,
    'login response has accessToken': (r) => r.json('accessToken') !== undefined,
  });

  const feedRes = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=5`, {
    headers: authHeaders(token),
  });

  check(feedRes, {
    'protected feed status is 200': (r) => r.status === 200,
    'protected feed has items': (r) => Array.isArray(r.json('items')),
  });
}