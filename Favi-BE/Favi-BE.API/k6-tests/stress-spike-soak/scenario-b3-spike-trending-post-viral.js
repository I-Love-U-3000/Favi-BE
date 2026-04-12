// SPIKE-TRENDING_POST-VIRAL
// Viral post spike scenario that combines heavy reads with occasional reactions/comments.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, seedPosts, pickToken, authHeaders } from './common.js';

const viralPostId = seedPosts[0]?.post_id;

export const options = {
  scenarios: {
    spike_viral_post: {
      executor: 'ramping-vus',
      stages: [
        { duration: '20s', target: 20 },
        { duration: '10s', target: 200 },
        { duration: '25s', target: 200 },
        { duration: '20s', target: 20 },
        { duration: '20s', target: 0 },
      ],
      gracefulRampDown: '10s',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<6000'],
    http_req_failed: ['rate<0.45'],
  },
};

export default function () {
  const headers = authHeaders(pickToken());

  const detailRes = http.get(`${BASE_URL}/api/posts/${viralPostId}`, { headers });
  check(detailRes, { 'viral detail 200': (r) => r.status === 200 });

  if (__ITER % 4 === 0) {
    const reactRes = http.post(`${BASE_URL}/api/posts/${viralPostId}/reactions?type=Like`, null, { headers });
    check(reactRes, { 'viral reaction accepted': (r) => r.status === 200 || r.status === 409 });
  }

  if (__ITER % 7 === 0) {
    const cmtRes = http.post(
      `${BASE_URL}/api/comments`,
      JSON.stringify({ postId: viralPostId, content: `viral-comment-${__VU}-${__ITER}` }),
      { headers }
    );
    check(cmtRes, { 'viral comment accepted': (r) => r.status === 200 || r.status === 429 });
  }

  sleep(0.2);
}
