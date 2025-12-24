-- 修复已有3票或以上但状态仍为未验证的 coworking spaces
-- 根据 coworking_verifications 表中的投票数更新 verification_status

-- 先查看需要更新的数据
SELECT 
    cs.id,
    cs.name,
    cs.verification_status,
    COUNT(cv.id) as vote_count
FROM public.coworking_spaces cs
LEFT JOIN public.coworking_verifications cv ON cs.id = cv.coworking_id
WHERE cs.verification_status = 'unverified'
GROUP BY cs.id, cs.name, cs.verification_status
HAVING COUNT(cv.id) >= 3;

-- 执行更新
UPDATE public.coworking_spaces cs
SET verification_status = 'verified',
    updated_at = NOW()
WHERE cs.verification_status = 'unverified'
AND (
    SELECT COUNT(*) 
    FROM public.coworking_verifications cv 
    WHERE cv.coworking_id = cs.id
) >= 3;
