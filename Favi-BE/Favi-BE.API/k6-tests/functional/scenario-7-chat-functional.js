// Functional chat/messaging: two-user DM flow — create DM, send messages bidirectionally,
// get conversations, get messages, mark as read. Validates message-read correctness parity.
import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, login, authHeaders, seedUsers } from './common.js';

export const options = {
  scenarios: {
    func_chat: {
      executor: 'per-vu-iterations',
      vus: 1,
      iterations: 1,
      maxDuration: '3m',
    },
  },
};

export default function () {
  const userA = seedUsers[0];
  const userB = seedUsers[1];

  if (!userA || !userB) {
    throw new Error('Need at least 2 seeded users for chat functional test');
  }

  const { token: tokenA } = login(userA.username, userA.password || '123456');
  const { token: tokenB } = login(userB.username, userB.password || '123456');

  if (!tokenA || !tokenB) {
    throw new Error('Login failed');
  }

  const hA = authHeaders(tokenA);
  const hB = authHeaders(tokenB);

  // ── A creates DM with B ──────────────────────────────────────────────────────
  const dmRes = http.post(
    `${BASE_URL}/api/chat/dm`,
    JSON.stringify({ otherProfileId: userB.profile_id }),
    { headers: hA }
  );
  check(dmRes, {
    'FUNC-CHAT-01 DM create/get status 200': (r) => r.status === 200,
    'FUNC-CHAT-01 DM returns id': (r) => r.json('id') !== null,
    'FUNC-CHAT-01 DM members count >= 2': (r) => (r.json('members') || []).length >= 2,
  });

  const convId = dmRes.json('id');
  if (!convId) return;

  // ── B opens the same DM (idempotent) ────────────────────────────────────────
  const dmResB = http.post(
    `${BASE_URL}/api/chat/dm`,
    JSON.stringify({ otherProfileId: userA.profile_id }),
    { headers: hB }
  );
  check(dmResB, {
    'FUNC-CHAT-02 B opens same DM status 200': (r) => r.status === 200,
    'FUNC-CHAT-02 same conversation id': (r) => r.json('id') === convId,
  });

  // ── A sends 2 messages ───────────────────────────────────────────────────────
  const msgA1 = http.post(
    `${BASE_URL}/api/chat/${convId}/messages`,
    JSON.stringify({ content: `FUNC-CHAT-A1-${__ITER}`, mediaUrl: null, postId: null }),
    { headers: hA }
  );
  const msgA2 = http.post(
    `${BASE_URL}/api/chat/${convId}/messages`,
    JSON.stringify({ content: `FUNC-CHAT-A2-${__ITER}`, mediaUrl: null, postId: null }),
    { headers: hA }
  );
  check(msgA1, {
    'FUNC-CHAT-03 A sends msg1 status 200': (r) => r.status === 200,
    'FUNC-CHAT-03 msg1 has id': (r) => !!r.json('id'),
    'FUNC-CHAT-03 msg1 senderId matches A': (r) => r.json('senderId') === userA.profile_id,
  });
  check(msgA2, {
    'FUNC-CHAT-03 A sends msg2 status 200': (r) => r.status === 200,
  });

  const lastMsgId = msgA2.json('id');

  // ── B replies ────────────────────────────────────────────────────────────────
  const msgB = http.post(
    `${BASE_URL}/api/chat/${convId}/messages`,
    JSON.stringify({ content: `FUNC-CHAT-B-reply-${__ITER}`, mediaUrl: null, postId: null }),
    { headers: hB }
  );
  check(msgB, {
    'FUNC-CHAT-04 B reply status 200': (r) => r.status === 200,
  });

  // ── A reads conversation messages ────────────────────────────────────────────
  const msgsRes = http.get(
    `${BASE_URL}/api/chat/${convId}/messages?page=1&pageSize=20`,
    { headers: hA }
  );
  check(msgsRes, {
    'FUNC-CHAT-05 get messages 200': (r) => r.status === 200,
    'FUNC-CHAT-05 messages items array': (r) => Array.isArray(r.json('items')),
    'FUNC-CHAT-05 total > 0': (r) => r.json('total') > 0,
    'FUNC-CHAT-05 messages count >= 3': (r) => (r.json('items') || []).length >= 3,
  });

  // ── B marks as read up to lastMsgId ─────────────────────────────────────────
  if (lastMsgId) {
    const readRes = http.post(
      `${BASE_URL}/api/chat/${convId}/read`,
      JSON.stringify(lastMsgId),
      { headers: hB }
    );
    check(readRes, {
      'FUNC-CHAT-06 mark as read status 204': (r) => r.status === 204,
    });
  }

  // ── Both users get conversations list ───────────────────────────────────────
  const convsA = http.get(`${BASE_URL}/api/chat/conversations?page=1&pageSize=20`, { headers: hA });
  const convsB = http.get(`${BASE_URL}/api/chat/conversations?page=1&pageSize=20`, { headers: hB });

  check(convsA, {
    'FUNC-CHAT-07 A conversations 200': (r) => r.status === 200,
    'FUNC-CHAT-07 A conversation list includes DM': (r) =>
      (r.json() || []).some((c) => c.id === convId),
  });
  check(convsB, {
    'FUNC-CHAT-07 B conversations 200': (r) => r.status === 200,
    'FUNC-CHAT-07 B conversation list includes DM': (r) =>
      (r.json() || []).some((c) => c.id === convId),
  });

  // ── Reject send with no content ──────────────────────────────────────────────
  const emptyMsg = http.post(
    `${BASE_URL}/api/chat/${convId}/messages`,
    JSON.stringify({ content: null, mediaUrl: null, postId: null }),
    { headers: hA }
  );
  check(emptyMsg, {
    'FUNC-CHAT-08 empty message rejected 400': (r) => r.status === 400,
  });

  // ── Unauthorized user cannot read messages ───────────────────────────────────
  const randomGuid = '00000000-0000-0000-0000-000000000099';
  const { token: tokenC } = login(seedUsers[2]?.username || userA.username, seedUsers[2]?.password || '123456');
  if (tokenC && seedUsers[2] && seedUsers[2].profile_id !== userA.profile_id && seedUsers[2].profile_id !== userB.profile_id) {
    const hC = authHeaders(tokenC);
    const unauthorizedRead = http.get(
      `${BASE_URL}/api/chat/${convId}/messages?page=1&pageSize=20`,
      { headers: hC }
    );
    check(unauthorizedRead, {
      'FUNC-CHAT-09 non-member gets empty result or 403': (r) =>
        r.status === 403 || (r.status === 200 && r.json('total') === 0),
    });
  }
}
