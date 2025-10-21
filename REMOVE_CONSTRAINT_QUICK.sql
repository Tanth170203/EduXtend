-- Quick script to remove unique constraint for accumulation
-- Run this in SQL Server Management Studio or Azure Data Studio

USE EduXtend;
GO

-- Remove unique constraint to allow accumulation
DROP INDEX IX_MovementRecordDetails_MovementRecordId_CriterionId ON MovementRecordDetails;
GO

-- Create non-unique index for performance
CREATE NONCLUSTERED INDEX IX_MovementRecordDetails_MovementRecordId_CriterionId_NonUnique
ON MovementRecordDetails (MovementRecordId, CriterionId);
GO

PRINT '✅ Constraint removed successfully! Now you can accumulate multiple scores for the same criterion.';
