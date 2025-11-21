-- Update existing collaboration activities to set CollaborationStatus = 'Pending'
-- This is needed for activities created before the new workflow was implemented

UPDATE Activities
SET CollaborationStatus = 'Pending'
WHERE ClubCollaborationId IS NOT NULL
  AND CollaborationStatus IS NULL;

-- Verify the update
SELECT Id, Title, ClubId, ClubCollaborationId, Status, CollaborationStatus
FROM Activities
WHERE ClubCollaborationId IS NOT NULL;
