# Module Boundary Enforcement

## 1. Boundary rules
1. API depends on module facades/contracts only.
2. Module internal namespaces are not referenced cross-module.
3. Cross-module communication via integration events + outbox/inbox.

## 2. Facade contracts
- `IIdentityFacade`
- `ISocialGraphFacade`
- `IContentPublishingFacade`
- `IEngagementFacade`
- `INotificationsFacade`
- `IStoriesFacade`
- `IMessagingFacade`
- `IModerationFacade`

## 3. Dependency matrix
| From \ To | Identity | SocialGraph | Content | Engagement | Notifications | Stories | Messaging | Moderation |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Identity | ✅ | ❌ direct | ❌ direct | ❌ direct | ❌ direct | ❌ direct | ❌ direct | ✅ via contracts |
| SocialGraph | ✅ contract | ✅ | ❌ direct | ❌ direct | ✅ via events | ❌ | ❌ | ❌ |
| Content | ✅ contract | ❌ | ✅ | ✅ via events | ✅ via events | ❌ | ❌ | ✅ via events |
| Engagement | ✅ contract | ❌ | ✅ contract | ✅ | ✅ via events | ❌ | ❌ | ✅ via events |
| Notifications | ✅ contract | ✅ via events | ✅ via events | ✅ via events | ✅ | ✅ via events | ✅ via events | ✅ via events |
| Stories | ✅ contract | ❌ | ❌ | ❌ | ✅ via events | ✅ | ❌ | ✅ via events |
| Messaging | ✅ contract | ❌ | ❌ | ❌ | ✅ via events | ❌ | ✅ | ✅ via events |
| Moderation | ✅ contract | ✅ via events | ✅ via events | ✅ via events | ✅ via events | ✅ via events | ✅ via events | ✅ |

## 4. Architecture tests
- Use NetArchTest:
  - `Domain` cannot reference `Infrastructure`.
  - `Application` cannot reference other module internals.
  - `API` cannot reference repositories directly.
- Add CI gate fail when test fails.

## 5. Governance
- PR checklist requires boundary confirmation.
- Any exception must include ADR/decision log.