# Smoke Tests

This directory contains smoke tests for the Favi-BE API. Smoke tests are designed to quickly verify that the system is operational and that critical functionality is working as expected.

## Test Scenarios

The following smoke test scenarios are implemented:

1. **SMOKE-AUTH-LOGIN-SINGLE_USER**
   - Verify that login functionality works and returns a valid token.

2. **SMOKE-FEED-READ-SINGLE_USER**
   - Verify that feed endpoints return data and pagination works.

3. **SMOKE-POST-CREATE_DELETE-SINGLE_USER**
   - Verify the lifecycle of a post (create, read, delete).

4. **SMOKE-REACTION-LIKE-SINGLE_USER**
   - Verify that liking a post updates the reaction count and state correctly.

5. **SMOKE-COMMENT-CREATE-SINGLE_USER**
   - Verify that comments can be created and retrieved.

6. **SMOKE-PROFILE-PRIVACY-READ-SINGLE_USER**
   - Verify that profile privacy settings are respected.

## Running the Tests

To run the smoke tests, use the following command:

```bash
k6 run <test-file.js>
```

Replace `<test-file.js>` with the specific test file you want to execute.