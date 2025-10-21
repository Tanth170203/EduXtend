-- =====================================================
-- FIX SCRIPT: STUDENTS WITH UNDEFINED NAMES
-- Date: 17/10/2025
-- =====================================================

-- STEP 1: Verify the problem
SELECT 
    Id,
    StudentCode,
    FullName,
    Status,
    UserId,
    LEN(FullName) as NameLength
FROM Students
WHERE FullName IS NULL 
   OR FullName = ''
   OR FullName LIKE '%undefined%'
   OR FullName LIKE 'undefined%'
ORDER BY Id;

-- =====================================================
-- STEP 2: Get user data to fill in missing names (if available)
-- =====================================================
SELECT 
    s.Id,
    s.StudentCode,
    s.FullName as StudentFullName,
    u.FullName as UserFullName,
    u.Email
FROM Students s
INNER JOIN Users u ON s.UserId = u.Id
WHERE s.FullName IS NULL 
   OR s.FullName = ''
   OR s.FullName LIKE '%undefined%'
ORDER BY s.Id;

-- =====================================================
-- STEP 3: Fix Option A - Use User FullName if Student FullName is invalid
-- =====================================================
UPDATE Students
SET FullName = u.FullName
FROM Students s
INNER JOIN Users u ON s.UserId = u.Id
WHERE (s.FullName IS NULL 
    OR s.FullName = ''
    OR s.FullName LIKE '%undefined%')
  AND u.FullName IS NOT NULL
  AND u.FullName != '';

-- =====================================================
-- STEP 4: Fix Option B - Set placeholder for any still-invalid names
-- =====================================================
UPDATE Students
SET FullName = 'Sinh viên ' + StudentCode
WHERE FullName IS NULL 
   OR FullName = ''
   OR FullName LIKE '%undefined%';

-- =====================================================
-- STEP 5: Verify the fix
-- =====================================================
SELECT TOP 20
    Id,
    StudentCode,
    FullName,
    Status,
    LEN(FullName) as NameLength
FROM Students
ORDER BY FullName;

-- Count total valid students
SELECT 
    COUNT(*) as TotalStudents,
    SUM(CASE WHEN FullName IS NOT NULL AND FullName != '' THEN 1 ELSE 0 END) as ValidNames,
    SUM(CASE WHEN FullName IS NULL OR FullName = '' THEN 1 ELSE 0 END) as InvalidNames
FROM Students;

-- =====================================================
-- STEP 6: Check if any students still have "undefined"
-- =====================================================
SELECT COUNT(*) as UndefinedCount
FROM Students
WHERE FullName LIKE '%undefined%'
   OR FullName IS NULL
   OR FullName = '';

-- Result should be: 0
-- If > 0, then there are still problems to fix manually

-- =====================================================
-- ROLLBACK OPTION (if something goes wrong)
-- =====================================================
-- ROLLBACK;  -- Uncomment to undo changes (only works if in transaction)
-- COMMIT;    -- Uncomment to confirm changes
