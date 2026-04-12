import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, login } from './common.js';

export const options = {
  thresholds: {
    http_req_duration: ['p(95)<3000'],
    http_req_failed: ['rate<0.6'],
  },
};

export default function () {
  const { token } = login('user_00001', '123456');
  const headers = { Authorization: `Bearer ${token}` };

  // Create post
  const createRes = http.post(
    `${BASE_URL}/api/posts`,
    {
      caption: 'Smoke test post',
      privacyLevel: 'Public',
    },
    { headers }
  );

  check(createRes, {
    'create post status is 201': (r) => r.status === 201,
    'create post response has id': (r) => r.json('id') !== undefined,
  });

  const postId = createRes.json('id');

  // Read post
  const readRes = http.get(`${BASE_URL}/api/posts/${postId}`, { headers });

  check(readRes, {
    'read post status is 200': (r) => r.status === 200,
    'read post response has correct id': (r) => r.json('id') === postId,
  });

  // Delete post
  const deleteRes = http.del(`${BASE_URL}/api/posts/${postId}`, null, { headers });

  check(deleteRes, {
    'delete post status is 200': (r) => r.status === 200,
  });

  const rereadRes = http.get(`${BASE_URL}/api/posts/${postId}`, { headers });
  check(rereadRes, {
    'read after delete is blocked': (r) => r.status === 404 || r.status === 403,
  });
}