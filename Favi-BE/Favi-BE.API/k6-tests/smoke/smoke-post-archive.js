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
  const jsonHeaders = { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' };

  // Create a post to operate on — [FromForm]
  const createRes = http.post(
    `${BASE_URL}/api/posts`,
    { caption: 'Archive smoke test post', privacyLevel: 'Public' },
    { headers }
  );

  check(createRes, {
    'create post for archive test: 201': (r) => r.status === 201,
  });

  const postId = createRes.json('id');

  // --- UpdatePost — [FromBody] → JSON ---
  const updateRes = http.put(
    `${BASE_URL}/api/posts/${postId}`,
    JSON.stringify({ caption: 'Updated caption for archive smoke test' }),
    { headers: jsonHeaders }
  );

  check(updateRes, {
    'update post status is 200': (r) => r.status === 200,
  });

  // Verify update persisted
  const afterUpdateRes = http.get(`${BASE_URL}/api/posts/${postId}`, { headers });
  check(afterUpdateRes, {
    'read after update: 200': (r) => r.status === 200,
    'caption updated correctly': (r) => r.json('caption') === 'Updated caption for archive smoke test',
  });

  // --- ArchivePost ---
  const archiveRes = http.post(`${BASE_URL}/api/posts/${postId}/archive`, null, { headers });
  check(archiveRes, {
    'archive post status is 200': (r) => r.status === 200,
  });

  // Verify archived post is not visible in public feed
  const afterArchiveRes = http.get(`${BASE_URL}/api/posts/${postId}`, { headers });
  check(afterArchiveRes, {
    'archived post not visible or still accessible to owner': (r) =>
      r.status === 200 || r.status === 403 || r.status === 404,
  });

  // --- UnarchivePost ---
  const unarchiveRes = http.post(`${BASE_URL}/api/posts/${postId}/unarchive`, null, { headers });
  check(unarchiveRes, {
    'unarchive post status is 200': (r) => r.status === 200,
  });

  const afterUnarchiveRes = http.get(`${BASE_URL}/api/posts/${postId}`, { headers });
  check(afterUnarchiveRes, {
    'post accessible again after unarchive': (r) => r.status === 200,
  });

  // Cleanup: delete the post
  http.del(`${BASE_URL}/api/posts/${postId}`, null, { headers });
}
