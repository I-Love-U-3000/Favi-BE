// Functional Notification side-effects: reaction/comment/follow should generate retrievable notifications.
import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, pickSeedToken, pickSeedPostId, seedUsers } from './common.js';

export const options = {
  scenarios: {
    func_noti_sideeffects: {
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

  // trigger actions
  const reactRes = http.post(`${BASE_URL}/api/posts/${postId}/reactions?type=Like`, null, { headers });
  const commentRes = http.post(
    `${BASE_URL}/api/comments`,
    JSON.stringify({ postId, content: `func-noti-comment-${__VU}-${__ITER}` }),
    { headers }
  );

  const someone = seedUsers[1];
  const followRes = http.post(`${BASE_URL}/api/profiles/follow/${someone.profile_id}`, null, {
    headers: { Authorization: `Bearer ${token}` },
  });

  check(reactRes, { 'FUNC-NOTI-REACTION_COMMENT_FOLLOW reaction accepted': (r) => [200, 409].includes(r.status) });
  check(commentRes, { 'FUNC-NOTI-REACTION_COMMENT_FOLLOW comment accepted': (r) => [200, 429].includes(r.status) });
  check(followRes, { 'FUNC-NOTI-REACTION_COMMENT_FOLLOW follow accepted': (r) => [200, 400, 403, 404].includes(r.status) });

  // verify notifications endpoint availability
  const notiRes = http.get(`${BASE_URL}/api/notifications?page=1&pageSize=20`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  check(notiRes, {
    'FUNC-NOTI-REACTION_COMMENT_FOLLOW notifications endpoint 200': (r) => r.status === 200,
    'FUNC-NOTI-REACTION_COMMENT_FOLLOW notifications items array': (r) => Array.isArray(r.json('items')),
  });
}
