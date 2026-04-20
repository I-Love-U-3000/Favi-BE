// Functional Feed scenarios: pagination, refresh consistency, dedupe/basic stability, empty state behavior.
import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, pickSeedToken } from './common.js';

export const options = {
  scenarios: {
    func_feed: {
      executor: 'per-vu-iterations',
      vus: 1,
      iterations: 1,
      maxDuration: '2m',
    },
  },
};

export default function () {
  const token = pickSeedToken();
  const headers = { Authorization: `Bearer ${token}` };

  // FUNC-FEED-PAGINATION
  const page1 = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, { headers });
  const page2 = http.get(`${BASE_URL}/api/posts/feed?page=2&pageSize=20`, { headers });
  check(page1, { 'FUNC-FEED-PAGINATION page1 200': (r) => r.status === 200 });
  check(page2, { 'FUNC-FEED-PAGINATION page2 200': (r) => r.status === 200 });

  const ids1 = (page1.json('items') || []).map((x) => x.id).filter(Boolean);
  const ids2 = (page2.json('items') || []).map((x) => x.id).filter(Boolean);
  const overlap = ids1.filter((x) => ids2.includes(x));
  check(overlap, {
    'FUNC-FEED-PAGINATION no duplicate cross pages (best-effort)': (arr) => arr.length === 0,
  });

  // FUNC-FEED-PAGINATION_STABILITY_UNDER_NEW_POSTS (best-effort snapshot stability)
  const page1Again = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, { headers });
  check(page1Again, {
    'FUNC-FEED-PAGINATION_STABILITY_UNDER_NEW_POSTS page1 refresh 200': (r) => r.status === 200,
  });

  // FUNC-FEED-REFRESH_CONSISTENCY
  for (let i = 0; i < 5; i++) {
    const refresh = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, { headers });
    check(refresh, {
      'FUNC-FEED-REFRESH_CONSISTENCY status 200': (r) => r.status === 200,
      'FUNC-FEED-REFRESH_CONSISTENCY has items': (r) => Array.isArray(r.json('items')),
    });
    sleep(0.1);
  }

  // FUNC-FEED-EMPTY_STATE (guest/latest fallback)
  const guestFeed = http.get(`${BASE_URL}/api/posts/guest-feed?page=1&pageSize=20`);
  check(guestFeed, {
    'FUNC-FEED-EMPTY_STATE guest feed available': (r) => r.status === 200,
    'FUNC-FEED-EMPTY_STATE guest has items array': (r) => Array.isArray(r.json('items')),
  });

  // FUNC-FEED-DEDUPE_POST_REPOST (basic endpoint availability)
  const withReposts = http.get(`${BASE_URL}/api/posts/feed-with-reposts?page=1&pageSize=20`, { headers });
  check(withReposts, {
    'FUNC-FEED-DEDUPE_POST_REPOST feed-with-reposts 200': (r) => r.status === 200,
    'FUNC-FEED-DEDUPE_POST_REPOST has items': (r) => Array.isArray(r.json('items')),
  });
}
