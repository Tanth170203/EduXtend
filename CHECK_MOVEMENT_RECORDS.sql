-- Script kiểm tra MovementRecords và MovementRecordDetails sau khi điểm danh

-- 1. Xem MovementRecordDetails của StudentId = 14 (UserId = 11 - Nguyen Thanh Hieu)
SELECT 
    d.Id,
    d.MovementRecordId,
    d.CriterionId,
    c.Title as CriterionTitle,
    c.GroupId,
    d.ActivityId,
    a.Title as ActivityTitle,
    a.Type as ActivityType,
    d.Score,
    d.ScoreType,
    d.AwardedAt,
    d.CreatedAt
FROM MovementRecordDetails d
JOIN MovementCriteria c ON d.CriterionId = c.Id
LEFT JOIN Activities a ON d.ActivityId = a.Id
WHERE d.MovementRecordId IN (
    SELECT Id FROM MovementRecords WHERE StudentId = 14
)
ORDER BY d.CreatedAt DESC;

-- 2. Xem MovementRecords của StudentId = 14
SELECT 
    r.Id,
    r.StudentId,
    s.StudentCode,
    s.FullName,
    r.SemesterId,
    sem.Name as SemesterName,
    r.TotalScore,
    r.CreatedAt,
    r.LastUpdated,
    (SELECT COUNT(*) FROM MovementRecordDetails WHERE MovementRecordId = r.Id) as DetailCount
FROM MovementRecords r
JOIN Students s ON r.StudentId = s.Id
JOIN Semesters sem ON r.SemesterId = sem.Id
WHERE r.StudentId = 14
ORDER BY r.CreatedAt DESC;

-- 3. Xem tất cả MovementRecordDetails mới nhất (để debug)
SELECT TOP 10
    d.Id,
    d.MovementRecordId,
    r.StudentId,
    s.FullName as StudentName,
    d.CriterionId,
    c.Title as CriterionTitle,
    d.ActivityId,
    a.Title as ActivityTitle,
    d.Score,
    d.ScoreType,
    d.AwardedAt
FROM MovementRecordDetails d
JOIN MovementRecords r ON d.MovementRecordId = r.Id
JOIN Students s ON r.StudentId = s.Id
JOIN MovementCriteria c ON d.CriterionId = c.Id
LEFT JOIN Activities a ON d.ActivityId = a.Id
WHERE d.ScoreType = 'Auto'
ORDER BY d.Id DESC;

-- 4. Kiểm tra ActivityAttendances của UserId = 11
SELECT 
    aa.ActivityId,
    a.Title as ActivityTitle,
    a.Type as ActivityType,
    a.Status as ActivityStatus,
    aa.UserId,
    u.FullName as UserName,
    aa.IsPresent,
    aa.ParticipationScore,
    aa.CheckedAt,
    aa.CheckedById
FROM ActivityAttendances aa
JOIN Activities a ON aa.ActivityId = a.Id
JOIN Users u ON aa.UserId = u.Id
WHERE aa.UserId = 11
ORDER BY aa.CheckedAt DESC;

-- 5. Kiểm tra MovementCriteria ID = 10
SELECT 
    c.Id,
    c.GroupId,
    g.Name as GroupName,
    c.Title,
    c.Description,
    c.MaxScore,
    c.TargetType,
    c.DataSource,
    c.IsActive
FROM MovementCriteria c
JOIN MovementCriterionGroups g ON c.GroupId = g.Id
WHERE c.Id = 10;



