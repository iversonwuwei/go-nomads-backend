-- Pros & Cons feature schema
-- This script creates the tables, indexes, and trigger needed by the Flutter pros/cons voting flow.

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS city_pros_cons (
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

CREATE INDEX IF NOT EXISTS idx_city_pros_cons_city_ispro
    ON city_pros_cons (city_id, is_pro)
    WHERE status = 'active';

CREATE TABLE IF NOT EXISTS city_pros_cons_votes (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    pros_cons_id    UUID NOT NULL REFERENCES city_pros_cons(id) ON DELETE CASCADE,
    voter_user_id   UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    is_upvote       BOOLEAN NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_city_pros_cons_votes_unique
    ON city_pros_cons_votes (pros_cons_id, voter_user_id);

-- Trigger to keep aggregate counts in sync after each vote insert, update, or delete
CREATE OR REPLACE FUNCTION trg_city_pros_cons_vote_aggregate()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        -- 新增投票：增加对应计数
        UPDATE city_pros_cons SET
            upvotes   = upvotes + CASE WHEN NEW.is_upvote THEN 1 ELSE 0 END,
            downvotes = downvotes + CASE WHEN NEW.is_upvote THEN 0 ELSE 1 END,
            updated_at = now()
        WHERE id = NEW.pros_cons_id;
        RETURN NEW;
    ELSIF TG_OP = 'UPDATE' THEN
        -- 更新投票：调整计数（减去旧值，加上新值）
        UPDATE city_pros_cons SET
            upvotes   = upvotes 
                        - CASE WHEN OLD.is_upvote THEN 1 ELSE 0 END
                        + CASE WHEN NEW.is_upvote THEN 1 ELSE 0 END,
            downvotes = downvotes 
                        - CASE WHEN OLD.is_upvote THEN 0 ELSE 1 END
                        + CASE WHEN NEW.is_upvote THEN 0 ELSE 1 END,
            updated_at = now()
        WHERE id = NEW.pros_cons_id;
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        -- 删除投票：减少对应计数
        UPDATE city_pros_cons SET
            upvotes   = upvotes - CASE WHEN OLD.is_upvote THEN 1 ELSE 0 END,
            downvotes = downvotes - CASE WHEN OLD.is_upvote THEN 0 ELSE 1 END,
            updated_at = now()
        WHERE id = OLD.pros_cons_id;
        RETURN OLD;
    END IF;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS city_pros_cons_votes_ai ON city_pros_cons_votes;

CREATE TRIGGER city_pros_cons_votes_ai
AFTER INSERT OR UPDATE OR DELETE ON city_pros_cons_votes
FOR EACH ROW EXECUTE FUNCTION trg_city_pros_cons_vote_aggregate();
