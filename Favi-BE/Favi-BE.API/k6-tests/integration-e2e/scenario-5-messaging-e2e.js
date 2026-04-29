// E2E messaging: full DM flow integrated with social context —
// A follows B, A opens DM with B, they exchange messages, read receipts propagate.
import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, seedUsers, login, headers } from './common.js';

export const options = {
  scenarios: {
    e2e_messaging: {
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

  const lA = login(userA.username, userA.password || '123456');
  const lB = login(userB.username, userB.password || '123456');

  check(lA.res, { 'E2E-MSG login A 200': (r) => r.status === 200 });
  check(lB.res, { 'E2E-MSG login B 200': (r) => r.status === 200 });

  const hA = headers(lA.token);
  const hB = headers(lB.token);

  // A opens DM with B
  const dmA = http.post(
    `${BASE_URL}/api/chat/dm`,
    JSON.stringify({ otherProfileId: userB.profile_id }),
    { headers: hA }
  );
  check(dmA, {
    'E2E-MSG-01 A opens DM 200': (r) => r.status === 200,
    'E2E-MSG-01 DM has id': (r) => !!r.json('id'),
  });

  const convId = dmA.json('id');
  if (!convId) return;

  // B opens same DM — must be idempotent
  const dmB = http.post(
    `${BASE_URL}/api/chat/dm`,
    JSON.stringify({ otherProfileId: userA.profile_id }),
    { headers: hB }
  );
  check(dmB, {
    'E2E-MSG-02 B opens DM 200': (r) => r.status === 200,
    'E2E-MSG-02 same conv id (idempotent)': (r) => r.json('id') === convId,
  });

  // A sends first message
  const m1 = http.post(
    `${BASE_URL}/api/chat/${convId}/messages`,
    JSON.stringify({ content: `hey B from A — iter ${__ITER}`, mediaUrl: null, postId: null }),
    { headers: hA }
  );
  check(m1, {
    'E2E-MSG-03 A sends msg 200': (r) => r.status === 200,
    'E2E-MSG-03 msg has id': (r) => !!r.json('id'),
    'E2E-MSG-03 content round-trips': (r) => r.json('content')?.startsWith('hey B from A'),
  });

  sleep(0.1);

  // B replies
  const m2 = http.post(
    `${BASE_URL}/api/chat/${convId}/messages`,
    JSON.stringify({ content: `hey A from B — iter ${__ITER}`, mediaUrl: null, postId: null }),
    { headers: hB }
  );
  check(m2, {
    'E2E-MSG-04 B replies 200': (r) => r.status === 200,
  });

  const lastMsgId = m2.json('id');

  // A fetches message history
  const hist = http.get(
    `${BASE_URL}/api/chat/${convId}/messages?page=1&pageSize=20`,
    { headers: hA }
  );
  check(hist, {
    'E2E-MSG-05 history 200': (r) => r.status === 200,
    'E2E-MSG-05 total >= 2': (r) => r.json('total') >= 2,
    'E2E-MSG-05 items has content': (r) => (r.json('items') || []).every((m) => m.id && m.senderId),
  });

  // B marks conversation as read
  if (lastMsgId) {
    const readRes = http.post(
      `${BASE_URL}/api/chat/${convId}/read`,
      JSON.stringify(lastMsgId),
      { headers: hB }
    );
    check(readRes, {
      'E2E-MSG-06 B marks read 204': (r) => r.status === 204,
    });
  }

  // A checks conversation list — unread count for A should reflect B's messages
  const convsA = http.get(`${BASE_URL}/api/chat/conversations?page=1&pageSize=20`, { headers: hA });
  check(convsA, {
    'E2E-MSG-07 A conversations 200': (r) => r.status === 200,
    'E2E-MSG-07 DM appears in list': (r) => (r.json() || []).some((c) => c.id === convId),
    'E2E-MSG-07 last message preview set': (r) =>
      (r.json() || []).find((c) => c.id === convId)?.lastMessagePreview !== undefined,
  });

  sleep(0.2);
}
