# Go + Python Backend Refactor API Contract Baseline

This document freezes the current backend surface that the Go + Python refactor must preserve. It is a working baseline, not a replacement for generated OpenAPI diff tests.

## 1. Baseline Scan

Generated on 2026-05-07 from `src/Services` and `src/Gateway`.

```bash
find src/Services src/Gateway -type f \( -name '*Controller.cs' -o -name 'Program.cs' -o -name '*Hub.cs' \) \
  | sort \
  | while IFS= read -r file; do
      printf '\n## %s\n' "$file"
      grep -nE '\[Route\(|\[Http(Get|Post|Put|Delete|Patch)|Map(Get|Post|Put|Delete|Patch)|MapHub|MapGroup|MapHealthChecks|MapControllers|MapReverseProxy' "$file"
    done
```

Counts:

- Services: 13
- Controllers: 68
- HTTP action attributes: 514
- Hubs: 5

## 2. Gateway Contract

### Health and Proxy

| Method | Path | Behavior |
| --- | --- | --- |
| GET | `/health` | Returns healthy gateway payload. |
| Any | matching service path | Reverse proxy to configured upstream. |

### Gateway Route Prefixes

| Service | Prefixes |
| --- | --- |
| city-service | `/api/v1/admin/city-reviews`, `/api/v1/admin/pros-cons`, `/api/v1/admin/moderators`, `/api/v1/admin/moderator-applications`, `/api/v1/user-favorite-cities`, `/api/v1/user-content/pros-cons`, `/api/v1/cities/{cityId}/user-content/**`, `/api/v1/cities`, `/api/v1/countries`, `/api/v1/provinces` |
| cache-service | `/api/v1/cache` |
| user-service | `/api/v1/admin/membership`, `/api/v1/admin/legal`, `/api/v1/admin/audit/events`, `/api/v1/auth`, `/api/v1/reports`, `/api/v1/users`, `/api/v1/travel-history`, `/api/v1/visited-places`, `/api/v1/membership`, `/api/v1/payments`, `/api/v1/roles`, `/api/v1/skills`, `/api/v1/interests`, `/api/v1/profile-snapshot` |
| event-service | `/api/v1/event-types`, `/api/v1/events`, `/hubs/meetup` |
| ai-service | `/api/v1/admin/travel-plans`, `/api/v1/admin/community`, `/api/v1/admin/ai`, `/api/v1/ai`, `/api/v1/migration-workspace`, `/api/v1/explore-dashboard`, `/api/v1/land-hub`, `/api/v1/community-snapshot`, `/api/v1/community`, `/api/v1/budgets`, `/api/v1/visa` |
| coworking-service | `/api/v1/coworking`, `/api/v1/coworking-spaces` |
| search-service | `/api/v1/search`, `/api/v1/index` |
| accommodation-service | `/api/v1/admin/hotel-reviews`, `/api/v1/hotels` |
| product-service | `/api/v1/products` |
| message-service | `/api/v1/admin/notifications`, `/api/v1/admin/chats`, `/api/v1/im`, `/api/v1/notifications`, `/api/v1/chats`, `/hubs/chat`, `/hubs/notifications`, `/hubs/ai-progress`, `/api/v1/inbox` |
| innovation-service | `/api/innovations`, `/api/v1/innovations`, `/api/v1/innovation-projects` |
| config-service | `/api/v1/admin/static-texts`, `/api/v1/admin/option-groups`, `/api/v1/admin/config`, `/api/v1/app/config` |

### Authentication Rules

- Fully public route prefixes: `/api/v1/auth/login`, `/api/v1/auth/register`, `/api/v1/auth/forgot-password`, `/api/v1/auth/refresh`, `/api/v1/auth/logout`, `/api/v1/auth/social-login`, `/api/v1/auth/alipay/auth-info`, `/api/v1/auth/sms/send`, `/api/v1/auth/sms/login`, legacy `/api/users/login`, `/api/users/register`, `/api/users/refresh`, `/api/test`, `/health`, `/metrics`, `/scalar/v1`.
- Public GET prefixes: `/api/v1/cities`, `/api/v1/hotels`, `/api/v1/coworking`, `/api/v1/products`, `/api/v1/search`, `/api/v1/index`, `/api/v1/users/legal`, `/api/v1/app/config`.
- Admin prefixes: `/api/users/admin`, `/api/v1/admin/`, `/api/v1/reports`.

## 3. Service API Inventory

Notation: `BASE` is the controller route prefix.

### AccommodationService

`BASE /api/v1/admin/hotel-reviews`

- GET `BASE`
- DELETE `BASE/{id:guid}`

`BASE /api/v1/hotels`

- GET `BASE`
- GET `BASE/city/{cityId:guid}`
- GET `BASE/{id:guid}`
- POST `BASE`
- PUT `BASE/{id:guid}`
- DELETE `BASE/{id:guid}`
- GET `BASE/my`
- GET `BASE/{hotelId:guid}/rooms`
- GET `BASE/rooms/{roomTypeId:guid}`
- POST `BASE/{hotelId:guid}/rooms`
- PUT `BASE/rooms/{roomTypeId:guid}`
- DELETE `BASE/rooms/{roomTypeId:guid}`
- GET `BASE/{hotelId:guid}/reviews`
- GET `BASE/reviews/{reviewId:guid}`
- GET `BASE/{hotelId:guid}/reviews/mine`
- POST `BASE/{hotelId:guid}/reviews`
- PUT `BASE/reviews/{reviewId:guid}`
- DELETE `BASE/reviews/{reviewId:guid}`
- POST `BASE/reviews/{reviewId:guid}/helpful`
- GET `BASE/{hotelId:guid}/reviews/stats`

### AIService

`BASE /api/v1/admin/ai/conversations`

- GET `BASE`
- GET `BASE/{id:guid}`
- DELETE `BASE/{id:guid}`

`BASE /api/v1/admin/community`

- GET `BASE/posts`
- GET `BASE/posts/{id:guid}`
- PUT `BASE/posts/{id:guid}/status`
- DELETE `BASE/posts/{id:guid}`

`BASE /api/v1/admin/travel-plans`

- GET `BASE`
- GET `BASE/{id:guid}`
- PUT `BASE/{id:guid}/status`
- DELETE `BASE/{id:guid}`

`BASE /api/v1/budgets`

- GET `BASE/current`
- POST `BASE/plans/{planId:guid}`

`BASE /api/v1/ai`

- POST `BASE/conversations`
- GET `BASE/conversations`
- GET `BASE/conversations/{conversationId:guid}`
- PUT `BASE/conversations/{conversationId:guid}`
- DELETE `BASE/conversations/{conversationId:guid}`
- POST `BASE/conversations/{conversationId:guid}/archive`
- POST `BASE/conversations/{conversationId:guid}/activate`
- POST `BASE/conversations/{conversationId:guid}/messages`
- POST `BASE/conversations/{conversationId:guid}/messages/stream`
- POST `BASE/conversations/{conversationId:guid}/messages/signalr-stream`
- GET `BASE/conversations/{conversationId:guid}/messages`
- GET `BASE/stats`
- GET `BASE/health`
- POST `BASE/travel-plan`
- POST `BASE/travel-plan/stream`
- POST `BASE/travel-plan/stream-text`
- POST `BASE/travel-plan/async`
- GET `BASE/travel-plan/tasks/{taskId}`
- GET `BASE/travel-plans/{planId}`
- GET `BASE/travel-plans/{planId:guid}/detail`
- GET `BASE/travel-plans`
- POST `BASE/travel-guide`
- POST `BASE/travel-guide/stream`
- POST `BASE/guide/async`
- POST `BASE/nearby-cities`
- POST `BASE/nearby-cities/stream`
- POST `BASE/nearby-cities/async`
- POST `BASE/images/generate`
- POST `BASE/images/city`
- POST `BASE/images/city/async`
- GET `BASE/images/tasks/{taskId}`

`BASE /api/v1/community`

- POST `BASE/questions`
- POST `BASE/questions/{questionId:guid}/answers`
- POST `BASE/questions/{questionId:guid}/upvote`
- POST `BASE/answers/{answerId:guid}/upvote`

`BASE /api/v1/community-snapshot`

- GET `BASE/current`

`BASE /api/v1/explore-dashboard`

- GET `BASE/current`

`BASE /api/v1/land-hub`

- GET `BASE/current`

`BASE /api/v1/migration-workspace`

- GET `BASE`
- POST `BASE/plans/{planId:guid}/state`

`BASE /api/v1/ai/openclaw`

- POST `BASE/execute`
- POST `BASE/reminder`
- POST `BASE/visa-reminder`
- POST `BASE/automation/{scenario}`
- POST `BASE/research`

`BASE /api/v1/visa/profiles`

- GET `BASE`
- POST `BASE/{planId:guid}`

Hubs and health:

- Hub `/hubs/notifications`
- GET `/health`
- GET `/health/ai`

### CacheService

`BASE /api/v1/cache/costs`

- GET `BASE/city/{cityId}`
- POST `BASE/city/batch`
- PUT `BASE/city/{cityId}`
- DELETE `BASE/city/{cityId}`

`BASE /api/v1/cache/scores`

- GET `BASE/city/{cityId}`
- POST `BASE/city/batch`
- PUT `BASE/city/{cityId}`
- GET `BASE/coworking/{coworkingId}`
- POST `BASE/coworking/batch`
- PUT `BASE/coworking/{coworkingId}`
- DELETE `BASE/city/{cityId}`
- POST `BASE/city/invalidate-batch`
- DELETE `BASE/coworking/{coworkingId}`
- POST `BASE/coworking/invalidate-batch`
- POST `BASE/city/cleanup-zero-scores`

Phase 4c migration scope currently covers only the read-through query subset:

- GET `/api/v1/cache/costs/city/{cityId}`
- POST `/api/v1/cache/costs/city/batch`
- GET `/api/v1/cache/scores/city/{cityId}`
- POST `/api/v1/cache/scores/city/batch`
- GET `/api/v1/cache/scores/coworking/{coworkingId}`
- POST `/api/v1/cache/scores/coworking/batch`
- GET `/health`

The following CacheService routes remain on .NET during Phase 4c:

- PUT `/api/v1/cache/costs/city/{cityId}`
- DELETE `/api/v1/cache/costs/city/{cityId}`
- PUT `/api/v1/cache/scores/city/{cityId}`
- PUT `/api/v1/cache/scores/coworking/{coworkingId}`
- DELETE `/api/v1/cache/scores/city/{cityId}`
- POST `/api/v1/cache/scores/city/invalidate-batch`
- DELETE `/api/v1/cache/scores/coworking/{coworkingId}`
- POST `/api/v1/cache/scores/coworking/invalidate-batch`
- POST `/api/v1/cache/scores/city/cleanup-zero-scores`

### CityService

`BASE /api/v1/admin/city-reviews`

- GET `BASE`
- DELETE `BASE/{id:guid}`

`BASE /api/v1/admin/moderator-applications`

- GET `BASE`
- POST `BASE/{id:guid}/approve`
- POST `BASE/{id:guid}/reject`

`BASE /api/v1/admin/moderators`

- GET `BASE`
- DELETE `BASE/{id:guid}`

`BASE /api/v1/admin/pros-cons`

- GET `BASE`
- PUT `BASE/{id:guid}/hide`
- DELETE `BASE/{id:guid}`

`BASE /api/v1/cities`

- GET `BASE`
- GET `BASE/list`
- GET `BASE/list-basic`
- GET `BASE/region-tabs`
- POST `BASE/counts`
- POST `BASE/lookup`
- POST `BASE/match`
- GET `BASE/recommended`
- GET `BASE/popular`
- GET `BASE/by-country/{countryId:guid}`
- GET `BASE/grouped-by-country`
- GET `BASE/countries`
- GET `BASE/search`
- GET `BASE/{id:guid}`
- GET `BASE/{id:guid}/nomad-summary`
- GET `BASE/{id:guid}/statistics`
- GET `BASE/{id:guid}/weather`
- GET `BASE/{id:guid}/coworking-count`
- POST `BASE`
- PUT `BASE/{id}`
- DELETE `BASE/{id}`
- GET `BASE/with-coworking-count`
- GET `BASE/with-coworking-ids`
- GET `BASE/with-coworking`
- GET `BASE/{cityId}/guide`
- POST `BASE/{cityId}/guide`
- POST `BASE/moderator/assign`
- GET `BASE/{id}/moderators`
- POST `BASE/{id}/moderators`
- DELETE `BASE/{cityId}/moderators/{userId}`
- PATCH `BASE/{cityId}/moderators/{moderatorId}`
- POST `BASE/{cityId:guid}/generate-images`
- GET `BASE/{cityId}/nearby`
- POST `BASE/{cityId}/nearby`
- DELETE `BASE/{cityId}/nearby`

`BASE /api/v1/cities/{cityId}/ratings`

- GET `BASE`
- GET `BASE/statistics`
- POST `/api/v1/cities/ratings/statistics/batch`
- POST `BASE`
- GET `BASE/categories`
- POST `BASE/categories`
- PUT `BASE/categories/{categoryId}`
- DELETE `BASE/categories/{categoryId}`
- POST `BASE/categories/initialize`

`BASE /api/v1/admin/geography`

- POST `BASE/seed/china-provinces`
- POST `BASE/seed/countries`
- POST `BASE/seed/china-default`

`BASE /api/GeoNames`

- POST `BASE/import`
- GET `BASE/search`
- GET `BASE/city/{cityName}`
- POST `BASE/import/country/{countryCode}`

`BASE /api/Metrics`

- GET `BASE/weather`
- GET `BASE/cache/health`
- POST `BASE/reset`
- GET `BASE/prometheus`

`BASE /api/v1/cities/moderator`

- POST `BASE/apply`
- POST `BASE/handle`
- GET `BASE/applications/pending`
- GET `BASE/applications/my`
- GET `BASE/applications/{id}`
- GET `BASE/applications/statistics`
- POST `BASE/revoke`

`BASE /api/v1/cities/moderator/transfers`

- POST `BASE`
- POST `BASE/{transferId}/respond`
- POST `BASE/{transferId}/cancel`
- GET `BASE/initiated`
- GET `BASE/received`
- GET `BASE/pending`
- GET `BASE/{transferId}`

`BASE /api/v1/user/city-content`

- GET `BASE/photos`
- GET `BASE/expenses`
- GET `BASE/reviews/{cityId}`

`BASE /api/v1/cities/{cityId}/user-content`

Phase 4e migration scope currently covers only the city region-tabs read subset:

- GET `/api/v1/cities/region-tabs`
- GET `/health`

The following CityService routes remain on .NET during Phase 4e:

- All `/api/v1/cities/**` routes except `GET /api/v1/cities/region-tabs`
- All `/api/v1/admin/**` city routes
- All `/api/v1/countries/**`, `/api/v1/provinces/**`, `/api/v1/cities/moderator/**`, and city rating/content routes

- POST `BASE/photos`
- POST `BASE/photos/batch`
- GET `BASE/photos`
- POST `BASE/photos/{photoId}/approve`
- POST `BASE/photos/{photoId}/reject`
- DELETE `BASE/photos/{photoId}`
- POST `BASE/expenses`
- GET `BASE/expenses`
- DELETE `BASE/expenses/{expenseId}`
- POST `BASE/reviews`
- GET `BASE/reviews`
- DELETE `BASE/reviews/{reviewId}`
- GET `BASE/stats`
- GET `BASE/cost-summary`
- GET `/api/v1/cities/{cityId}/expenses/statistics`
- POST `BASE/pros-cons`
- GET `BASE/pros-cons`
- PUT `BASE/pros-cons/{id}`
- DELETE `BASE/pros-cons/{id}`

`BASE /api/v1/user-content/pros-cons`

- POST `BASE/{id}/vote`

`BASE /api/v1/user-favorite-cities`

- GET `BASE/check/{cityId}`
- POST `BASE`
- DELETE `BASE/{cityId}`
- POST `BASE/{cityId}/remove`
- GET `BASE/ids`
- GET `BASE/user/{userId}/count`
- GET `BASE`
- GET `BASE/details`

### ConfigService

`BASE /api/v1/admin/config`

- POST `BASE/publish`
- GET `BASE/snapshots`
- GET `BASE/snapshots/{id:guid}`
- POST `BASE/snapshots/{id:guid}/rollback`
- POST `BASE/rollback/{id:guid}`

`BASE /api/v1/admin/option-groups`

- GET `BASE`
- GET `BASE/{id:guid}`
- POST `BASE`
- PUT `BASE/{id:guid}`
- DELETE `BASE/{id:guid}`
- GET `BASE/{groupId:guid}/items`
- POST `BASE/{groupId:guid}/items`
- PUT `BASE/{groupId:guid}/items/{id:guid}`
- DELETE `BASE/{groupId:guid}/items/{id:guid}`
- POST `BASE/{groupId:guid}/items/reorder`
- PUT `BASE/{groupId:guid}/items/{id:guid}/toggle`
- POST `BASE/{id:guid}/toggle`

`BASE /api/v1/admin/static-texts`

- GET `BASE`
- GET `BASE/{id:guid}`
- POST `BASE`
- PUT `BASE/{id:guid}`
- DELETE `BASE/{id:guid}`
- GET `BASE/categories`

`BASE /api/v1/admin/config/system-settings`

- GET `BASE`
- GET `BASE/{id:guid}`
- POST `BASE`
- PUT `BASE/{id:guid}`
- DELETE `BASE/{id:guid}`

`BASE /api/v1/app/config`

- GET `BASE`
- GET `BASE/version`

### CoworkingService

`BASE /api/v1/coworking` and alias `/api/v1/coworking-spaces`

- GET `BASE`
- GET `BASE/city/{cityId}`
- GET `BASE/{id}`
- POST `BASE`
- PUT `BASE/{id}`
- DELETE `BASE/{id}`
- GET `BASE/search`
- GET `BASE/top-rated`
- GET `BASE/{id}/verification-eligibility`
- POST `BASE/{id}/verifications`
- PUT `BASE/{id}/verification-status`
- POST `BASE/{coworkingId}/bookings`
- GET `BASE/bookings/{id}`
- POST `BASE/bookings/{id}/cancel`
- GET `BASE/bookings/user/{userId}`
- POST `BASE/{coworkingId}/comments`
- GET `BASE/{coworkingId}/comments`
- DELETE `BASE/comments/{id}`
- GET `BASE/cities/{cityId}/count`
- POST `BASE/cities/counts`
- GET `BASE/{coworkingId}/reviews`
- GET `BASE/reviews/{reviewId}`
- GET `BASE/{coworkingId}/reviews/my-review`
- POST `BASE/{coworkingId}/reviews`
- PUT `BASE/reviews/{reviewId}`
- DELETE `BASE/reviews/{reviewId}`

### DocumentService

Minimal API groups:

- GET `/api/products/`
- GET `/api/products/{id}`
- GET `/api/products/user/{userId}`
- POST `/api/products/`
- PUT `/api/products/{id}`
- DELETE `/api/products/{id}`
- GET `/api/users/`
- GET `/api/users/{id}`
- POST `/api/users/`
- PUT `/api/users/{id}`
- DELETE `/api/users/{id}`
- GET `/api/system/health`
- GET `/api/system/services`
- GET `/api/system/specs`
- GET `/health`

### EventService

`BASE /api/v1/events`

- POST `BASE`
- GET `BASE/{id}`
- GET `BASE`
- PUT `BASE/{id}`
- POST `BASE/{id}/cancel`
- DELETE `BASE/{id}`
- GET `BASE/joined`
- GET `BASE/cancelled`
- POST `BASE/{id}/join`
- DELETE `BASE/{id}/join`
- POST `BASE/{id}/follow`
- DELETE `BASE/{id}/follow`
- GET `BASE/{id}/participants`
- GET `BASE/{id}/followers`
- GET `BASE/me/created`
- GET `BASE/user/{userId}/created/count`
- GET `BASE/user/{userId}/joined/count`
- GET `BASE/me/joined`
- GET `BASE/me/following`
- POST `BASE/{eventId}/invitations`
- POST `BASE/invitations/{invitationId}/respond`
- GET `BASE/invitations/{invitationId}`
- GET `BASE/invitations/received`
- GET `BASE/invitations/sent`
- POST `BASE/cities/counts`

`BASE /api/v1/event-types`

- GET `BASE`
- GET `BASE/all`
- GET `BASE/{id}`
- POST `BASE`
- PUT `BASE/{id}`
- DELETE `BASE/{id}`

Hubs:

- Hub `/hubs/meetup`

### InnovationService

`BASE /api/innovations`, `/api/v1/innovations`, `/api/v1/innovation-projects`

- GET `BASE`
- GET `BASE/{id:guid}`
- POST `BASE`
- PUT `BASE/{id:guid}`
- DELETE `BASE/{id:guid}`
- GET `BASE/user/{userId:guid}`
- GET `BASE/my`
- GET `BASE/featured`
- GET `BASE/popular`
- POST `BASE/{id:guid}/like`
- GET `BASE/{id:guid}/comments`
- POST `BASE/{id:guid}/comments`
- DELETE `BASE/comments/{commentId:guid}`
- POST `BASE/{id:guid}/team`
- DELETE `BASE/{id:guid}/team/{memberId:guid}`

### MessageService

`BASE /api/v1/admin/chats`

- GET `BASE`
- GET `BASE/{id}`
- DELETE `BASE/{id}`

`BASE /api/v1/admin/notifications`

- GET `BASE`
- GET `BASE/{id:guid}`
- POST `BASE`
- PUT `BASE/{id:guid}`
- DELETE `BASE/{id:guid}`

`BASE /api/v1/chats`

- GET `BASE`
- GET `BASE/{roomId}`
- POST `BASE/meetup`
- POST `BASE/direct`
- GET `BASE/user/{userId}`
- POST `BASE/{roomId}/join`
- POST `BASE/{roomId}/leave`
- GET `BASE/{roomId}/messages`
- GET `BASE/{roomId}/messages/search`
- GET `BASE/{roomId}/messages/search/count`
- POST `BASE/{roomId}/messages`
- DELETE `BASE/{roomId}/messages/{messageId}`
- GET `BASE/{roomId}/members`
- GET `BASE/{roomId}/members/online`
- GET `BASE/{roomId}/participants`

`BASE /Health`

- GET `BASE`

`BASE /api/v1/inbox`

- GET `BASE/summary`

`BASE /api/v1/notifications`

- GET `BASE`
- GET `BASE/unread/count`
- POST `BASE`
- POST `BASE/batch`
- POST `BASE/admins`
- PUT `BASE/{id}/read`
- PATCH `BASE/{id}/metadata`
- PUT `BASE/read/batch`
- PUT `BASE/read/all`
- DELETE `BASE/{id}`

`BASE /api/v1/im`

- GET `BASE/usersig`
- GET `BASE/usersig/{userId}`
- POST `BASE/accounts/import`
- POST `BASE/accounts/batch-import`
- POST `BASE/accounts/batch-import-ids`
- GET `BASE/accounts/{userId}/exists`
- POST `BASE/accounts/status`
- POST `BASE/accounts/ensure`

Hubs:

- Hub `/hubs/ai-progress`
- Hub `/hubs/notifications`
- Hub `/hubs/chat`

### ProductService

`BASE /api/v1/products`

- GET `BASE`
- GET `BASE/{id}`
- GET `BASE/user/{userId}`
- POST `BASE`
- PUT `BASE/{id}`
- DELETE `BASE/{id}`
- GET `BASE/health`

Phase 4b migration scope currently covers only the read-only subset:

- `GET /api/v1/products`
- `GET /api/v1/products/{id}`
- `GET /api/v1/products/user/{userId}`
- `GET /api/v1/products/health`
- `GET /health`

Product write routes remain on .NET until JWT-protected write parity and rollback tests exist.

### SearchService

`BASE /api/v1/index`

- GET `BASE/health`
- GET `BASE/stats`
- POST `BASE/sync/all`
- POST `BASE/sync/cities`
- POST `BASE/sync/coworkings`
- POST `BASE/rebuild`
- POST `BASE/sync/cities/{id:guid}`
- POST `BASE/sync/coworkings/{id:guid}`

`BASE /api/v1/search`

- GET `BASE`
- GET `BASE/cities`
- GET `BASE/coworkings`
- GET `BASE/suggest`

Phase 4d migration scope currently covers only the public query subset:

- GET `/api/v1/search`
- GET `/api/v1/search/cities`
- GET `/api/v1/search/coworkings`
- GET `/api/v1/search/suggest`
- GET `/health`

The following SearchService routes remain on .NET during Phase 4d:

- GET `/api/v1/index/health`
- GET `/api/v1/index/stats`
- POST `/api/v1/index/sync/all`
- POST `/api/v1/index/sync/cities`
- POST `/api/v1/index/sync/coworkings`
- POST `/api/v1/index/rebuild`
- POST `/api/v1/index/sync/cities/{id:guid}`
- POST `/api/v1/index/sync/coworkings/{id:guid}`

### UserService

`BASE /api/v1/admin/audit/events`

- GET `BASE`
- POST `BASE`

`BASE /api/v1/admin/legal`

- GET `BASE`
- GET `BASE/{id}`
- POST `BASE`
- PUT `BASE/{id}`
- DELETE `BASE/{id}`

`BASE /api/v1/admin/membership`

- GET `BASE/plans`
- GET `BASE/plans/{id}`
- GET `BASE/plans/{id}/subscribers`
- POST `BASE/plans`
- PUT `BASE/plans/{id}`
- DELETE `BASE/plans/{id}`

`BASE /api/v1/auth`

- POST `BASE/register/send-code`
- POST `BASE/register`
- POST `BASE/login`
- POST `BASE/refresh`
- POST `BASE/logout`
- POST `BASE/change-password`
- POST `BASE/forgot-password/send-code`
- POST `BASE/forgot-password/reset`
- POST `BASE/sms/send-code`
- POST `BASE/login/phone`
- GET `BASE/alipay/auth-info`
- POST `BASE/social-login`

`BASE /api/v1/interests`

- GET `BASE`
- GET `BASE/by-category`
- GET `BASE/category/{category}`
- GET `BASE/{id}`
- GET `BASE/users/{userId}`
- GET `BASE/me`
- POST `BASE/users/{userId}`
- POST `BASE/me`
- POST `BASE/me/batch`
- POST `BASE/users/{userId}/batch`
- DELETE `BASE/me/{interestId}`
- DELETE `BASE/users/{userId}/{interestId}`
- PUT `BASE/me/{interestId}`
- PUT `BASE/users/{userId}/{interestId}`

`BASE /api/v1/users/legal`

- GET `BASE`
- GET `BASE/privacy-policy`
- GET `BASE/terms-of-service`
- GET `BASE/history`

`BASE /api/v1/membership`

- GET `BASE/plans`
- GET `BASE/plans/{level:int}`
- GET `BASE`
- POST `BASE/upgrade`
- POST `BASE/deposit`
- POST `BASE/auto-renew`
- POST `BASE/ai-usage`
- GET `BASE/ai-usage/check`
- GET `BASE/expiring`
- POST `BASE/process-renewals`
- POST `BASE/process-expired`

`BASE /api/v1/payments`

- POST `BASE/orders`
- POST `BASE/orders/{orderId}/capture`
- GET `BASE/orders/{orderId}`
- GET `BASE/orders`
- POST `BASE/orders/{orderId}/cancel`
- POST `BASE/webhooks/paypal`
- GET `BASE/return`
- GET `BASE/cancel`
- POST `BASE/orders/wechat`
- POST `BASE/orders/{orderId}/wechat-confirm`
- POST `BASE/orders/alipay`
- POST `BASE/webhooks/wechat`
- POST `BASE/webhooks/alipay`

`BASE /api/v1/profile-snapshot`

- GET `BASE/current`

`BASE /api/v1/reports`

- GET `BASE/my`
- GET `BASE/{id}`
- POST `BASE/{id}/{action}`

`BASE /api/v1/roles`

- GET `BASE`
- GET `BASE/{id}`
- GET `BASE/by-name/{name}`
- POST `BASE`
- PUT `BASE/{id}`
- DELETE `BASE/{id}`
- GET `BASE/{id}/users`

`BASE /api/v1/skills`

- GET `BASE`
- GET `BASE/by-category`
- GET `BASE/category/{category}`
- GET `BASE/{id}`
- GET `BASE/users/{userId}`
- GET `BASE/me`
- POST `BASE/users/{userId}`
- POST `BASE/me`
- POST `BASE/me/batch`
- POST `BASE/users/{userId}/batch`
- DELETE `BASE/me/{skillId}`
- DELETE `BASE/users/{userId}/{skillId}`
- PUT `BASE/me/{skillId}`
- PUT `BASE/users/{userId}/{skillId}`

`BASE /api/v1/travel-history`

- GET `BASE`
- GET `BASE/confirmed`
- GET `BASE/unconfirmed`
- GET `BASE/{id}`
- POST `BASE`
- POST `BASE/batch`
- PUT `BASE/{id}`
- DELETE `BASE/{id}`
- POST `BASE/{id}/confirm`
- POST `BASE/confirm/batch`
- GET `BASE/stats`
- GET `BASE/user/{userId}`

`BASE /api/v1/users`

- GET `BASE/me/preferences`
- PUT `BASE/me/preferences`
- PATCH `BASE/me/preferences`
- GET `BASE/{userId}/preferences`
- POST `BASE/me/accept-privacy-policy`
- POST `BASE/me/accept-terms-of-service`
- GET `BASE`
- GET `BASE/search`
- GET `BASE/moderator-candidates`
- GET `BASE/dashboard/overview`
- GET `BASE/{id}`
- GET `BASE/{id}/basic`
- POST `BASE/batch`
- GET `BASE/me`
- GET `BASE/admins`
- POST `BASE`
- PUT `BASE/{id}`
- PUT `BASE/me`
- DELETE `BASE/{id}`
- PATCH `BASE/{id}/role`
- PATCH `BASE/batch/role`
- GET `BASE/health`
- GET `BASE/{userId}/products`
- GET `BASE/{id}/cached`
- GET `BASE/me/stats`
- GET `BASE/{userId}/stats`
- PUT `BASE/me/stats`
- PUT `BASE/{userId}/stats`

`BASE /api/v1/visited-places`

- GET `BASE/by-travel-history/{travelHistoryId}`
- GET `BASE/by-travel-history/{travelHistoryId}/highlights`
- GET `BASE/my`
- GET `BASE/city-summary/{cityId}`
- GET `BASE/{id}`
- POST `BASE`
- POST `BASE/batch`
- PUT `BASE/{id}`
- DELETE `BASE/{id}`
- PATCH `BASE/{id}/highlight`
- GET `BASE/by-travel-history/{travelHistoryId}/stats`

## 4. AI Image Contract

Public Go API must preserve the current AI image routes while delegating only image generation execution to Python.

### Generate Image

`POST /api/v1/ai/images/generate`

Request fields:

- `prompt`: required, max length 800.
- `negativePrompt`: optional, max length 800.
- `style`: default `<auto>`.
- `size`: default `1024*1024`; current supported values include `1024*1024`, `720*1280`, `1280*720`.
- `count`: 1 to 4, default 1.
- `bucket`: default `city-photos`.
- `pathPrefix`: optional.

Response data:

- `images[]`: `url`, `storagePath`, `originalUrl`, `fileSize`.
- `taskId`.
- `generationTimeMs`.
- `success`.
- `errorMessage`.

### Generate City Images

`POST /api/v1/ai/images/city`

Request fields:

- `cityId`: required.
- `cityName`: required.
- `country`: optional.
- `portraitPrompt`: optional.
- `landscapePrompt`: optional.
- `negativePrompt`: optional.
- `style`: default `<photography>`.
- `bucket`: default `city-photos`.
- `userId`: optional service-passed user id for notification.

Response data:

- `cityId`.
- `portraitImage`: `GeneratedImageInfo` for `720*1280`.
- `landscapeImages[]`: up to 4 `GeneratedImageInfo` records for `1280*720`.
- `generationTimeMs`.
- `success`.
- `errorMessage`.

### Generate City Images Async

`POST /api/v1/ai/images/city/async`

Behavior:

- Returns task immediately.
- Stores `task:image:{taskId}` in Redis for 24 hours.
- Publishes `AIProgressMessage` at task start.
- Publishes `CityImageGeneratedMessage` on completion/failure.
- Current estimated time: 180 seconds.

### Get Image Task Status

`GET /api/v1/ai/images/tasks/{taskId}`

Response data:

- `taskId`.
- `status`: `PENDING`, `RUNNING`, `SUCCEEDED`, `FAILED`, `CANCELED`, `UNKNOWN`, `TIMEOUT`, or `ERROR`.
- `imageUrls[]`.
- `succeededCount`.
- `failedCount`.
- `errorMessage`.

### City API Wrapper

`POST /api/v1/cities/{cityId:guid}/generate-images`

Behavior:

- Validates city exists.
- Sends city name, country, style and bucket to AI image task flow.
- Returns `taskId`, `cityId`, `cityName`, `status`, `estimatedTimeSeconds`, `message`.

## 5. Message Contract Baseline

At minimum, the Go implementation must preserve these shared message names and payload fields before switching consumers:

- `AIProgressMessage`: task id, user id, progress, message, task type, current stage, status, timestamp.
- `CityImageGeneratedMessage`: task id, city id, city name, user id, portrait image url, landscape image urls, success, error message, completed at, duration seconds.
- `AITaskCompletedMessage` and `AITaskFailedMessage`.
- `CityUpdatedMessage`, `CityRatingUpdatedMessage`, `CityReviewUpdatedMessage`.
- `CoworkingVerificationVotesMessage`.
- `SearchSyncMessages`.
- `UserUpdatedMessage`.
- `TravelPlanTaskMessage`, `DigitalNomadGuideTaskMessage`, `AIChatStreamMessages`.
- `ChatRoomOnlineStatusMessage`.

## 6. Contract Test Requirements

- Every route in this document must have a Go handler, Go proxy rule, or migration test explicitly covering fallback to .NET.
- Every message in this document must have JSON schema compatibility tests against current .NET message samples.
- Every public route must be tested through the Gateway, not only service-local handlers.
- Python sidecar tests are internal-only; public contract tests assert Go API behavior.
