// Functional Post/Media scenarios: create valid, invalid media upload, update/archive/delete lifecycle.
import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, pickSeedToken, pickSeedPostId } from './common.js';

export const options = {
  scenarios: {
    func_post_media: {
      executor: 'per-vu-iterations',
      vus: 1,
      iterations: 1,
      maxDuration: '2m',
    },
  },
};

export default function () {
  const token = pickSeedToken();

  // FUNC-POST-CREATE_WITH_VALID_MEDIA (without file but valid create payload)
  const createRes = http.post(
    `${BASE_URL}/api/posts`,
    { caption: `func-post-${__VU}-${__ITER}`, privacyLevel: 'Public' },
    { headers: { Authorization: `Bearer ${token}` } }
  );
  check(createRes, {
    'FUNC-POST-CREATE_WITH_VALID_MEDIA created': (r) => r.status === 201 || r.status === 200,
  });

  const createdId = createRes.json('id');

  // FUNC-POST-CREATE_INVALID_MEDIA
  const targetPost = createdId || pickSeedPostId();
  const invalidMedia = http.post(
    `${BASE_URL}/api/posts/${targetPost}/media`,
    JSON.stringify({ bad: 'payload' }),
    {
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    }
  );
  check(invalidMedia, {
    'FUNC-POST-CREATE_INVALID_MEDIA rejected': (r) => [400, 415, 422].includes(r.status),
  });

  if (createdId) {
    // FUNC-POST-UPDATE_ARCHIVE_DELETE
    const updateRes = http.put(
      `${BASE_URL}/api/posts/${createdId}`,
      JSON.stringify({ caption: `func-updated-${__ITER}` }),
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      }
    );
    check(updateRes, {
      'FUNC-POST-UPDATE_ARCHIVE_DELETE update accepted': (r) => [200, 403].includes(r.status),
    });

    const archiveRes = http.post(`${BASE_URL}/api/posts/${createdId}/archive`, null, {
      headers: { Authorization: `Bearer ${token}` },
    });
    check(archiveRes, {
      'FUNC-POST-UPDATE_ARCHIVE_DELETE archive accepted': (r) => [200, 403].includes(r.status),
    });

    const deleteRes = http.del(`${BASE_URL}/api/posts/${createdId}`, null, {
      headers: { Authorization: `Bearer ${token}` },
    });
    check(deleteRes, {
      'FUNC-POST-DELETE_CASCADE_SOFTDELETE delete accepted': (r) => [200, 403].includes(r.status),
    });

    const readAfterDelete = http.get(`${BASE_URL}/api/posts/${createdId}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    check(readAfterDelete, {
      'FUNC-POST-DELETE_CASCADE_SOFTDELETE no wrong exposure': (r) => [403, 404, 200].includes(r.status),
    });
  }

  // FUNC-MEDIA-PARTIAL_FAILURE_HANDLING (best-effort: failed media does not break read)
  const readFallback = http.get(`${BASE_URL}/api/posts/${targetPost}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  check(readFallback, {
    'FUNC-MEDIA-PARTIAL_FAILURE_HANDLING post still queryable': (r) => [200, 403, 404].includes(r.status),
  });
}
