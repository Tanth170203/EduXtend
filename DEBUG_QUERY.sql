-- DEBUG: Check all conditions for query
SELECT 
    Id,
    ClubId,
    Type,
    Amount,
    Status,
    CASE 
        WHEN Status IS NULL THEN '✅ NULL (OK)'
        WHEN Status = '' THEN '❌ Empty String'
        WHEN Status = 'completed' THEN '✅ completed'
        WHEN Status = 'confirmed' THEN '✅ confirmed'
        ELSE '❌ Other: [' + Status + ']'
    END AS StatusCheck,
    SemesterId,
    CASE 
        WHEN SemesterId = 5 THEN '✅ Correct'
        WHEN SemesterId IS NULL THEN '❌ NULL'
        ELSE '❌ Wrong: ' + CAST(SemesterId AS VARCHAR)
    END AS SemesterCheck,
    Title
FROM PaymentTransactions
WHERE ClubId = 1
ORDER BY Id;

-- Count transactions that SHOULD match the query:
SELECT 
    Type,
    COUNT(*) AS Total,
    SUM(Amount) AS TotalAmount
FROM PaymentTransactions
WHERE ClubId = 1
  AND SemesterId = 5
  AND (Status IS NULL OR Status IN ('completed', 'confirmed'))
GROUP BY Type;

-- If still 0, check what's actually in Status:
SELECT DISTINCT 
    Status,
    CASE WHEN Status IS NULL THEN 'NULL' ELSE 'NOT NULL' END AS IsNull,
    LEN(ISNULL(Status, '')) AS Length
FROM PaymentTransactions
WHERE ClubId = 1;





