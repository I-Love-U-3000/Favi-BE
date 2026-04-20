// Functional Search/Related scenarios for semantic search and tag-based fallback behavior.
import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, pickSeedToken, pickSeedPostId } from './common.js';

export const options = {
  scenarios: {
    func_search_related: {
      executor: 'per-vu-iterations',
      vus: 1,
      iterations: 1,
      maxDuration: '2m',
    },
  },
};

export default function () {
  const token = pickSeedToken();
  const postId = pickSeedPostId();
  const headers = {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json',
  };

  // FUNC-SEARCH-SEMANTIC_BASIC
  const semanticCommon = http.post(
    `${BASE_URL}/api/search/semantic`,
    JSON.stringify({ query: 'seed', page: 1, pageSize: 10, k: 50 }),
    { headers }
  );
  const semanticRare = http.post(
    `${BASE_URL}/api/search/semantic`,
    JSON.stringify({ query: 'qzxwv-rare-query', page: 1, pageSize: 10, k: 50 }),
    { headers }
  );

  check(semanticCommon, {
    'FUNC-SEARCH-SEMANTIC_BASIC common query 200': (r) => r.status === 200,
    'FUNC-SEARCH-SEMANTIC_BASIC common posts array': (r) => Array.isArray(r.json('posts')),
  });
  check(semanticRare, {
    'FUNC-SEARCH-SEMANTIC_BASIC rare query 200': (r) => r.status === 200,
    'FUNC-SEARCH-SEMANTIC_BASIC rare posts array': (r) => Array.isArray(r.json('posts')),
  });

  // FUNC-RELATED-TAG_BASED_FALLBACK
  const related = http.get(`${BASE_URL}/api/posts/${postId}/related?page=1&pageSize=10`, { headers });
  check(related, {
    'FUNC-RELATED-TAG_BASED_FALLBACK status 200': (r) => r.status === 200,
    'FUNC-RELATED-TAG_BASED_FALLBACK items array': (r) => Array.isArray(r.json('items')),
  });
}
