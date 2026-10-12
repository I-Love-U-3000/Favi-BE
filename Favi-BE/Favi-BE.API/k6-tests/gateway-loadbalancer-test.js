import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

export const errorRate = new Rate('gateway_errors');
export const latencyTrend = new Trend('gateway_latency_ms');
export const rateLimitHits = new Counter('rate_limit_hits_429');

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export const options = {
  scenarios: {
    // 1. Gateway Routing & Distribution
    cluster_distribution: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '15s', target: 20 },
        { duration: '30s', target: 50 },
        { duration: '15s', target: 0 },
      ],
      gracefulRampDown: '5s',
    },
    // 2. High burst to verify Rate Limiter
    rate_limiter_burst: {
      executor: 'constant-arrival-rate',
      rate: 30, // 30 req/s to trigger rate limit on auth
      timeUnit: '1s',
      duration: '10s',
      preAllocatedVUs: 20,
      maxVUs: 50,
      startTime: '65s',
    },
  },
  thresholds: {
    'http_req_duration{scenario:cluster_distribution}': ['p(95)<250', 'p(99)<500'],
    'gateway_errors{scenario:cluster_distribution}': ['rate<0.01'],
  },
};

export default function () {
  // Test 1: Health Liveness
  const liveRes = http.get(`${BASE_URL}/health/live`);
  check(liveRes, {
    'health live status is 200': (r) => r.status === 200,
    'has X-Request-ID header': (r) => r.headers['X-Request-Id'] !== undefined,
  });

  // Test 2: General API Feed / Posts or Public Health
  const healthRes = http.get(`${BASE_URL}/health`);
  const isOk = check(healthRes, {
    'gateway routed successfully': (r) => r.status === 200,
  });
  if (!isOk) {
    errorRate.add(1);
  } else {
    errorRate.add(0);
  }
  latencyTrend.add(healthRes.timings.duration);

  // Test 3: Static Seed Assets through Gateway
  const assetRes = http.get(`${BASE_URL}/seed-assets/demo.png`);
  check(assetRes, {
    'seed-asset routed away from api': (r) => r.status === 200 || r.status === 404,
  });

  // Test 4: Rate Limiting Probe on Auth
  const authRes = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ emailOrUsername: 'loadtest@favi.local', password: 'wrongpassword' }),
    { headers: { 'Content-Type': 'application/json' } }
  );
  if (authRes.status === 429) {
    rateLimitHits.add(1);
  }

  sleep(0.5);
}
