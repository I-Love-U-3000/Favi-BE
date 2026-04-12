import http from 'k6/http';
import { parse } from 'https://jslib.k6.io/papaparse/5.1.1/index.js';

export const BASE_URL = __ENV.BASE_URL || 'https://localhost:7138';
const SEED_OUTPUT_DIR = __ENV.SEED_OUTPUT_DIR || '../../seed-output';

function loadCsv(fileName) {
  const candidates = [
    `${SEED_OUTPUT_DIR}/${fileName}`,
    `../../seed-output/${fileName}`,
    `./seed-output/${fileName}`,
    `seed-output/${fileName}`,
  ];

  for (const path of candidates) {
    try {
      return parse(open(path), { header: true, skipEmptyLines: true }).data;
    } catch (_) {
    }
  }

  throw new Error(`Cannot load ${fileName}. Checked: ${candidates.join(', ')}`);
}

export const seedUsers = loadCsv('users.csv').filter((u) => u.username && u.password && u.profile_id);
export const seedTokens = loadCsv('tokens.csv').filter((t) => t.token && t.profile_id);
export const seedPosts = loadCsv('posts.csv').filter((p) => p.post_id && String(p.is_archived).toLowerCase() !== 'true');
export const seedFollows = loadCsv('follows.csv').filter((f) => f.follower_id && f.followee_id);

const tokenByProfile = new Map(seedTokens.map((t) => [t.profile_id, t.token]));

export function authHeaders(token) {
  return {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json',
  };
}

export function login(emailOrUsername, password) {
  const res = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ emailOrUsername, password }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  return {
    res,
    token: res.status === 200 ? res.json('accessToken') : null,
  };
}

export function getTokenForProfile(profileId) {
  return tokenByProfile.get(profileId) || null;
}

export function getPowerUserProfileIds(limit = 20) {
  const counts = new Map();
  for (const f of seedFollows) {
    counts.set(f.followee_id, (counts.get(f.followee_id) || 0) + 1);
  }

  return [...counts.entries()]
    .sort((a, b) => b[1] - a[1])
    .slice(0, limit)
    .map(([profileId]) => profileId)
    .filter((id) => tokenByProfile.has(id));
}
