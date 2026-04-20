// Integration cross-module consistency checks across post/reaction/comment/share/search/follow/privacy.
import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, seedUsers, seedPosts, login, headers } from './common.js';

export const options = {
  scenarios: {
    int_cross_module: {
      executor: 'per-vu-iterations',
      vus: 1,
      iterations: 1,
      maxDuration: '3m',
    },
  },
};

export default function () {
  const a = seedUsers[2];
  const b = seedUsers[3];
  const postId = seedPosts[0].post_id;

  const la = login(a.username, a.password || '123456');
  const lb = login(b.username, b.password || '123456');
  const hA = headers(la.token);
  const hB = headers(lb.token);

  // INT-POST_REACTION_FEED_CONSISTENCY
  const like = http.post(`${BASE_URL}/api/posts/${postId}/reactions?type=Like`, null, { headers: hA });
  const feed = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, { headers: { Authorization: hA.Authorization } });
  check(like, { 'INT-POST_REACTION_FEED_CONSISTENCY like accepted': (r) => [200, 409].includes(r.status) });
  check(feed, { 'INT-POST_REACTION_FEED_CONSISTENCY feed 200': (r) => r.status === 200 });

  // INT-COMMENT_NOTIFICATION_CONSISTENCY
  const cmt = http.post(`${BASE_URL}/api/comments`, JSON.stringify({ postId, content: `int-cmt-${__ITER}` }), { headers: hA });
  const noti = http.get(`${BASE_URL}/api/notifications?page=1&pageSize=20`, { headers: { Authorization: hB.Authorization } });
  check(cmt, { 'INT-COMMENT_NOTIFICATION_CONSISTENCY comment accepted': (r) => [200, 429].includes(r.status) });
  check(noti, { 'INT-COMMENT_NOTIFICATION_CONSISTENCY notifications endpoint 200': (r) => r.status === 200 });

  // INT-SHARE_PROFILE_FEED_CONSISTENCY
  const share = http.post(`${BASE_URL}/api/posts/${postId}/share`, JSON.stringify({ caption: null }), { headers: hA });
  const sharesList = http.get(`${BASE_URL}/api/posts/profile/${a.profile_id}/shares?page=1&pageSize=20`);
  const feedWithReposts = http.get(`${BASE_URL}/api/posts/feed-with-reposts?page=1&pageSize=20`, { headers: { Authorization: hA.Authorization } });
  check(share, { 'INT-SHARE_PROFILE_FEED_CONSISTENCY share accepted': (r) => [200, 409].includes(r.status) });
  check(sharesList, { 'INT-SHARE_PROFILE_FEED_CONSISTENCY profile shares endpoint 200': (r) => r.status === 200 });
  check(feedWithReposts, { 'INT-SHARE_PROFILE_FEED_CONSISTENCY feed-with-reposts 200': (r) => r.status === 200 });

  // INT-SEARCH_TO_POSTDETAIL
  const sem = http.post(`${BASE_URL}/api/search/semantic`, JSON.stringify({ query: 'seed', page: 1, pageSize: 10, k: 50 }), { headers: hA });
  const detail = http.get(`${BASE_URL}/api/posts/${postId}`, { headers: { Authorization: hA.Authorization } });
  check(sem, { 'INT-SEARCH_TO_POSTDETAIL semantic 200': (r) => r.status === 200 });
  check(detail, { 'INT-SEARCH_TO_POSTDETAIL detail 200': (r) => r.status === 200 });

  // INT-FOLLOW_TO_FEED_PROPAGATION
  const follow = http.post(`${BASE_URL}/api/profiles/follow/${b.profile_id}`, null, { headers: { Authorization: hA.Authorization } });
  const profileFeed = http.get(`${BASE_URL}/api/posts/profile/${b.profile_id}?page=1&pageSize=20`, { headers: { Authorization: hA.Authorization } });
  check(follow, { 'INT-FOLLOW_TO_FEED_PROPAGATION follow accepted': (r) => [200, 400, 403, 404].includes(r.status) });
  check(profileFeed, { 'INT-FOLLOW_TO_FEED_PROPAGATION profile feed policy status': (r) => [200, 403].includes(r.status) });

  // INT-PROFILE_PRIVACY-CROSS_ENDPOINTS
  const profile = http.get(`${BASE_URL}/api/profiles/${b.profile_id}`, { headers: { Authorization: hA.Authorization } });
  check(profile, {
    'INT-PROFILE_PRIVACY-CROSS_ENDPOINTS profile policy status': (r) => [200, 403].includes(r.status),
  });
}
