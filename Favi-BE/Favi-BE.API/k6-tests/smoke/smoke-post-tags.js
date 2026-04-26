import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, login } from './common.js';

// NOTE: Dedicated POST/DELETE /api/posts/{id}/tags endpoints do not exist yet in PostController
// (legacy path: tags are set at creation time via CreatePostRequest.Tags).
// AddPostTagsCommand / RemovePostTagCommand handlers are wired in the module but controller
// strangler for those operations is pending. This smoke test covers the currently available
// surface: create-with-tags parity + tag lookup via GET /api/tags/{id}/posts.

export const options = {
  thresholds: {
    http_req_duration: ['p(95)<3000'],
    http_req_failed: ['rate<0.6'],
  },
};

export default function () {
  const { token } = login('user_00001', '123456');
  const headers = { Authorization: `Bearer ${token}` };

  // --- Create post with tags ---
  const createRes = http.post(
    `${BASE_URL}/api/posts`,
    { caption: 'Tag smoke test post', privacyLevel: 'Public', tags: ['smoke-tag-alpha', 'smoke-tag-beta'] },
    { headers }
  );

  check(createRes, {
    'create post with tags: 201': (r) => r.status === 201,
  });

  const postId = createRes.json('id');

  // --- Verify tags in post response ---
  const readRes = http.get(`${BASE_URL}/api/posts/${postId}`, { headers });

  check(readRes, {
    'read post: 200': (r) => r.status === 200,
    'post has at least 2 tags': (r) => (r.json('tags') || []).length >= 2,
    'smoke-tag-alpha present': (r) =>
      (r.json('tags') || []).some((t) => t.name === 'smoke-tag-alpha'),
    'smoke-tag-beta present': (r) =>
      (r.json('tags') || []).some((t) => t.name === 'smoke-tag-beta'),
  });

  // --- Verify tag is discoverable via GET /api/tags/{id}/posts ---
  const tagAlpha = (readRes.json('tags') || []).find((t) => t.name === 'smoke-tag-alpha');
  if (tagAlpha) {
    const tagPostsRes = http.get(`${BASE_URL}/api/tags/${tagAlpha.id}/posts`, { headers });

    check(tagPostsRes, {
      'tag posts endpoint: 200': (r) => r.status === 200,
      'post appears under its tag': (r) => {
        const items = r.json('items') || [];
        return items.some((p) => p.id === postId);
      },
    });
  }

  // Cleanup
  http.del(`${BASE_URL}/api/posts/${postId}`, null, { headers });
}
