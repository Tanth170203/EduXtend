-- ================================================
-- FIX: Update SemesterId = 5 cho các transactions
-- ================================================

-- Vấn đề: SemesterId = NULL → không match với WHERE SemesterId = 5
-- Giải pháp: Update tất cả transactions chưa có SemesterId

UPDATE PaymentTransactions
SET SemesterId = 5  -- Fall2025
WHERE SemesterId IS NULL;

-- Verify
SELECT 
    Id,
    ClubId,
    Type,
    Amount,
    SemesterId,
    Title
FROM PaymentTransactions
WHERE ClubId = 1;

-- Expected result:
-- Tất cả rows phải có SemesterId = 5





