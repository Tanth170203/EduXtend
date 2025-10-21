-- =============================================
-- Script: Fix Duplicate Key Error
-- Description: Sửa lỗi duplicate key trong MovementRecordDetails
-- Date: October 21, 2025
-- =============================================

USE EduXtend;
GO

-- =============================================
-- 1. KIỂM TRA DỮ LIỆU HIỆN TẠI
-- =============================================

PRINT '=== KIỂM TRA DUPLICATE RECORDS ===';

-- Tìm các record duplicate
WITH DuplicateRecords AS (
    SELECT 
        MovementRecordId,
        CriterionId,
        COUNT(*) as Count,
        STRING_AGG(CAST(Id as VARCHAR), ', ') as Ids,
        STRING_AGG(CAST(Score as VARCHAR), ', ') as Scores,
        STRING_AGG(CAST(AwardedAt as VARCHAR), ', ') as AwardedAts
    FROM MovementRecordDetails
    GROUP BY MovementRecordId, CriterionId
    HAVING COUNT(*) > 1
)
SELECT * FROM DuplicateRecords;

-- Kiểm tra record cụ thể gây lỗi
PRINT '=== RECORD GÂY LỖI (MovementRecordId=8, CriterionId=5) ===';
SELECT * FROM MovementRecordDetails 
WHERE MovementRecordId = 8 AND CriterionId = 5
ORDER BY AwardedAt DESC;

-- =============================================
-- 2. XÓA DUPLICATE RECORDS
-- =============================================

PRINT '=== XÓA DUPLICATE RECORDS ===';

-- Xóa duplicate records (giữ lại record mới nhất)
WITH DuplicateRecords AS (
    SELECT 
        Id,
        ROW_NUMBER() OVER (
            PARTITION BY MovementRecordId, CriterionId 
            ORDER BY AwardedAt DESC, Id DESC
        ) as rn
    FROM MovementRecordDetails
)
DELETE FROM MovementRecordDetails 
WHERE Id IN (
    SELECT Id FROM DuplicateRecords WHERE rn > 1
);

PRINT 'Đã xóa duplicate records';

-- =============================================
-- 3. THÊM CONSTRAINT ĐỂ TRÁNH TƯƠNG LAI
-- =============================================

PRINT '=== THÊM CONSTRAINT UNIQUE ===';

-- Kiểm tra xem constraint đã tồn tại chưa
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_MovementRecordDetails_MovementRecordId_CriterionId'
)
BEGIN
    -- Thêm unique constraint
    ALTER TABLE MovementRecordDetails 
    ADD CONSTRAINT IX_MovementRecordDetails_MovementRecordId_CriterionId 
    UNIQUE (MovementRecordId, CriterionId);
    
    PRINT 'Đã thêm unique constraint';
END
ELSE
BEGIN
    PRINT 'Unique constraint đã tồn tại';
END

-- =============================================
-- 4. KIỂM TRA KẾT QUẢ
-- =============================================

PRINT '=== KIỂM TRA KẾT QUẢ ===';

-- Kiểm tra còn duplicate không
WITH DuplicateCheck AS (
    SELECT 
        MovementRecordId,
        CriterionId,
        COUNT(*) as Count
    FROM MovementRecordDetails
    GROUP BY MovementRecordId, CriterionId
    HAVING COUNT(*) > 1
)
SELECT 
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ KHÔNG CÒN DUPLICATE'
        ELSE '❌ VẪN CÒN DUPLICATE: ' + CAST(COUNT(*) as VARCHAR)
    END as Status
FROM DuplicateCheck;

-- Kiểm tra record cụ thể
SELECT 
    CASE 
        WHEN COUNT(*) <= 1 THEN '✅ RECORD (8,5) ĐÃ SẠCH'
        ELSE '❌ RECORD (8,5) VẪN CÒN DUPLICATE'
    END as Status
FROM MovementRecordDetails 
WHERE MovementRecordId = 8 AND CriterionId = 5;

-- =============================================
-- 5. THÊM INDEX ĐỂ TĂNG PERFORMANCE
-- =============================================

PRINT '=== THÊM INDEX PERFORMANCE ===';

-- Thêm index cho AwardedAt để tăng performance
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_MovementRecordDetails_AwardedAt'
)
BEGIN
    CREATE INDEX IX_MovementRecordDetails_AwardedAt 
    ON MovementRecordDetails (AwardedAt);
    PRINT 'Đã thêm index cho AwardedAt';
END
ELSE
BEGIN
    PRINT 'Index cho AwardedAt đã tồn tại';
END

-- Thêm index cho MovementRecordId
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_MovementRecordDetails_MovementRecordId'
)
BEGIN
    CREATE INDEX IX_MovementRecordDetails_MovementRecordId 
    ON MovementRecordDetails (MovementRecordId);
    PRINT 'Đã thêm index cho MovementRecordId';
END
ELSE
BEGIN
    PRINT 'Index cho MovementRecordId đã tồn tại';
END

-- =============================================
-- 6. VERIFICATION QUERIES
-- =============================================

PRINT '=== VERIFICATION ===';

-- Tổng số records
SELECT 'Tổng số MovementRecordDetails' as Metric, COUNT(*) as Value FROM MovementRecordDetails;

-- Tổng số MovementRecords
SELECT 'Tổng số MovementRecords' as Metric, COUNT(*) as Value FROM MovementRecords;

-- Tổng số Students
SELECT 'Tổng số Students' as Metric, COUNT(*) as Value FROM Students;

-- Tổng số Semesters
SELECT 'Tổng số Semesters' as Metric, COUNT(*) as Value FROM Semesters;

-- Active semester
SELECT 'Active Semester' as Metric, Name as Value FROM Semesters WHERE IsActive = 1;

PRINT '=== SCRIPT COMPLETED SUCCESSFULLY ===';
