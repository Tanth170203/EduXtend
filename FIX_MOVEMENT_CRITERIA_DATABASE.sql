-- =============================================
-- Script: Fix Movement Criteria Database
-- Description: Sửa lại database cho đúng với tiêu chí đánh giá
-- Date: October 21, 2025
-- =============================================

USE EduXtend;
GO

-- =============================================
-- 1. TẠO LẠI MOVEMENT CRITERION GROUPS
-- =============================================

-- Xóa dữ liệu cũ
DELETE FROM MovementCriteria WHERE GroupId IN (1,2,3,4);
DELETE FROM MovementCriterionGroups WHERE Id IN (1,2,3,4);

-- Tạo lại 4 nhóm chính theo tiêu chí
INSERT INTO MovementCriterionGroups (Id, Name, Description, MaxScore, TargetType, CreatedAt, UpdatedAt)
VALUES 
    (1, 'ĐÁNH GIÁ VỀ Ý THỨC HỌC TẬP', 'Đánh giá ý thức học tập của sinh viên', 35, 'Student', GETDATE(), GETDATE()),
    (2, 'ĐÁNH GIÁ VỀ Ý THỨC VÀ KẾT QUẢ THAM GIA CÁC HOẠT ĐỘNG CHÍNH TRỊ - XÃ HỘI', 'Đánh giá tham gia hoạt động chính trị, xã hội, văn hóa, thể thao', 50, 'Student', GETDATE(), GETDATE()),
    (3, 'ĐÁNH GIÁ VỀ PHẨM CHẤT CÔNG DÂN VÀ QUAN HỆ VỚI CỘNG ĐỒNG', 'Đánh giá phẩm chất công dân và hoạt động cộng đồng', 25, 'Student', GETDATE(), GETDATE()),
    (4, 'ĐÁNH GIÁ VỀ Ý THỨC VÀ KẾT QUẢ THAM GIA CÔNG TÁC PHỤ TRÁCH', 'Đánh giá công tác phụ trách lớp, đoàn thể, tổ chức', 30, 'Student', GETDATE(), GETDATE());

-- =============================================
-- 2. TẠO LẠI MOVEMENT CRITERIA THEO ĐÚNG TIÊU CHÍ
-- =============================================

-- Xóa tất cả criteria cũ
DELETE FROM MovementCriteria;

-- NHÓM 1: Ý THỨC HỌC TẬP (Max: 35 điểm)
INSERT INTO MovementCriteria (Id, GroupId, Title, Description, MaxScore, TargetType, DataSource, IsActive, CreatedAt, UpdatedAt)
VALUES 
    (1, 1, 'Tuyên dương công khai trước lớp', 'Được cộng 2 điểm/lần tuyên dương công khai trước lớp: có thái độ tích cực, đóng góp đặc biệt trong giờ học được GV ghi nhận', 2, 'Student', 'Sổ nhận xét giờ giảng', 1, GETDATE(), GETDATE()),
    (2, 1, 'Tham gia kỳ thi Olympic, ACM/CPC, Robocon', 'Tham gia các kỳ thi Olympic, ACM/CPC, Robocon hoặc các cuộc thi học thuật mang tầm Quốc gia/khu vực', 10, 'Student', 'Danh sách CTSV', 1, GETDATE(), GETDATE()),
    (3, 1, 'Tham gia hoạt động, cuộc thi cấp trường', 'Tham gia các hoạt động, cuộc thi cấp trường', 5, 'Student', 'Danh sách CTSV', 1, GETDATE(), GETDATE());

-- NHÓM 2: HOẠT ĐỘNG CHÍNH TRỊ - XÃ HỘI (Max: 50 điểm)
INSERT INTO MovementCriteria (Id, GroupId, Title, Description, MaxScore, TargetType, DataSource, IsActive, CreatedAt, UpdatedAt)
VALUES 
    (4, 2, 'Tham gia sự kiện CTSV', 'Với mỗi sự kiện tham gia CTSV và ban tổ chức (BTC) sự kiện sẽ công bố số điểm sinh viên được cộng theo thang điểm từ 3-5 điểm/sự kiện', 5, 'Student', 'Danh sách điểm CTSV', 1, GETDATE(), GETDATE()),
    (5, 2, 'Tham gia CLB, đội tình nguyện, đội văn nghệ, đội tuyển thể thao', 'Chủ nhiệm CLB, Cán bộ Quản lý CLB hoặc Huấn luyện viên đánh giá thành viên chính thức và có sinh hoạt đầy đủ tại các CLB, đội tình nguyện, đội văn nghệ, đội tuyển thể thao', 10, 'Student', 'Đánh giá của Ban chủ nhiệm CLB và cán bộ phòng Phát triển cá nhân (PDP)', 1, GETDATE(), GETDATE()),
    (6, 2, 'Tham gia hoạt động văn hóa, văn nghệ', 'Tham gia các hoạt động văn hóa, văn nghệ do trường tổ chức', 5, 'Student', 'Danh sách CTSV', 1, GETDATE(), GETDATE()),
    (7, 2, 'Tham gia hoạt động thể dục thể thao', 'Tham gia các hoạt động thể dục thể thao do trường tổ chức', 5, 'Student', 'Danh sách CTSV', 1, GETDATE(), GETDATE()),
    (8, 2, 'Tham gia phòng chống tệ nạn xã hội', 'Tham gia các hoạt động phòng chống tệ nạn xã hội', 5, 'Student', 'Danh sách CTSV', 1, GETDATE(), GETDATE());

-- NHÓM 3: PHẨM CHẤT CÔNG DÂN (Max: 25 điểm)
INSERT INTO MovementCriteria (Id, GroupId, Title, Description, MaxScore, TargetType, DataSource, IsActive, CreatedAt, UpdatedAt)
VALUES 
    (9, 3, 'Hành vi tốt được ghi nhận', 'Sinh viên có những hành vi tốt (nhặt được của rơi đem trả người mất, giúp đỡ người khuyết tật...) được ghi nhận ở cấp trường hay một tổ chức xã hội khác', 5, 'Student', 'Danh sách CTSV tập hợp', 1, GETDATE(), GETDATE()),
    (10, 3, 'Tham gia hoạt động xã hội, từ thiện, tình nguyện', 'Sinh viên tham gia các hoạt động xã hội, từ thiện, tình nguyện', 5, 'Student', 'Danh sách CTSV', 1, GETDATE(), GETDATE()),
    (11, 3, 'Tham gia hoạt động cộng đồng', 'Tham gia các hoạt động phục vụ cộng đồng, xã hội', 5, 'Student', 'Danh sách CTSV', 1, GETDATE(), GETDATE());

-- NHÓM 4: CÔNG TÁC PHỤ TRÁCH (Max: 30 điểm)
INSERT INTO MovementCriteria (Id, GroupId, Title, Description, MaxScore, TargetType, DataSource, IsActive, CreatedAt, UpdatedAt)
VALUES 
    (12, 4, 'Chủ nhiệm CLB', 'Chủ nhiệm các CLB được cộng từ 5-10 điểm', 10, 'Student', 'Danh sách do PDP, CTSV cung cấp', 1, GETDATE(), GETDATE()),
    (13, 4, 'Trưởng BTC các sự kiện của Trường', 'Trưởng BTC các sự kiện của Trường được cộng từ 5-10 điểm', 10, 'Student', 'Danh sách do PDP, CTSV cung cấp', 1, GETDATE(), GETDATE()),
    (14, 4, 'Lớp trưởng, thành viên BCH Đoàn trường, Hội sinh viên', 'Lớp trưởng, thành viên BCH Đoàn trường, Hội sinh viên được cộng 10 điểm', 10, 'Student', 'Danh sách từ cán bộ CTSV, Đoàn trường, Hội sinh viên cung cấp', 1, GETDATE(), GETDATE()),
    (15, 4, 'Phó BTC các sự kiện', 'Phó BTC các sự kiện của Trường', 5, 'Student', 'Danh sách do PDP, CTSV cung cấp', 1, GETDATE(), GETDATE()),
    (16, 4, 'Thành viên BTC các sự kiện', 'Thành viên BTC các sự kiện của Trường', 3, 'Student', 'Danh sách do PDP, CTSV cung cấp', 1, GETDATE(), GETDATE());

-- =============================================
-- 3. TẠO THÊM CRITERIA CHO CLUB (nếu cần)
-- =============================================

-- Tạo nhóm cho Club activities
INSERT INTO MovementCriterionGroups (Id, Name, Description, MaxScore, TargetType, CreatedAt, UpdatedAt)
VALUES 
    (5, 'BÁO CÁO CLB', 'Đánh giá hoạt động báo cáo của CLB', 100, 'Club', GETDATE(), GETDATE()),
    (6, 'KẾ HOẠCH CLB', 'Đánh giá kế hoạch hoạt động của CLB', 50, 'Club', GETDATE(), GETDATE());

-- Criteria cho Club
INSERT INTO MovementCriteria (Id, GroupId, Title, Description, MaxScore, TargetType, DataSource, IsActive, CreatedAt, UpdatedAt)
VALUES 
    (17, 5, 'Sinh hoạt CLB định kỳ', 'Hoạt động sinh hoạt CLB sẽ được tính 5 điểm/tuần', 20, 'Club', 'ActivityAttendance', 1, GETDATE(), GETDATE()),
    (18, 5, 'Tổ chức sự kiện lớn (100-200 người)', 'Tổ chức sự kiện lớn với 100-200 người tham gia', 20, 'Club', 'Activity', 1, GETDATE(), GETDATE()),
    (19, 5, 'Tổ chức sự kiện nhỏ (50-100 người)', 'Tổ chức sự kiện nhỏ với 50-100 người tham gia', 15, 'Club', 'Activity', 1, GETDATE(), GETDATE()),
    (20, 5, 'Tổ chức sự kiện nội bộ', 'Tổ chức sự kiện nội bộ CLB', 5, 'Club', 'Activity', 1, GETDATE(), GETDATE()),
    (21, 5, 'Phối hợp với CLB khác (vai trò BTC)', 'Phối hợp với các CLB khác với vai trò Ban tổ chức', 10, 'Club', 'ActivityCollaboration', 1, GETDATE(), GETDATE()),
    (22, 5, 'Phối hợp với CLB khác (vai trò tham dự)', 'Phối hợp với các CLB khác với vai trò tham dự', 3, 'Club', 'ActivityCollaboration', 1, GETDATE(), GETDATE()),
    (23, 5, 'Phối hợp với Nhà trường (vai trò BTC)', 'Phối hợp với Nhà trường với vai trò Ban tổ chức', 10, 'Club', 'ActivityCollaboration', 1, GETDATE(), GETDATE()),
    (24, 5, 'Phối hợp với Nhà trường (vai trò tham dự)', 'Phối hợp với Nhà trường với vai trò tham dự', 3, 'Club', 'ActivityCollaboration', 1, GETDATE(), GETDATE()),
    (25, 5, 'Tham gia cuộc thi cấp Trường', 'Tham gia các cuộc thi cấp Trường', 20, 'Club', 'ActivityCompetition', 1, GETDATE(), GETDATE()),
    (26, 5, 'Tham gia cuộc thi cấp Tỉnh/TP', 'Tham gia các cuộc thi cấp Tỉnh/Thành phố', 30, 'Club', 'ActivityCompetition', 1, GETDATE(), GETDATE()),
    (27, 5, 'Tham gia cuộc thi cấp Quốc gia', 'Tham gia các cuộc thi cấp Quốc gia', 30, 'Club', 'ActivityCompetition', 1, GETDATE(), GETDATE()),
    (28, 5, 'Tham gia các cuộc thi khác', 'Tham gia các cuộc thi khác tùy theo số lượng và tính chất', 10, 'Club', 'ActivityCompetition', 1, GETDATE(), GETDATE()),
    (29, 6, 'Hoàn thành kế hoạch đầy đủ và đúng hạn', 'Hoàn thành kế hoạch hoạt động đầy đủ và đúng hạn', 10, 'Club', 'PlanSubmission', 1, GETDATE(), GETDATE());

-- =============================================
-- 4. CẬP NHẬT IDENTITY SEED
-- =============================================

-- Reset identity seed cho MovementCriterionGroups
DBCC CHECKIDENT ('MovementCriterionGroups', RESEED, 6);

-- Reset identity seed cho MovementCriteria  
DBCC CHECKIDENT ('MovementCriteria', RESEED, 29);

-- =============================================
-- 5. VERIFICATION QUERIES
-- =============================================

-- Kiểm tra kết quả
PRINT '=== MOVEMENT CRITERION GROUPS ===';
SELECT Id, Name, MaxScore, TargetType FROM MovementCriterionGroups ORDER BY Id;

PRINT '=== MOVEMENT CRITERIA FOR STUDENTS ===';
SELECT c.Id, c.GroupId, g.Name as GroupName, c.Title, c.MaxScore, c.TargetType, c.DataSource 
FROM MovementCriteria c 
INNER JOIN MovementCriterionGroups g ON c.GroupId = g.Id 
WHERE c.TargetType = 'Student' 
ORDER BY c.GroupId, c.Id;

PRINT '=== MOVEMENT CRITERIA FOR CLUBS ===';
SELECT c.Id, c.GroupId, g.Name as GroupName, c.Title, c.MaxScore, c.TargetType, c.DataSource 
FROM MovementCriteria c 
INNER JOIN MovementCriterionGroups g ON c.GroupId = g.Id 
WHERE c.TargetType = 'Club' 
ORDER BY c.GroupId, c.Id;

PRINT '=== SUMMARY BY GROUP ===';
SELECT 
    g.Id,
    g.Name as GroupName,
    g.MaxScore as GroupMaxScore,
    COUNT(c.Id) as CriteriaCount,
    SUM(c.MaxScore) as TotalCriteriaMaxScore
FROM MovementCriterionGroups g
LEFT JOIN MovementCriteria c ON g.Id = c.GroupId AND c.IsActive = 1
WHERE g.TargetType = 'Student'
GROUP BY g.Id, g.Name, g.MaxScore
ORDER BY g.Id;

PRINT '=== SCRIPT COMPLETED SUCCESSFULLY ===';
