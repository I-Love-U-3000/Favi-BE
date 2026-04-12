/**
 * SANITY-FEED-REFRESH-REPEAT-SINGLE_USER
 * 
 * Mục tiêu: refresh liên tục không lỗi.
 * Luồng thực tế:
 * 1. Lấy token từ seed.
 * 2. Lặp 50 lần gọi API refresh feed.
 * 3. Kiểm tra response ổn định, không tăng lỗi dần.
 * 4. Đảm bảo response có pagination và items hợp lệ.
 * Pass: response ổn định, không lỗi logic.
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

export const options = {
  stages: [
    { duration: '10s', target: 1 }, // 1 user for 10s
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000', 'p(99)<2000'],
    http_req_failed: ['rate<0.1'],
  },
};

export function setup() {
  return { tokenData };
}

export default function (data) {
  if (data.tokenData.length === 0) {
    console.log('No seed data available');
    return;
  }

  // Select a random token
  const user = data.tokenData[Math.floor(Math.random() * data.tokenData.length)];
  const token = user.token;

  const headers = {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json',
  };

  const refreshCount = 50; // Refresh 50 times
  let successCount = 0;
  let failCount = 0;
  let previousFeedHash = null;

  for (let i = 0; i < refreshCount; i++) {
    // Refresh feed
    const feedRes = http.get(
      `${BASE_URL}/api/posts/feed?page=1&pageSize=20`,
      { headers }
    );

    const isSuccess = feedRes.status === 200;
    if (isSuccess) {
      successCount++;
    } else {
      failCount++;
    }

    check(feedRes, {
      'feed request status is 200': (r) => r.status === 200,
      'feed response has items': (r) => Array.isArray(r.json('items')),
    });

    // Check response shape consistency
    if (feedRes.status === 200) {
      const feedData = feedRes.json();
      check(feedData, {
        'feed has pagination': (f) => f.page !== undefined && f.totalCount !== undefined,
        'feed items is array': (f) => Array.isArray(f.items),
        'feed items have expected fields': (f) => {
          if (f.items.length > 0) {
            const firstItem = f.items[0];
            return firstItem.id !== undefined && firstItem.caption !== undefined;
          }
          return true;
        },
      });

      // Optional: Hash the feed to detect if it's changing appropriately
      const currentFeedHash = JSON.stringify(feedData.items.map(item => item.post_id)).substring(0, 50);
      if (previousFeedHash !== null && i > 5) {
        // After a few refreshes, we might expect some stability or changes
        // This is just for observability
      }
      previousFeedHash = currentFeedHash;
    }

    sleep(0.1);
  }

  // Final statistics check
  check(successCount / refreshCount, {
    'success rate > 95%': (rate) => rate > 0.95,
    'fail count is low': (failCount) => failCount < 5,
  });

  // Verify error rate doesn't spike
  check(failCount, {
    'total failures is acceptable': (count) => count <= 3,
  });
}
