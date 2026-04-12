import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, login, seedPosts } from './common.js';

export const options = {
  thresholds: {
    http_req_duration: ['p(95)<3000'],
    http_req_failed: ['rate<0.6'],
  },
};

export default function () {
  const { token } = login('user_00001', '123456');
  const postId = seedPosts[0]?.post_id;

  if (!postId) {
    throw new Error('No seeded post_id found for smoke comment test');
  }

  const headers = {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json',
  };

  // Create comment
  const commentRes = http.post(
    `${BASE_URL}/api/comments`,
    JSON.stringify({
      postId,
      content: 'Smoke test comment',
    }),
    { headers }
  );

  check(commentRes, {
    'create comment status is 200': (r) => r.status === 200,
    'create comment response has id': (r) => r.json('id') !== undefined,
  });

  const commentId = commentRes.json('id');

  // Read comments by post
  const readRes = http.get(`${BASE_URL}/api/comments/post/${postId}?page=1&pageSize=20`, { headers });

  check(readRes, {
    'read comment status is 200': (r) => r.status === 200,
    'read comment response has items': (r) => Array.isArray(r.json('items')),
    'created comment appears in list': (r) => (r.json('items') || []).some((c) => c.id === commentId),
  });
}