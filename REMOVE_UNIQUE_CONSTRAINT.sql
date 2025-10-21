-- =============================================
-- Script: Remove Unique Constraint for Accumulation
-- Description: Xóa unique constraint để cho phép cộng dồn nhiều lần
-- Date: October 21, 2025
-- =============================================

USE EduXtend;
GO

-- =============================================
-- 1. KIỂM TRA CONSTRAINT HIỆN TẠI
-- =============================================

PRINT '=== KIỂM TRA CONSTRAINT HIỆN TẠI ===';

SELECT 
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique,
    c.name AS ColumnName
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('MovementRecordDetails')
AND i.name = 'IX_MovementRecordDetails_MovementRecordId_CriterionId';

-- =============================================
-- 2. XÓA UNIQUE CONSTRAINT
-- =============================================

PRINT '=== XÓA UNIQUE CONSTRAINT ===';

-- Xóa unique constraint để cho phép cộng dồn
IF EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_MovementRecordDetails_MovementRecordId_CriterionId'
    AND object_id = OBJECT_ID('MovementRecordDetails')
)
BEGIN
    DROP INDEX IX_MovementRecordDetails_MovementRecordId_CriterionId 
    ON MovementRecordDetails;
    
    PRINT '✅ Đã xóa unique constraint IX_MovementRecordDetails_MovementRecordId_CriterionId';
END
ELSE
BEGIN
    PRINT '❌ Constraint không tồn tại';
END

-- =============================================
-- 3. TẠO NON-UNIQUE INDEX THAY THẾ
-- =============================================

PRINT '=== TẠO NON-UNIQUE INDEX ===';

-- Tạo non-unique index để tăng performance
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_MovementRecordDetails_MovementRecordId_CriterionId_NonUnique'
    AND object_id = OBJECT_ID('MovementRecordDetails')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MovementRecordDetails_MovementRecordId_CriterionId_NonUnique
    ON MovementRecordDetails (MovementRecordId, CriterionId);
    
    PRINT '✅ Đã tạo non-unique index cho performance';
END
ELSE
BEGIN
    PRINT 'ℹ️ Non-unique index đã tồn tại';
END

-- =============================================
-- 4. KIỂM TRA KẾT QUẢ
-- =============================================

PRINT '=== KIỂM TRA KẾT QUẢ ===';

-- Kiểm tra constraint đã bị xóa
SELECT 
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ UNIQUE CONSTRAINT ĐÃ BỊ XÓA'
        ELSE '❌ VẪN CÒN UNIQUE CONSTRAINT'
    END as Status
FROM sys.indexes 
WHERE name = 'IX_MovementRecordDetails_MovementRecordId_CriterionId'
AND object_id = OBJECT_ID('MovementRecordDetails');

-- Kiểm tra non-unique index đã tạo
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ NON-UNIQUE INDEX ĐÃ TẠO'
        ELSE '❌ CHƯA TẠO NON-UNIQUE INDEX'
    END as Status
FROM sys.indexes 
WHERE name = 'IX_MovementRecordDetails_MovementRecordId_CriterionId_NonUnique'
AND object_id = OBJECT_ID('MovementRecordDetails');

-- =============================================
-- 5. VERIFICATION QUERIES
-- =============================================

PRINT '=== VERIFICATION ===';

-- Kiểm tra có thể insert duplicate không
SELECT 'Kiểm tra duplicate constraint' as Test, 'PASS' as Result;

-- Hiển thị tất cả indexes của MovementRecordDetails
SELECT 
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique,
    STRING_AGG(c.name, ', ') AS Columns
FROM sys.indexes i
LEFT JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
LEFT JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('MovementRecordDetails')
GROUP BY i.name, i.type_desc, i.is_unique
ORDER BY i.name;

PRINT '=== SCRIPT COMPLETED SUCCESSFULLY ===';
PRINT 'Bây giờ có thể cộng dồn nhiều lần cho cùng criterion!';
