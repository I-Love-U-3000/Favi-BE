// Shared helpers for Functional k6 scenarios.
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
    } catch (_) {}
  }

  throw new Error(`Cannot load ${fileName}. Checked: ${candidates.join(', ')}`);
}

export const seedUsers = loadCsv('users.csv').filter((u) => u.username && u.password && u.profile_id);
export const seedTokens = loadCsv('tokens.csv').filter((t) => t.profile_id && t.token);
export const seedPosts = loadCsv('posts.csv').filter((p) => p.post_id && String(p.is_archived).toLowerCase() !== 'true');

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

export function pickSeedUser() {
  return seedUsers[(__VU + __ITER) % seedUsers.length];
}

export function pickSeedToken() {
  return seedTokens[(__VU + __ITER) % seedTokens.length].token;
}

export function pickSeedPostId() {
  return seedPosts[(__VU + __ITER) % seedPosts.length].post_id;
}
