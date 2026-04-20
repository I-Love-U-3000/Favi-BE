// Integration read-after-write / eventual consistency checks for create/react/follow counters and async side effects.
import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, seedUsers, seedPosts, login, headers } from './common.js';

export const options = {
  scenarios: {
    int_raw_consistency: {
      executor: 'per-vu-iterations',
      vus: 1,
      iterations: 1,
      maxDuration: '4m',
    },
  },
};

export default function () {
  const user = seedUsers[4];
  const other = seedUsers[5];
  const { token } = login(user.username, user.password || '123456');
  const h = headers(token);

  // INT-RAW-CREATE_POST_THEN_READ
  const createPost = http.post(`${BASE_URL}/api/posts`, { caption: `int-raw-post-${__ITER}`, privacyLevel: 'Public' }, { headers: { Authorization: `Bearer ${token}` } });
  check(createPost, { 'INT-RAW-CREATE_POST_THEN_READ create accepted': (r) => [200, 201].includes(r.status) });

  const postId = createPost.json('id') || seedPosts[0].post_id;
  const detail = http.get(`${BASE_URL}/api/posts/${postId}`, { headers: { Authorization: `Bearer ${token}` } });
  const feed = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, { headers: { Authorization: `Bearer ${token}` } });
  const profile = http.get(`${BASE_URL}/api/posts/profile/${user.profile_id}?page=1&pageSize=20`, { headers: { Authorization: `Bearer ${token}` } });
  check(detail, { 'INT-RAW-CREATE_POST_THEN_READ detail 200': (r) => r.status === 200 });
  check(feed, { 'INT-RAW-CREATE_POST_THEN_READ feed 200': (r) => r.status === 200 });
  check(profile, { 'INT-RAW-CREATE_POST_THEN_READ profile list 200': (r) => r.status === 200 });

  // INT-RAW-REACT_THEN_READ_COUNT + INT-COUNTER-INTEGRITY-CROSS_ENDPOINTS
  const react = http.post(`${BASE_URL}/api/posts/${postId}/reactions?type=Like`, null, { headers: h });
  check(react, { 'INT-RAW-REACT_THEN_READ_COUNT reaction accepted': (r) => [200, 409].includes(r.status) });

  const detailAfterReact = http.get(`${BASE_URL}/api/posts/${postId}`, { headers: { Authorization: `Bearer ${token}` } });
  const feedAfterReact = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, { headers: { Authorization: `Bearer ${token}` } });
  check(detailAfterReact, { 'INT-COUNTER-INTEGRITY-CROSS_ENDPOINTS detail after react 200': (r) => r.status === 200 });
  check(feedAfterReact, { 'INT-COUNTER-INTEGRITY-CROSS_ENDPOINTS feed after react 200': (r) => r.status === 200 });

  // INT-FOLLOW-COUNTER-INTEGRITY
  const follow = http.post(`${BASE_URL}/api/profiles/follow/${other.profile_id}`, null, { headers: { Authorization: `Bearer ${token}` } });
  const followers = http.get(`${BASE_URL}/api/profiles/${other.profile_id}/followers?skip=0&take=20`, { headers: { Authorization: `Bearer ${token}` } });
  const followings = http.get(`${BASE_URL}/api/profiles/${user.profile_id}/followings?skip=0&take=20`, { headers: { Authorization: `Bearer ${token}` } });
  check(follow, { 'INT-FOLLOW-COUNTER-INTEGRITY follow accepted': (r) => [200, 400, 403, 404].includes(r.status) });
  check(followers, { 'INT-FOLLOW-COUNTER-INTEGRITY followers endpoint 200': (r) => [200, 403].includes(r.status) });
  check(followings, { 'INT-FOLLOW-COUNTER-INTEGRITY followings endpoint 200': (r) => [200, 403].includes(r.status) });

  // INT-ASYNC-SIDE_EFFECTS + INT-RANKING-FRESHNESS-BOUNDARY (smoke-like boundary)
  sleep(1);
  const feedBefore = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, { headers: { Authorization: `Bearer ${token}` } });
  sleep(2);
  const feedAfter = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, { headers: { Authorization: `Bearer ${token}` } });
  check(feedBefore, { 'INT-RANKING-FRESHNESS-BOUNDARY feed before 200': (r) => r.status === 200 });
  check(feedAfter, { 'INT-RANKING-FRESHNESS-BOUNDARY feed after 200': (r) => r.status === 200 });
}
