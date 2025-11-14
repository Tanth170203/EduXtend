-- Check Type field values (case-sensitive!)
SELECT 
    Id,
    ClubId,
    Type,
    CASE 
        WHEN Type = 'Income' THEN '✅ Correct'
        WHEN Type = 'Expense' THEN '✅ Correct'
        ELSE '❌ Wrong case: ' + Type
    END AS TypeCheck,
    Amount,
    SemesterId,
    Title
FROM PaymentTransactions
WHERE ClubId = 1
ORDER BY Id;

-- If Type is wrong case, fix it:
/*
UPDATE PaymentTransactions
SET Type = CASE
    WHEN LOWER(Type) = 'income' THEN 'Income'
    WHEN LOWER(Type) = 'expense' THEN 'Expense'
    ELSE Type
END
WHERE ClubId = 1;
*/





