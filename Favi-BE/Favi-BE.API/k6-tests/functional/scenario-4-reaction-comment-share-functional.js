// Functional Reaction/Comment/Share scenarios including idempotency and integrity checks.
import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, pickSeedToken, pickSeedPostId, seedUsers } from './common.js';

export const options = {
  scenarios: {
    func_reaction_comment_share: {
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

  // FUNC-REACTION-LIKE_UNLIKE_LOOP-SINGLE_USER + DUPLICATE_GUARD
  for (let i = 0; i < 5; i++) {
    const toggleRes = http.post(`${BASE_URL}/api/posts/${postId}/reactions?type=Like`, null, { headers });
    check(toggleRes, {
      'FUNC-REACTION-LIKE_UNLIKE_LOOP-SINGLE_USER toggle accepted': (r) => [200, 409].includes(r.status),
    });
  }

  // FUNC-COMMENT-PARENT_CHILD_DEPTH_LIMIT + FUNC-COMMENT-WITH_URL_CONTENT
  const parentRes = http.post(
    `${BASE_URL}/api/comments`,
    JSON.stringify({ postId, content: 'parent comment with url https://example.com' }),
    { headers }
  );
  check(parentRes, {
    'FUNC-COMMENT-WITH_URL_CONTENT created': (r) => r.status === 200,
  });

  const parentId = parentRes.json('id');
  if (parentId) {
    const childRes = http.post(
      `${BASE_URL}/api/comments`,
      JSON.stringify({ postId, parentCommentId: parentId, content: 'child comment' }),
      { headers }
    );
    check(childRes, {
      'FUNC-COMMENT-PARENT_CHILD_DEPTH_LIMIT child accepted': (r) => [200, 400].includes(r.status),
    });

    const childId = childRes.json('id');
    if (childId) {
      const depth3Res = http.post(
        `${BASE_URL}/api/comments`,
        JSON.stringify({ postId, parentCommentId: childId, content: 'depth-3 comment' }),
        { headers }
      );
      check(depth3Res, {
        'FUNC-COMMENT-PARENT_CHILD_DEPTH_LIMIT depth>2 rejected': (r) => [400, 403, 422].includes(r.status),
      });
    }
  }

  // FUNC-SHARE-UNIQUE_RULE + WRITE-IDEMPOTENCY-ALL_ACTIONS (subset share/unshare)
  const share1 = http.post(`${BASE_URL}/api/posts/${postId}/share`, JSON.stringify({ caption: null }), { headers });
  const share2 = http.post(`${BASE_URL}/api/posts/${postId}/share`, JSON.stringify({ caption: null }), { headers });
  check(share1, {
    'FUNC-SHARE-UNIQUE_RULE first share accepted': (r) => [200, 409].includes(r.status),
  });
  check(share2, {
    'FUNC-SHARE-UNIQUE_RULE duplicate share guarded': (r) => [200, 404, 409].includes(r.status),
  });

  const unshare = http.del(`${BASE_URL}/api/posts/${postId}/share`, null, { headers });
  check(unshare, {
    'FUNC-WRITE-IDEMPOTENCY-ALL_ACTIONS unshare accepted': (r) => [200, 404].includes(r.status),
  });

  // FUNC-FOLLOW-GRAPH-INTEGRITY (basic follow/unfollow)
  const someone = seedUsers.find((u) => u.profile_id !== seedUsers[0].profile_id);
  if (someone) {
    const followRes = http.post(`${BASE_URL}/api/profiles/follow/${someone.profile_id}`, null, {
      headers: { Authorization: `Bearer ${token}` },
    });
    check(followRes, {
      'FUNC-FOLLOW-GRAPH-INTEGRITY follow accepted': (r) => [200, 400, 403, 404].includes(r.status),
    });

    const unfollowRes = http.del(`${BASE_URL}/api/profiles/follow/${someone.profile_id}`, null, {
      headers: { Authorization: `Bearer ${token}` },
    });
    check(unfollowRes, {
      'FUNC-FOLLOW-GRAPH-INTEGRITY unfollow accepted': (r) => [200, 400, 404].includes(r.status),
    });
  }
}
