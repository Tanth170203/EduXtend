-- Run this script on Azure SQL Database to sync migration history
-- This will mark all existing migrations as applied

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251008110405_InitialCreate')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251008110405_InitialCreate', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251008230413_AddLoggedOutTokensTable')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251008230413_AddLoggedOutTokensTable', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251008230559_UpdateLoggedOutTokenColumnLength')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251008230559_UpdateLoggedOutTokenColumnLength', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251009094424_UpdateCriteriaTable')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251009094424_UpdateCriteriaTable', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251016072805_AddCorhotAddCohortColumnToStudents')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251016072805_AddCorhotAddCohortColumnToStudents', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251016080722_UpdateStudentTable')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251016080722_UpdateStudentTable', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251017085350_AdIMGcolumnInActivitiesTable')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251017085350_AdIMGcolumnInActivitiesTable', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251022181154_AddRecruitmentAndJoinRequestFields')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251022181154_AddRecruitmentAndJoinRequestFields', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251022200408_UpdateDatabase')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251022200408_UpdateDatabase', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251023034636_UpdateLoggedOutTokenWithHash')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251023034636_UpdateLoggedOutTokenWithHash', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251023035137_AddTokenFullColumn')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251023035137_AddTokenFullColumn', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251023040252_FixLoggedOutTokenSchema')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251023040252_FixLoggedOutTokenSchema', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251026211502_AllowMultipleScoresPerCriterion')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251026211502_AllowMultipleScoresPerCriterion', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251027071519_AddClubMovementRecords')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251027071519_AddClubMovementRecords', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251027162515_AddAuditFieldsToMovementRecordDetails')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251027162515_AddAuditFieldsToMovementRecordDetails', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251107084952_AddActivityScheduleTables')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251107084952_AddActivityScheduleTables', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251107085913_UpdatePaymentTransactionTable')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251107085913_UpdatePaymentTransactionTable', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251107091416_AddFundCollectionTables')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251107091416_AddFundCollectionTables', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251108214013_AddSemesterToFinancials')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251108214013_AddSemesterToFinancials', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251110221103_AddRejectionReasonToActivity')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251110221103_AddRejectionReasonToActivity', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251110230938_AddParticipationScoreToAttendance')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251110230938_AddParticipationScoreToAttendance', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251116085820_AddAttendanceCodeToActivity')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251116085820_AddAttendanceCodeToActivity', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251119104813_AddActivityCollaborationFields')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251119104813_AddActivityCollaborationFields', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251119110643_AddActivityEvaluation')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251119110643_AddActivityEvaluation', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251119135508_AddCollaborationInvitationFields')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251119135508_AddCollaborationInvitationFields', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251119162248_RemoveCollaborationResponderId')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251119162248_RemoveCollaborationResponderId', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251120065520_AddActivityMemberEvaluationAndCommunicationPlan')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251120065520_AddActivityMemberEvaluationAndCommunicationPlan', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251120092139_AddMonthlyReportFieldsToPlan')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251120092139_AddMonthlyReportFieldsToPlan', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251120200927_AddVnpayAndAttendanceCode')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251120200927_AddVnpayAndAttendanceCode', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251121071925_AddRejectionReasonToPlans')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251121071925_AddRejectionReasonToPlans', '9.0.10');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251121150223_AddEditableSectionsToPlans')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251121150223_AddEditableSectionsToPlans', '9.0.10');

SELECT * FROM [__EFMigrationsHistory];
