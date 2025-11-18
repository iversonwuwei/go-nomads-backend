# Coworking Verification Flow

This document describes how a coworking space transitions between `unverified` and `verified` using the new community voting workflow.

## Data Model

| Table | Key Columns | Notes |
| --- | --- | --- |
| `coworking_spaces` | `verification_status` | `TEXT NOT NULL` with `verified`/`unverified` constraint. Defaults to `unverified` for ordinary submissions; admins/moderators can set `verified` at creation time. |
| `coworking_verifications` | `coworking_id`, `user_id`, `created_at` | Records each endorsement. `(coworking_id, user_id)` is unique so a user can only vote once per space. Rows cascade-delete with the parent coworking space. |

### Running the migration

Apply the SQL at `database/migrations/add_coworking_verification_support.sql` against Supabase to add the column, table, indexes, and RLS policies.

```bash
psql "$SUPABASE_CONNECTION_STRING" \
  -f database/migrations/add_coworking_verification_support.sql
```

Afterwards you can regenerate the reference schema by running whatever `schema.sql` refresh command you normally use (typically `supabase db dump`).

## API Flow

1. **Creation** – `POST /api/v1/coworking` automatically marks entries from non-admin/moderator users as `unverified` while privileged roles remain `verified`.
2. **Voting** – Authenticated non-creators call `POST /api/v1/coworking/{id}/verifications`. The service ensures:
   - creators cannot endorse their own spaces,
   - duplicate votes are rejected,
   - each vote writes to `coworking_verifications`.
3. **Auto-promotion** – When a space accumulates ≥3 unique votes it is automatically promoted to `verified` and persisted back to Supabase.
4. **Manual override** – Admins or city moderators can call `PUT /api/v1/coworking/{id}/verification-status` to override the status at any time.

### Sample vote request

```bash
curl -X POST \
  -H "Authorization: Bearer <user-jwt>" \
  "https://<api-host>/api/v1/coworking/<coworkingId>/verifications"
```

The JSON response includes the latest `verificationStatus` plus `verificationVotes` so clients can update their UI immediately.

## Deployment Checklist

- [ ] Execute the migration SQL on each Supabase environment.
- [ ] Restart `CoworkingService` (and dependent BFF services) so the updated Shared extensions are loaded.
- [ ] Verify that listing/detail endpoints now surface `verificationStatus` and `verificationVotes`.
- [ ] Wire the mobile/web client buttons to the new vote endpoint, hiding the action from creators and showing status badges for everyone else.
