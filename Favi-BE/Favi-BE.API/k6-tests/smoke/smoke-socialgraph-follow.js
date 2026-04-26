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
  const targetId = seedUsers[1]?.profile_id;

  if (!targetId) {
    throw new Error('No seeded target user found for smoke follow test');
  }

  const headers = { Authorization: `Bearer ${token}` };

  // Follow user
  const followRes = http.post(`${BASE_URL}/api/profiles/follow/${targetId}`, null, { headers });

  check(followRes, {
    'follow user status is 200': (r) => r.status === 200,
  });

  // Verify followers list
  const followersRes = http.get(`${BASE_URL}/api/profiles/${targetId}/followers`, { headers });

  check(followersRes, {
    'followers list status is 200': (r) => r.status === 200,
  });
}
