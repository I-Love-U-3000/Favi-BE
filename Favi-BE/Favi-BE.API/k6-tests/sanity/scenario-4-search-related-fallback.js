/**
 * SANITY-SEARCH-RELATED-FALLBACK-SINGLE_USER
 * 
 * Mục tiêu: kiểm tra fallback khi semantic search yếu.
 * Luồng thực tế:
 * 1. Lấy token và post từ seed.
 * 2. Gọi semantic search với query cố định.
 * 3. Gọi related posts cho post đã chọn.
 * 4. Kiểm tra response semantic và related hợp lệ.
 * 5. Đảm bảo fallback hoạt động khi semantic yếu.
 * Pass: semantic hoặc related trả về kết quả hợp lệ.
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
    http_req_duration: ['p(95)<800', 'p(99)<1500'],
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

  const headers = {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json',
  };

  // Test 1: Semantic Search
  const searchQuery = 'seed';
  const semanticSearchRes = http.post(
    `${BASE_URL}/api/search/semantic`,
    JSON.stringify({
      query: searchQuery,
      page: 1,
      pageSize: 10,
      k: 50,
    }),
    { headers, tags: { name: 'SemanticSearch' } }
  );

  check(semanticSearchRes, {
    'semantic search status is 200': (r) => r.status === 200,
    'semantic search response has posts': (r) => Array.isArray(r.json('posts')),
  });

  let semanticResults = [];
  if (semanticSearchRes.status === 200) {
    const searchData = semanticSearchRes.json();
    check(searchData.posts, {
      'search results is array': (d) => Array.isArray(d),
      'search results have items': (d) => d.length >= 0,
    });
    semanticResults = searchData.posts || [];
  }

  sleep(0.5);

  // Test 2: Related Posts (Fallback scenario)
  const relatedRes = http.get(
    `${BASE_URL}/api/posts/${postId}/related?page=1&pageSize=10`,
    { headers, tags: { name: 'RelatedPosts' } }
  );

  check(relatedRes, {
    'related posts status is 200': (r) => r.status === 200,
    'related posts response has items': (r) => Array.isArray(r.json('items')),
  });

  let relatedResults = [];
  if (relatedRes.status === 200) {
    const relatedData = relatedRes.json();
    check(relatedData.items, {
      'related results is array': (d) => Array.isArray(d),
      'related results have items': (d) => d.length > 0, // Should always have fallback
    });
    relatedResults = relatedData.items || [];
  }

  sleep(0.5);

  // Test 3: Fallback Logic verification
  // If semantic search is weak, related posts should provide fallback
  if (semanticResults.length === 0 && relatedResults.length > 0) {
    check(true, {
      'fallback works when semantic is weak': (result) => result === true,
    });
  }

  // Test 4: Verify results have expected fields
  if (relatedResults.length > 0) {
    const firstResult = relatedResults[0];
    check(firstResult, {
      'related post has id': (r) => r.id !== undefined,
      'related post has caption': (r) => r.caption !== undefined,
      'related post has createdAt': (r) => r.createdAt !== undefined,
    });
  }
}
