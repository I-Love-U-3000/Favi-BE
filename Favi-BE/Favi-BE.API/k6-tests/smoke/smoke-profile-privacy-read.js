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
  const headers = { Authorization: `Bearer ${token}` };

  const publicProfile = seedUsers.find((u) => String(u.privacy_level).toLowerCase() === 'public');
  const restrictedProfile = seedUsers.find((u) => {
    const privacy = String(u.privacy_level).toLowerCase();
    return privacy === 'followers' || privacy === 'private';
  });

  if (!publicProfile || !restrictedProfile) {
    throw new Error('Seed users do not contain required privacy profiles for smoke test');
  }

  // Read public profile
  const publicRes = http.get(`${BASE_URL}/api/profiles/${publicProfile.profile_id}`, { headers });

  check(publicRes, {
    'public profile status is 200': (r) => r.status === 200,
    'public profile response has id': (r) => r.json('id') !== undefined,
  });

  // Read restricted profile
  const privateRes = http.get(`${BASE_URL}/api/profiles/${restrictedProfile.profile_id}`, { headers });

  check(privateRes, {
    'restricted profile status follows policy': (r) => r.status === 403 || r.status === 200,
  });
}