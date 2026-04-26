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
  const headers = { Authorization: `Bearer ${token}` };

  // Use a seeded post to add to the collection
  const seedPostId = seedPosts[0]?.post_id;
  if (!seedPostId) {
    throw new Error('No seeded post found for collection smoke test');
  }

  // --- CreateCollection ---
  const createRes = http.post(
    `${BASE_URL}/api/collections`,
    { title: 'Smoke Test Collection', privacyLevel: 'Public' },
    { headers }
  );

  check(createRes, {
    'create collection status is 200': (r) => r.status === 200,
    'create collection response has id': (r) => r.json('id') !== undefined,
  });

  const collectionId = createRes.json('id');

  // --- GetCollection (read-back) ---
  const readRes = http.get(`${BASE_URL}/api/collections/${collectionId}`, { headers });
  check(readRes, {
    'read collection status is 200': (r) => r.status === 200,
    'read collection has correct title': (r) => r.json('title') === 'Smoke Test Collection',
  });

  // --- UpdateCollection ---
  const updateRes = http.put(
    `${BASE_URL}/api/collections/${collectionId}`,
    { title: 'Updated Smoke Collection', privacyLevel: 'Public' },
    { headers }
  );

  check(updateRes, {
    'update collection status is 200': (r) => r.status === 200,
  });

  const afterUpdateRes = http.get(`${BASE_URL}/api/collections/${collectionId}`, { headers });
  check(afterUpdateRes, {
    'title updated correctly': (r) => r.json('title') === 'Updated Smoke Collection',
  });

  // --- AddPostToCollection ---
  const addPostRes = http.post(
    `${BASE_URL}/api/collections/${collectionId}/posts/${seedPostId}`,
    null,
    { headers }
  );

  check(addPostRes, {
    'add post to collection status is 200': (r) => r.status === 200,
  });

  // Verify post appears in collection
  const collectionPostsRes = http.get(
    `${BASE_URL}/api/collections/${collectionId}/posts`,
    { headers }
  );

  check(collectionPostsRes, {
    'collection posts status is 200': (r) => r.status === 200,
    'collection contains the added post': (r) => {
      const items = r.json('items') || [];
      return items.some((p) => p.id === seedPostId);
    },
  });

  // --- RemovePostFromCollection ---
  const removePostRes = http.del(
    `${BASE_URL}/api/collections/${collectionId}/posts/${seedPostId}`,
    null,
    { headers }
  );

  check(removePostRes, {
    'remove post from collection status is 204': (r) => r.status === 204,
  });

  // Verify post no longer in collection
  const afterRemoveRes = http.get(
    `${BASE_URL}/api/collections/${collectionId}/posts`,
    { headers }
  );

  check(afterRemoveRes, {
    'collection posts empty after remove': (r) => {
      const items = r.json('items') || [];
      return !items.some((p) => p.id === seedPostId);
    },
  });

  // --- DeleteCollection ---
  const deleteRes = http.del(`${BASE_URL}/api/collections/${collectionId}`, null, { headers });
  check(deleteRes, {
    'delete collection status is 204': (r) => r.status === 204,
  });

  // Verify collection is gone
  const afterDeleteRes = http.get(`${BASE_URL}/api/collections/${collectionId}`, { headers });
  check(afterDeleteRes, {
    'collection not found after delete': (r) => r.status === 404,
  });
}
