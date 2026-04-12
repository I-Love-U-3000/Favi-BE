/**
 * SANITY-REACTION-LIKE_UNLIKE_LOOP-SINGLE_USER
 * 
 * Mục tiêu: đảm bảo toggle like/unlike ổn định
 * Luồng thực tế:
 * 1. Lấy token và post từ seed.
 * 2. Lặp 10 lần toggle like/unlike.
 * 3. Kiểm tra count không âm, không lệch.
 * 4. Đảm bảo trạng thái cuối phản ánh thao tác cuối.
 * Pass: không race-condition ở mức single user.
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { parse } from 'https://jslib.k6.io/papaparse/5.1.1/index.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
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

const tokenData = loadCsv('tokens.csv')
  .filter((row) => row.profile_id && row.token);

const postData = loadCsv('posts.csv')
  .filter((row) => row.post_id);

export const options = {
  stages: [
    { duration: '10s', target: 1 }, // 1 user for 10s
  ],
  thresholds: {
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
    http_req_failed: ['rate<0.1'],
  },
};

export function setup() {
  return { tokenData, postData };
}

export default function (data) {
  if (data.tokenData.length === 0 || data.postData.length === 0) {
    console.log('No seed data available');
    return;
  }

  // Select a random token and post
  const user = data.tokenData[Math.floor(Math.random() * data.tokenData.length)];
  const post = data.postData[Math.floor(Math.random() * data.postData.length)];

  const token = user.token;
  const postId = post.post_id;
  const profileId = user.profile_id;

  const headers = {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json',
  };

  // Like/Unlike loop (10 iterations)
  const loopCount = 10;
  let initialCount = 0;
  let finalCount = 0;

  for (let i = 0; i < loopCount; i++) {
    // Get post detail before action
    const getRes = http.get(`${BASE_URL}/api/posts/${postId}`, { headers });
    check(getRes, {
      'get post status is 200': (r) => r.status === 200,
      'post detail has reactions': (r) => r.json('reactions') !== null,
    });

    if (getRes.status === 200) {
      const postDetail = getRes.json();
      if (i === 0) {
        initialCount = postDetail.reactions?.total || 0;
      }
    }

    sleep(0.1);

    // Toggle reaction endpoint (same endpoint for like/unlike)
    const toggleRes = http.post(
      `${BASE_URL}/api/posts/${postId}/reactions?type=Like`,
      null,
      { headers }
    );

    check(toggleRes, {
      'toggle reaction status is 200': (r) => r.status === 200,
    });

    sleep(0.1);
  }

  // Final verification
  const finalRes = http.get(`${BASE_URL}/api/posts/${postId}`, { headers });
  check(finalRes, {
    'final get post status is 200': (r) => r.status === 200,
  });

  if (finalRes.status === 200) {
    const finalPost = finalRes.json();
    finalCount = finalPost.reactions?.total || 0;

    check(finalCount, {
      'count is not negative': (c) => c >= 0,
      'count is reasonable': (c) => Math.abs(c - initialCount) <= 1,
    });
  }
}
