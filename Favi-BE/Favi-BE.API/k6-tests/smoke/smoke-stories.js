// Smoke test: stories — feed, profile stories, count, single story, record view, viewers.
// Story creation (multipart image upload) is covered by functional tests, not smoke.
// Seeded stories use loremflickr.com URLs written directly to DB — no Cloudinary dependency.
import http from 'k6/http';
import { check } from 'k6';
import { parse } from 'https://jslib.k6.io/papaparse/5.1.1/index.js';
import { BASE_URL, login, seedUsers, authHeaders } from './common.js';

export const options = {
  thresholds: {
    http_req_duration: ['p(95)<3000'],
    http_req_failed: ['rate<0.05'],
  },
};

const SEED_OUTPUT_DIR = __ENV.SEED_OUTPUT_DIR || '../../seed-output';

function loadStoriesCsv() {
  const candidates = [
    `${SEED_OUTPUT_DIR}/stories.csv`,
    `../../seed-output/stories.csv`,
    `./seed-output/stories.csv`,
    `seed-output/stories.csv`,
  ];
  for (const path of candidates) {
    try {
      return parse(open(path), { header: true, skipEmptyLines: true }).data;
    } catch (_) {}
  }
  return [];
}

const seedStories = loadStoriesCsv();

export default function () {
  const actor = seedUsers[0];
  const other = seedUsers[1];

  if (!actor) {
    throw new Error('Need at least 1 seeded user for stories smoke test');
  }

  const { token } = login(actor.username, actor.password || '123456');
  if (!token) {
    throw new Error('Login failed for actor');
  }

  const h = authHeaders(token);
  const profileId = actor.profile_id;

  // 1. GET /stories/feed — requires auth, returns grouped stories from followings
  const feedRes = http.get(`${BASE_URL}/api/stories/feed`, { headers: h });
  check(feedRes, {
    'SMOKE-STORIES feed status 200': (r) => r.status === 200,
    'SMOKE-STORIES feed is array': (r) => Array.isArray(r.json()),
  });

  // 2. GET /stories/archived — requires auth, returns own archived stories
  const archivedRes = http.get(`${BASE_URL}/api/stories/archived`, { headers: h });
  check(archivedRes, {
    'SMOKE-STORIES archived status 200': (r) => r.status === 200,
    'SMOKE-STORIES archived is array': (r) => Array.isArray(r.json()),
  });

  // 3. GET /stories/profile/{profileId} — returns active stories for a profile
  if (profileId) {
    const profileStoriesRes = http.get(`${BASE_URL}/api/stories/profile/${profileId}`, { headers: h });
    check(profileStoriesRes, {
      'SMOKE-STORIES profile stories status 200': (r) => r.status === 200,
      'SMOKE-STORIES profile stories is array': (r) => Array.isArray(r.json()),
    });

    // 4. GET /stories/profile/{profileId}/count — public endpoint
    const countRes = http.get(`${BASE_URL}/api/stories/profile/${profileId}/count`);
    check(countRes, {
      'SMOKE-STORIES count status 200': (r) => r.status === 200,
      'SMOKE-STORIES count is number': (r) => typeof r.json() === 'number',
    });
  }

  // 5. Use a seeded story ID (from stories.csv) owned by actor for GET + viewers
  const actorStory = seedStories.find((s) => s.profile_id === profileId);
  if (actorStory) {
    const storyRes = http.get(`${BASE_URL}/api/stories/${actorStory.story_id}`, { headers: h });
    check(storyRes, {
      'SMOKE-STORIES get by id status 200': (r) => r.status === 200,
      'SMOKE-STORIES get by id has id field': (r) => r.json('id') !== undefined,
    });

    // GET viewers — actor is owner, should return 200
    const viewersRes = http.get(`${BASE_URL}/api/stories/${actorStory.story_id}/viewers`, { headers: h });
    check(viewersRes, {
      'SMOKE-STORIES viewers status 200': (r) => r.status === 200,
      'SMOKE-STORIES viewers is array': (r) => Array.isArray(r.json()),
    });
  }

  // 6. Record a view on another user's story (actor views other's story)
  if (other?.profile_id) {
    const countRes = http.get(`${BASE_URL}/api/stories/profile/${other.profile_id}/count`);
    check(countRes, {
      'SMOKE-STORIES other profile count status 200': (r) => r.status === 200,
    });

    const otherStory = seedStories.find((s) => s.profile_id === other.profile_id && s.privacy === 'Public');
    if (otherStory) {
      const viewRes = http.post(
        `${BASE_URL}/api/stories/${otherStory.story_id}/view`,
        null,
        { headers: h }
      );
      check(viewRes, {
        'SMOKE-STORIES record view status 200': (r) => r.status === 200,
      });

      // Viewing non-owner story → should get 403 on viewers endpoint
      const viewersRes = http.get(`${BASE_URL}/api/stories/${otherStory.story_id}/viewers`, { headers: h });
      check(viewersRes, {
        'SMOKE-STORIES non-owner viewers status 403': (r) => r.status === 403,
      });
    }
  }
}
