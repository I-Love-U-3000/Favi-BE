import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, login, seedUsers } from './common.js';

export const options = {
  thresholds: {
    http_req_duration: ['p(95)<3000'],
    http_req_failed: ['rate<0.6'],
  },
};

export default function () {
  const { token } = login('user_00001', '123456');

  const feedRes = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  check(feedRes, {
    '/api/posts/feed status is 200': (r) => r.status === 200,
    '/api/posts/feed response has items': (r) => Array.isArray(r.json('items')),
  });

  const guestFeedRes = http.get(`${BASE_URL}/api/posts/guest-feed?page=1&pageSize=20`);
  check(guestFeedRes, {
    '/api/posts/guest-feed status is 200': (r) => r.status === 200,
    '/api/posts/guest-feed response has items': (r) => Array.isArray(r.json('items')),
  });

  const latestRes = http.get(`${BASE_URL}/api/posts/latest?page=1&pageSize=20`);
  check(latestRes, {
    '/api/posts/latest status is 200': (r) => r.status === 200,
    '/api/posts/latest response has items': (r) => Array.isArray(r.json('items')),
  });

  const sampleProfileId = seedUsers.find((u) => u.profile_id)?.profile_id;
  if (sampleProfileId) {
    const profileFeedRes = http.get(`${BASE_URL}/api/posts/profile/${sampleProfileId}?page=1&pageSize=20`);
    check(profileFeedRes, {
      '/api/posts/profile/{id} status is 200': (r) => r.status === 200,
      '/api/posts/profile/{id} response has items': (r) => Array.isArray(r.json('items')),
    });
  }
}