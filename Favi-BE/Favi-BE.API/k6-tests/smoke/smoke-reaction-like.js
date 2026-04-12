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
    throw new Error('No seeded post_id found for smoke reaction test');
  }

  const headers = { Authorization: `Bearer ${token}` };

  // Like post
  const likeRes = http.post(`${BASE_URL}/api/posts/${postId}/reactions?type=Like`, null, { headers });

  check(likeRes, {
    'like post status is 200': (r) => r.status === 200,
  });

  // Verify reaction count
  const postRes = http.get(`${BASE_URL}/api/posts/${postId}`, { headers });

  check(postRes, {
    'post status is 200': (r) => r.status === 200,
    'post has reactions summary': (r) => r.json('reactions.total') !== undefined,
  });
}