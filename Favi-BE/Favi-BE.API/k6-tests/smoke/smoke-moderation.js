// Smoke test: moderation — create report (user), resolve report (admin), ban/unban user (admin), audit log.
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
  const admin    = seedUsers[0];   // user_00001 — role=Admin in seed CSV
  const reporter = seedUsers[1];
  const target   = seedUsers[2];

  if (!reporter || !target || !admin) {
    throw new Error('Need at least 3 seeded users for moderation smoke test');
  }

  const { token: reporterToken } = login(reporter.username, reporter.password || '123456');
  const { token: adminToken }    = login(admin.username,    admin.password    || '123456');

  if (!reporterToken || !adminToken) {
    throw new Error('Login failed for reporter or admin');
  }

  const rh = authHeaders(reporterToken);
  const ah = authHeaders(adminToken);

  // 1. User creates a report
  const createRes = http.post(
    `${BASE_URL}/api/reports`,
    JSON.stringify({
      reporterProfileId: reporter.profile_id,
      targetType: 'User',
      targetId:   target.profile_id,
      reason:     'smoke-test report',
    }),
    { headers: rh }
  );
  check(createRes, {
    'SMOKE-MOD create report status 200': (r) => r.status === 200,
    'SMOKE-MOD create report returns id': (r) => r.json('id') !== undefined,
  });

  const reportId = createRes.status === 200 ? createRes.json('id') : null;

  // 2. User gets own reports
  const myReportsRes = http.get(`${BASE_URL}/api/reports/my?page=1&pageSize=20`, { headers: rh });
  check(myReportsRes, {
    'SMOKE-MOD my reports status 200': (r) => r.status === 200,
    'SMOKE-MOD my reports has items':  (r) => Array.isArray(r.json('items')),
  });

  // 3. Admin lists all reports
  const adminReportsRes = http.get(`${BASE_URL}/api/admin/reports?page=1&pageSize=20`, { headers: ah });
  check(adminReportsRes, {
    'SMOKE-MOD admin list reports status 200': (r) => r.status === 200,
    'SMOKE-MOD admin list reports has items':  (r) => Array.isArray(r.json('items')),
  });

  // 4. Admin resolves the report (if created)
  if (reportId) {
    const resolveRes = http.post(
      `${BASE_URL}/api/admin/reports/${reportId}/resolve`,
      null,
      { headers: ah }
    );
    check(resolveRes, {
      'SMOKE-MOD resolve report status 200': (r) => r.status === 200,
    });
  }

  // 5. Admin bans a user
  const banRes = http.post(
    `${BASE_URL}/api/admin/users/${target.profile_id}/ban`,
    JSON.stringify({ reason: 'smoke-test ban', durationDays: 1 }),
    { headers: ah }
  );
  check(banRes, {
    'SMOKE-MOD ban user status 200':     (r) => r.status === 200,
    'SMOKE-MOD ban user returns id':     (r) => r.json('id') !== undefined,
    'SMOKE-MOD ban user active is true': (r) => r.json('active') === true,
  });

  // 6. Admin unbans the user
  const unbanRes = http.del(
    `${BASE_URL}/api/admin/users/${target.profile_id}/ban`,
    JSON.stringify({ reason: 'smoke-test unban' }),
    { headers: ah }
  );
  check(unbanRes, {
    'SMOKE-MOD unban user status 200': (r) => r.status === 200,
  });

  // 7. Admin reads audit log
  const auditRes = http.get(`${BASE_URL}/api/admin/audit?page=1&pageSize=20`, { headers: ah });
  check(auditRes, {
    'SMOKE-MOD audit log status 200':  (r) => r.status === 200,
    'SMOKE-MOD audit log has items':   (r) => Array.isArray(r.json('items')),
  });

  // 8. Admin reads ban history for target user
  const historyRes = http.get(
    `${BASE_URL}/api/admin/users/${target.profile_id}/ban-history?page=1&pageSize=20`,
    { headers: ah }
  );
  check(historyRes, {
    'SMOKE-MOD ban history status 200': (r) => r.status === 200,
    'SMOKE-MOD ban history has bans':   (r) => Array.isArray(r.json('bans')),
  });
}
