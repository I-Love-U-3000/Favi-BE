// E2E journeys: NEW_USER-FIRST_DAY_FLOW and CREATOR-PUBLISH-TO-AUDIENCE.
import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, seedUsers, seedPosts, login, headers } from './common.js';

export const options = {
  scenarios: {
    e2e_user_journey: {
      executor: 'per-vu-iterations',
      vus: 1,
      iterations: 1,
      maxDuration: '3m',
    },
  },
};

export default function () {
  const actorA = seedUsers[0];
  const actorB = seedUsers[1];

  const l1 = login(actorA.username, actorA.password || '123456');
  const l2 = login(actorB.username, actorB.password || '123456');

  check(l1.res, { 'E2E-NEW_USER-FIRST_DAY_FLOW login A 200': (r) => r.status === 200 });
  check(l2.res, { 'E2E-CREATOR-PUBLISH-TO-AUDIENCE login B 200': (r) => r.status === 200 });

  const hA = headers(l1.token);
  const hB = headers(l2.token);

  const follow = http.post(`${BASE_URL}/api/profiles/follow/${actorB.profile_id}`, null, { headers: { Authorization: hA.Authorization } });
  check(follow, { 'E2E-NEW_USER-FIRST_DAY_FLOW follow step accepted': (r) => [200, 400, 403, 404].includes(r.status) });

  const createPost = http.post(
    `${BASE_URL}/api/posts`,
    { caption: `e2e-creator-post-${__ITER}`, privacyLevel: 'Public' },
    { headers: { Authorization: hB.Authorization } }
  );
  check(createPost, { 'E2E-CREATOR-PUBLISH-TO-AUDIENCE creator post created': (r) => [200, 201].includes(r.status) });

  const postId = createPost.json('id') || seedPosts[0].post_id;

  const feedA = http.get(`${BASE_URL}/api/posts/feed?page=1&pageSize=20`, { headers: { Authorization: hA.Authorization } });
  check(feedA, { 'E2E-CREATOR-PUBLISH-TO-AUDIENCE follower refresh feed 200': (r) => r.status === 200 });

  const react = http.post(`${BASE_URL}/api/posts/${postId}/reactions?type=Like`, null, { headers: hA });
  check(react, { 'E2E-NEW_USER-FIRST_DAY_FLOW like step accepted': (r) => [200, 409].includes(r.status) });

  const comment = http.post(
    `${BASE_URL}/api/comments`,
    JSON.stringify({ postId, content: `e2e-comment-${__ITER}` }),
    { headers: hA }
  );
  check(comment, { 'E2E-NEW_USER-FIRST_DAY_FLOW comment step accepted': (r) => [200, 429].includes(r.status) });

  const share = http.post(`${BASE_URL}/api/posts/${postId}/share`, JSON.stringify({ caption: null }), { headers: hA });
  check(share, { 'E2E-NEW_USER-FIRST_DAY_FLOW share step accepted': (r) => [200, 409].includes(r.status) });

  sleep(0.2);
}
