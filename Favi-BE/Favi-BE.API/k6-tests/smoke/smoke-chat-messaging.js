// Smoke test: chat/messaging — DM create, send message, get conversations, get messages, mark as read.
import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, login, seedUsers, authHeaders } from './common.js';

export const options = {
  thresholds: {
    http_req_duration: ['p(95)<3000'],
    http_req_failed: ['rate<0.05'],
  },
};

export default function () {
  const actor = seedUsers[0];
  const other = seedUsers[1];

  if (!actor || !other) {
    throw new Error('Need at least 2 seeded users for chat smoke test');
  }

  const { token: actorToken } = login(actor.username, actor.password || '123456');
  if (!actorToken) {
    throw new Error('Login failed for actor');
  }

  const h = authHeaders(actorToken);

  // 1. Get or create DM with other user
  const dmRes = http.post(
    `${BASE_URL}/api/chat/dm`,
    JSON.stringify({ otherProfileId: other.profile_id }),
    { headers: h }
  );
  check(dmRes, {
    'SMOKE-CHAT dm create/get status 200': (r) => r.status === 200,
    'SMOKE-CHAT dm returns id': (r) => r.json('id') !== undefined,
  });

  const conversationId = dmRes.status === 200 ? dmRes.json('id') : null;
  if (!conversationId) return;

  // 2. Send a message
  const sendRes = http.post(
    `${BASE_URL}/api/chat/${conversationId}/messages`,
    JSON.stringify({ content: `smoke-msg-${Date.now()}`, mediaUrl: null, postId: null }),
    { headers: h }
  );
  check(sendRes, {
    'SMOKE-CHAT send message status 200': (r) => r.status === 200,
    'SMOKE-CHAT send message returns id': (r) => r.json('id') !== undefined,
  });

  const messageId = sendRes.status === 200 ? sendRes.json('id') : null;

  // 3. Get conversations list
  const convsRes = http.get(`${BASE_URL}/api/chat/conversations?page=1&pageSize=20`, { headers: h });
  check(convsRes, {
    'SMOKE-CHAT conversations status 200': (r) => r.status === 200,
    'SMOKE-CHAT conversations is array': (r) => Array.isArray(r.json()),
  });

  // 4. Get messages in conversation
  const msgsRes = http.get(
    `${BASE_URL}/api/chat/${conversationId}/messages?page=1&pageSize=20`,
    { headers: h }
  );
  check(msgsRes, {
    'SMOKE-CHAT get messages status 200': (r) => r.status === 200,
    'SMOKE-CHAT get messages has items': (r) => Array.isArray(r.json('items')),
  });

  // 5. Mark as read (if we have a message)
  if (messageId) {
    const readRes = http.post(
      `${BASE_URL}/api/chat/${conversationId}/read`,
      JSON.stringify(messageId),
      { headers: h }
    );
    check(readRes, {
      'SMOKE-CHAT mark as read status 204': (r) => r.status === 204,
    });
  }
}
