-- =============================================
-- Script: Alternative Solution with Timestamp
-- Description: Thêm timestamp để tạo unique identifier
-- Date: October 21, 2025
-- =============================================

USE EduXtend;
GO

-- =============================================
-- 1. THÊM CỘT TIMESTAMP
-- =============================================

PRINT '=== THÊM CỘT TIMESTAMP ===';

-- Thêm cột CreatedAt để tạo unique identifier
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('MovementRecordDetails') 
    AND name = 'CreatedAt'
)
BEGIN
    ALTER TABLE MovementRecordDetails 
    ADD CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE();
    
    PRINT '✅ Đã thêm cột CreatedAt';
END
ELSE
BEGIN
    PRINT 'ℹ️ Cột CreatedAt đã tồn tại';
END

-- =============================================
-- 2. XÓA UNIQUE CONSTRAINT CŨ
-- =============================================

PRINT '=== XÓA UNIQUE CONSTRAINT CŨ ===';

IF EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_MovementRecordDetails_MovementRecordId_CriterionId'
    AND object_id = OBJECT_ID('MovementRecordDetails')
)
BEGIN
    DROP INDEX IX_MovementRecordDetails_MovementRecordId_CriterionId 
    ON MovementRecordDetails;
    
    PRINT '✅ Đã xóa unique constraint cũ';
END

-- =============================================
-- 3. TẠO UNIQUE CONSTRAINT MỚI VỚI TIMESTAMP
-- =============================================

PRINT '=== TẠO UNIQUE CONSTRAINT MỚI ===';

-- Tạo unique constraint mới với timestamp
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_MovementRecordDetails_MovementRecordId_CriterionId_CreatedAt'
    AND object_id = OBJECT_ID('MovementRecordDetails')
)
BEGIN
    CREATE UNIQUE INDEX IX_MovementRecordDetails_MovementRecordId_CriterionId_CreatedAt
    ON MovementRecordDetails (MovementRecordId, CriterionId, CreatedAt);
    
    PRINT '✅ Đã tạo unique constraint mới với timestamp';
END
ELSE
BEGIN
    PRINT 'ℹ️ Unique constraint mới đã tồn tại';
END

-- =============================================
-- 4. KIỂM TRA KẾT QUẢ
-- =============================================

PRINT '=== KIỂM TRA KẾT QUẢ ===';

-- Kiểm tra cột CreatedAt
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ CỘT CreatedAt ĐÃ TỒN TẠI'
        ELSE '❌ CHƯA CÓ CỘT CreatedAt'
    END as Status
FROM sys.columns 
WHERE object_id = OBJECT_ID('MovementRecordDetails') 
AND name = 'CreatedAt';

-- Kiểm tra unique constraint mới
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ UNIQUE CONSTRAINT MỚI ĐÃ TẠO'
        ELSE '❌ CHƯA TẠO UNIQUE CONSTRAINT MỚI'
    END as Status
FROM sys.indexes 
WHERE name = 'IX_MovementRecordDetails_MovementRecordId_CriterionId_CreatedAt'
AND object_id = OBJECT_ID('MovementRecordDetails');

PRINT '=== SCRIPT COMPLETED SUCCESSFULLY ===';
PRINT 'Bây giờ có thể cộng dồn nhiều lần với timestamp unique!';
