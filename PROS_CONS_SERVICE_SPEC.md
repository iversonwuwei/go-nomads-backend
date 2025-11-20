# Pros & Cons Service Specification

This document describes the backend service and SQL schema required by the Flutter pros & cons feature found in `df_admin_mobile`. The mobile client already ships the UI and repository logic; this spec fills the missing backend pieces so both projects can be wired together quickly.

## 1. Feature Overview

The Flutter app needs to:

1. List pros (advantages) and cons (challenges) for a city.
2. Add new entries (moderator/admin only, per product rules).
3. Delete entries (moderator/admin only).
4. Let any authenticated user up-vote an entry exactly once. Down-votes exist in the data model for future use.
5. Receive updated vote counts immediately after voting.

## 2. REST API Surface

| Endpoint | Method | Description |
| --- | --- | --- |
| `/api/cities/{cityId}/user-content/pros-cons?isPro=true|false|null` | GET | Return filtered list. `isPro` omitted → both lists grouped. |
| `/api/cities/{cityId}/user-content/pros-cons` | POST | Create one entry (body: `text`, `isPro`). |
| `/api/cities/{cityId}/user-content/pros-cons/{prosConsId}` | DELETE | Soft delete entry. |
| `/api/user-content/pros-cons/{prosConsId}/vote` | POST | Cast a single vote. Body: `{ "isUpvote": true }`. |

### 2.1 Payload Contracts

#### 2.1.1 GET response

```jsonc
{
  "success": true,
  "data": [{
    "id": "uuid",
    "cityId": "uuid",
    "userId": "uuid",
    "text": "string",
    "isPro": true,
    "upvotes": 12,
    "downvotes": 0,
    "createdAt": "2024-01-01T10:00:00Z",
    "updatedAt": "2024-01-02T10:00:00Z"
  }]
}
```

If `isPro` is omitted, you can return `{ "pros": [...], "cons": [...] }`, but the Flutter code assumes the list shape above, so keeping the flat array is simplest.

#### 2.1.2 POST create request

```json
{
  "cityId": "uuid",
  "text": "Fast fiber is everywhere",
  "isPro": true
}
```

Response: same shape as GET item.

#### 2.1.3 Vote request

```json
{ "isUpvote": true }
```

Response: `{ "success": true, "data": { "upvotes": 13, "downvotes": 0 } }`.

### 2.2 Error Codes

| Status | Code | Meaning |
| --- | --- | --- |
| 400 | `PROS_CONS_VALIDATION` | Invalid payload or duplicate vote. |
| 401 | `UNAUTHORIZED` | User not authenticated. |
| 403 | `FORBIDDEN` | User lacks moderator/admin rights for create/delete. |
| 404 | `PROS_CONS_NOT_FOUND` | Entry missing. |

Duplicate voting should raise `400` with message `"You already voted for this entry"` so the Flutter client can display the toast.

## 3. Database Schema

### 3.1 Tables

```sql
CREATE TABLE city_pros_cons (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    city_id         UUID NOT NULL REFERENCES cities(id) ON DELETE CASCADE,
    author_user_id  UUID NOT NULL REFERENCES users(id),
    text            TEXT NOT NULL CHECK (length(trim(text)) BETWEEN 5 AND 500),
    is_pro          BOOLEAN NOT NULL,
    upvotes         INTEGER NOT NULL DEFAULT 0,
    downvotes       INTEGER NOT NULL DEFAULT 0,
    status          TEXT NOT NULL DEFAULT 'active', -- active | deleted
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_city_pros_cons_city_ispro
    ON city_pros_cons (city_id, is_pro)
    WHERE status = 'active';
```

```sql
CREATE TABLE city_pros_cons_votes (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    pros_cons_id    UUID NOT NULL REFERENCES city_pros_cons(id) ON DELETE CASCADE,
    voter_user_id   UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    is_upvote       BOOLEAN NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX uq_city_pros_cons_votes_unique
    ON city_pros_cons_votes (pros_cons_id, voter_user_id);
```

> **Single-vote enforcement** is guaranteed by the unique index above. Attempting to insert a duplicate vote will raise an error that the service converts into a user-friendly message.

### 3.2 Vote Count Maintenance

Two options:

1. **Eventual update:** keep `upvotes/downvotes` as materialized counters. After inserting into `city_pros_cons_votes`, run:
   ```sql
   UPDATE city_pros_cons
      SET upvotes   = upvotes + CASE WHEN NEW.is_upvote THEN 1 ELSE 0 END,
          downvotes = downvotes + CASE WHEN NEW.is_upvote THEN 0 ELSE 1 END,
          updated_at = now()
    WHERE id = NEW.pros_cons_id;
   ```
   Wrap in a plpgsql trigger (`AFTER INSERT ON city_pros_cons_votes`).

2. **Derived counts:** drop the columns and compute via `COUNT(*) FILTER (WHERE is_upvote)` when querying. This simplifies writes but adds aggregation cost. The Flutter repo expects `upvotes` and `downvotes` fields, so approach (1) is friendlier.

Sample trigger:

```sql
CREATE OR REPLACE FUNCTION trg_city_pros_cons_vote_aggregate()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE city_pros_cons SET
            upvotes   = upvotes + CASE WHEN NEW.is_upvote THEN 1 ELSE 0 END,
            downvotes = downvotes + CASE WHEN NEW.is_upvote THEN 0 ELSE 1 END,
            updated_at = now()
        WHERE id = NEW.pros_cons_id;
        RETURN NEW;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER city_pros_cons_votes_ai
AFTER INSERT ON city_pros_cons_votes
FOR EACH ROW EXECUTE FUNCTION trg_city_pros_cons_vote_aggregate();
```

## 4. Service Flow / Pseudocode

Example using NestJS (Express variant); any framework works as long as the rules below hold.

### 4.1 DTOs

```ts
// pros-cons.dto.ts
export class CreateProsConsDto {
  @IsUUID()
  cityId: string;

  @IsString()
  @MinLength(5)
  @MaxLength(500)
  text: string;

  @IsBoolean()
  isPro: boolean;
}

export class VoteProsConsDto {
  @IsBoolean()
  isUpvote: boolean;
}
```

### 4.2 Controller

```ts
@Controller('api')
export class ProsConsController {
  constructor(private readonly service: ProsConsService) {}

  @Get('cities/:cityId/user-content/pros-cons')
  list(@Param('cityId') cityId: string, @Query('isPro') isPro?: string) {
    return this.service.list(cityId, isPro);
  }

  @Post('cities/:cityId/user-content/pros-cons')
  @Roles('admin', 'moderator')
  create(
    @Param('cityId') cityId: string,
    @Body() dto: CreateProsConsDto,
    @CurrentUser() user: AuthUser,
  ) {
    return this.service.create(cityId, dto, user.id);
  }

  @Delete('cities/:cityId/user-content/pros-cons/:id')
  @Roles('admin', 'moderator')
  remove(@Param('id') id: string, @CurrentUser() user: AuthUser) {
    return this.service.softDelete(id, user.id);
  }

  @Post('user-content/pros-cons/:id/vote')
  vote(
    @Param('id') id: string,
    @Body() dto: VoteProsConsDto,
    @CurrentUser() user: AuthUser,
  ) {
    return this.service.vote(id, dto.isUpvote, user.id);
  }
}
```

### 4.3 Service logic

```ts
async vote(id: string, isUpvote: boolean, userId: string) {
  await this.repo.findOrFail(id); // ensures entry exists & active

  try {
    await this.repo.insertVote(id, userId, isUpvote);
  } catch (error) {
    if (isUniqueViolation(error)) {
      throw new BadRequestException('You already voted for this entry');
    }
    throw error;
  }

  const counts = await this.repo.getVoteCounts(id);
  return { success: true, data: counts };
}
```

### 4.4 Repository snippets (SQL)

```sql
-- list
SELECT id, city_id, author_user_id AS "userId", text, is_pro AS "isPro",
       upvotes, downvotes, created_at, updated_at
  FROM city_pros_cons
 WHERE city_id = $1
   AND status = 'active'
   AND ($2::boolean IS NULL OR is_pro = $2)
 ORDER BY created_at DESC;
```

```sql
-- insert vote
INSERT INTO city_pros_cons_votes (pros_cons_id, voter_user_id, is_upvote)
VALUES ($1, $2, $3);
```

```sql
-- get counts after voting
SELECT upvotes, downvotes FROM city_pros_cons WHERE id = $1;
```

## 5. Integration Checklist

1. **Authentication**: All endpoints require logged-in users; ensure the user ID is propagated from JWT/session so we can enforce the single-vote rule.
2. **Authorization**: Only moderators/admins can create or delete entries. Voting is open to any authenticated user.
3. **Rate limiting**: Consider limiting POST endpoints to prevent spam.
4. **Auditing**: Optionally log `userId`, `action`, `prosConsId`, `requestId` for moderation purposes.
5. **Pagination**: Flutter currently fetches the entire list. If cities accumulate thousands of entries, add pagination query params and update the mobile client later.
6. **Soft delete**: keep deleted rows for moderation history – `status = 'deleted'` and hide them from queries.
7. **Testing**: Add unit tests covering duplicate vote constraint, unauthorized access, and cross-city tampering (vote only on entries from same city).

With this service and schema in place, the Flutter app’s existing repository (`ProsConsStateController`) will start working immediately; no further mobile-side changes are needed.
