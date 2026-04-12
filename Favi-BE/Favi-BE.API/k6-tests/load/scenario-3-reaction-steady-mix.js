/**
 * LOAD-REACTION-STEADY_MIX
 * Tỷ lệ: 70% read, 20% reaction/comment, 10% post create.
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, seedTokens, seedPosts, authHeaders } from './common.js';

export const options = {
  scenarios: {
    steady_mix: {
      executor: 'constant-vus',
      vus: 20,
      duration: '4m',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<3000', 'p(99)<5000'],
    http_req_failed: ['rate<0.2'],
  },
};

export default function () {
  const token = seedTokens[(__VU + __ITER) % seedTokens.length].token;
  const postId = seedPosts[(__VU + __ITER) % seedPosts.length].post_id;
  const headers = authHeaders(token);

  const bucket = Math.random();

  if (bucket < 0.7) {
    const res = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, { headers });
    check(res, { 'mix read feed 200': (r) => r.status === 200 });
  } else if (bucket < 0.9) {
    if (Math.random() < 0.5) {
      const reactRes = http.post(`${BASE_URL}/api/posts/${postId}/reactions?type=Like`, null, { headers });
      check(reactRes, { 'mix reaction 200': (r) => r.status === 200 });
    } else {
      const cmtRes = http.post(
        `${BASE_URL}/api/comments`,
        JSON.stringify({ postId, content: `load mix comment ${__VU}-${__ITER}` }),
        { headers }
      );
      check(cmtRes, { 'mix comment 200': (r) => r.status === 200 });
    }
  } else {
    const createRes = http.post(
      `${BASE_URL}/api/posts`,
      { caption: `load-mix-post-${__VU}-${__ITER}`, privacyLevel: 'Public' },
      { headers: { Authorization: `Bearer ${token}` } }
    );
    check(createRes, { 'mix create post 201/200': (r) => r.status === 201 || r.status === 200 });
  }

  sleep(0.8);
}
