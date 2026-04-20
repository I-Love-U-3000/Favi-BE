// Shared helpers for Integration + E2E k6 scenarios.
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
export const seedPosts = loadCsv('posts.csv').filter((p) => p.post_id);

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

export function headers(token) {
  return {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json',
  };
}
