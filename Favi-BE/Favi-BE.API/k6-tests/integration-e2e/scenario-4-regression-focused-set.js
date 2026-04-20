// Regression-focused integration mini set from plan section 4.
import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, seedPosts, login, seedUsers, headers } from './common.js';

export const options = {
  scenarios: {
    int_regression_set: {
      executor: 'per-vu-iterations',
      vus: 1,
      iterations: 1,
      maxDuration: '3m',
    },
  },
};

export default function () {
  const actor = seedUsers[6];
  const { token } = login(actor.username, actor.password || '123456');
  const h = headers(token);
  const postId = seedPosts[0].post_id;

  // 1) login -> feed
  const feed = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, { headers: { Authorization: `Bearer ${token}` } });
  check(feed, { 'REG-1 login->feed': (r) => r.status === 200 });

  // 2) create post -> detail -> feed
  const created = http.post(`${BASE_URL}/api/posts`, { caption: `reg-post-${__ITER}`, privacyLevel: 'Public' }, { headers: { Authorization: `Bearer ${token}` } });
  const createdId = created.json('id') || postId;
  const detail = http.get(`${BASE_URL}/api/posts/${createdId}`, { headers: { Authorization: `Bearer ${token}` } });
  const feed2 = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, { headers: { Authorization: `Bearer ${token}` } });
  check(created, { 'REG-2 create post accepted': (r) => [200, 201].includes(r.status) });
  check(detail, { 'REG-2 detail 200': (r) => r.status === 200 });
  check(feed2, { 'REG-2 feed 200': (r) => r.status === 200 });

  // 3) like/unlike loop
  const r1 = http.post(`${BASE_URL}/api/posts/${createdId}/reactions?type=Like`, null, { headers: h });
  const r2 = http.post(`${BASE_URL}/api/posts/${createdId}/reactions?type=Like`, null, { headers: h });
  check(r1, { 'REG-3 like accepted': (r) => [200, 409].includes(r.status) });
  check(r2, { 'REG-3 unlike/toggle accepted': (r) => [200, 409].includes(r.status) });

  // 4) comment parent/child
  const p = http.post(`${BASE_URL}/api/comments`, JSON.stringify({ postId: createdId, content: 'reg-parent' }), { headers: h });
  const pid = p.json('id');
  const c = http.post(`${BASE_URL}/api/comments`, JSON.stringify({ postId: createdId, parentCommentId: pid, content: 'reg-child' }), { headers: h });
  check(p, { 'REG-4 parent comment accepted': (r) => [200, 429].includes(r.status) });
  check(c, { 'REG-4 child comment accepted': (r) => [200, 400, 429].includes(r.status) });

  // 5) share + feed-with-reposts
  const share = http.post(`${BASE_URL}/api/posts/${createdId}/share`, JSON.stringify({ caption: null }), { headers: h });
  const fwr = http.get(`${BASE_URL}/api/posts/feed-with-reposts?page=1&pageSize=20`, { headers: { Authorization: `Bearer ${token}` } });
  check(share, { 'REG-5 share accepted': (r) => [200, 409].includes(r.status) });
  check(fwr, { 'REG-5 feed-with-reposts 200': (r) => r.status === 200 });

  // 6) semantic + related fallback
  const sem = http.post(`${BASE_URL}/api/search/semantic`, JSON.stringify({ query: 'seed', page: 1, pageSize: 10, k: 50 }), { headers: h });
  const rel = http.get(`${BASE_URL}/api/posts/${createdId}/related?page=1&pageSize=10`, { headers: h });
  check(sem, { 'REG-6 semantic 200': (r) => r.status === 200 });
  check(rel, { 'REG-6 related 200': (r) => r.status === 200 });
}
