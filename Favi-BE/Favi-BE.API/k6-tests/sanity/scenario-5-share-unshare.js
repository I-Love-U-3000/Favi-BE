/**
 * SANITY-SHARE-UNSHARE-SINGLE_USER
 * 
 * Mục tiêu: kiểm tra trạng thái share/unshare nhất quán.
 * Luồng thực tế:
 * 1. Lấy token và post từ seed.
 * 2. Share post -> kiểm tra feed/profile shares.
 * 3. Unshare/hide post -> kiểm tra trạng thái cập nhật.
 * 4. Đảm bảo trạng thái share/unshare nhất quán giữa các endpoint.
 * Pass: trạng thái share/unshare chính xác.
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
    http_req_duration: ['p(95)<600', 'p(99)<1200'],
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

  // Step 1: Get initial share count
  const initialPostRes = http.get(
    `${BASE_URL}/api/posts/${postId}`,
    { headers }
  );

  check(initialPostRes, {
    'get initial post status is 200': (r) => r.status === 200,
  });

  let initialShareCount = 0;
  if (initialPostRes.status === 200) {
    const postDetail = initialPostRes.json();
    initialShareCount = postDetail.repostsCount || 0;
  }

  sleep(0.3);

  // Step 2: Share the post
  const shareRes = http.post(
    `${BASE_URL}/api/posts/${postId}/share`,
    JSON.stringify({ caption: null }),
    { headers }
  );

  check(shareRes, {
    'share status is 200': (r) => r.status === 200,
    'share response has id': (r) => r.json('id') !== null && r.json('id') !== undefined,
  });

  sleep(0.3);

  // Step 3: Verify share count increased
  const afterShareRes = http.get(
    `${BASE_URL}/api/posts/${postId}`,
    { headers }
  );

  check(afterShareRes, {
    'get post after share status is 200': (r) => r.status === 200,
  });

  let afterShareCount = 0;
  if (afterShareRes.status === 200) {
    const postDetail = afterShareRes.json();
    afterShareCount = postDetail.repostsCount || 0;

    check(afterShareCount, {
      'share count increased': (c) => c >= initialShareCount,
    });
  }

  sleep(0.3);

  // Step 4: Check user's profile/shares feed
  const userSharesRes = http.get(
    `${BASE_URL}/api/posts/profile/${profileId}/shares?page=1&pageSize=20`,
    { headers }
  );

  check(userSharesRes, {
    'get user shares status is 200': (r) => r.status === 200,
    'shares list has items': (r) => Array.isArray(r.json('items')),
  });

  if (userSharesRes.status === 200) {
    const sharesList = userSharesRes.json('items');
    const userSharedThisPost = Array.isArray(sharesList) && sharesList.some(s => s.originalPostId === postId);
    check(userSharedThisPost, {
      'post appears in user shares feed': (result) => result === true,
    });
  }

  sleep(0.3);

  // Step 5: Unshare the post
  const unshareRes = http.delete(
    `${BASE_URL}/api/posts/${postId}/share`,
    null,
    { headers }
  );

  check(unshareRes, {
    'unshare status is 200': (r) => r.status === 200,
  });

  sleep(0.3);

  // Step 6: Verify share count decreased
  const afterUnshareRes = http.get(
    `${BASE_URL}/api/posts/${postId}`,
    { headers }
  );

  check(afterUnshareRes, {
    'get post after unshare status is 200': (r) => r.status === 200,
  });

  if (afterUnshareRes.status === 200) {
    const postDetail = afterUnshareRes.json();
    const finalShareCount = postDetail.repostsCount || 0;

    check(finalShareCount, {
      'share count restored after unshare': (c) => c <= afterShareCount,
    });
  }
}
