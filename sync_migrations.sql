CREATE TABLE [ClubCategories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(255) NULL,
    CONSTRAINT [PK_ClubCategories] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Majors] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(10) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Majors] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [MovementCriterionGroups] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(255) NULL,
    [MaxScore] int NOT NULL,
    [TargetType] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_MovementCriterionGroups] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY,
    [RoleName] nvarchar(50) NOT NULL,
    [Description] nvarchar(200) NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Semesters] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(20) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Semesters] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Clubs] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [SubName] nvarchar(150) NOT NULL,
    [Description] nvarchar(500) NULL,
    [LogoUrl] nvarchar(max) NULL,
    [BannerUrl] nvarchar(max) NULL,
    [FoundedDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [IsRecruitmentOpen] bit NOT NULL,
    [CategoryId] int NOT NULL,
    CONSTRAINT [PK_Clubs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Clubs_ClubCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ClubCategories] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [MovementCriteria] (
    [Id] int NOT NULL IDENTITY,
    [GroupId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [MaxScore] int NOT NULL,
    [MinScore] int NULL,
    [TargetType] nvarchar(20) NOT NULL,
    [DataSource] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_MovementCriteria] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MovementCriteria_MovementCriterionGroups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [MovementCriterionGroups] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(100) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [GoogleSubject] nvarchar(100) NULL,
    [AvatarUrl] nvarchar(255) NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [LastLoginAt] datetime2 NULL,
    [RoleId] int NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ClubAwards] (
    [Id] int NOT NULL IDENTITY,
    [ClubId] int NOT NULL,
    [Title] nvarchar(150) NOT NULL,
    [Description] nvarchar(300) NULL,
    [SemesterId] int NULL,
    [AwardedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ClubAwards] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClubAwards_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ClubAwards_Semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [Semesters] ([Id])
);
GO


CREATE TABLE [ClubDepartments] (
    [Id] int NOT NULL IDENTITY,
    [ClubId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(255) NULL,
    CONSTRAINT [PK_ClubDepartments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClubDepartments_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ClubMovementRecords] (
    [Id] int NOT NULL IDENTITY,
    [ClubId] int NOT NULL,
    [SemesterId] int NOT NULL,
    [Month] int NOT NULL,
    [ClubMeetingScore] float NOT NULL,
    [EventScore] float NOT NULL,
    [CompetitionScore] float NOT NULL,
    [PlanScore] float NOT NULL,
    [CollaborationScore] float NOT NULL,
    [TotalScore] float NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [LastUpdated] datetime2 NULL,
    CONSTRAINT [PK_ClubMovementRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClubMovementRecords_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClubMovementRecords_Semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [Semesters] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Activities] (
    [Id] int NOT NULL IDENTITY,
    [ClubId] int NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [Location] nvarchar(255) NULL,
    [ImageUrl] nvarchar(500) NULL,
    [StartTime] datetime2 NOT NULL,
    [EndTime] datetime2 NOT NULL,
    [Type] int NOT NULL,
    [RequiresApproval] bit NOT NULL,
    [CreatedById] int NOT NULL,
    [IsPublic] bit NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [ApprovedById] int NULL,
    [ApprovedAt] datetime2 NULL,
    [RejectionReason] nvarchar(500) NULL,
    [MaxParticipants] int NULL,
    [MovementPoint] float NOT NULL,
    [ClubCollaborationId] int NULL,
    [CollaborationPoint] int NULL,
    [CollaborationStatus] nvarchar(50) NULL,
    [CollaborationRejectionReason] nvarchar(500) NULL,
    [CollaborationRespondedAt] datetime2 NULL,
    [CollaborationRespondedBy] int NULL,
    [AttendanceCode] nvarchar(6) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Activities] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Activities_Clubs_ClubCollaborationId] FOREIGN KEY ([ClubCollaborationId]) REFERENCES [Clubs] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Activities_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([Id]),
    CONSTRAINT [FK_Activities_Users_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Activities_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ClubNews] (
    [Id] int NOT NULL IDENTITY,
    [ClubId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Content] nvarchar(1000) NULL,
    [ImageUrl] nvarchar(max) NULL,
    [FacebookUrl] nvarchar(max) NULL,
    [PublishedAt] datetime2 NOT NULL,
    [IsApproved] bit NOT NULL,
    [CreatedById] int NOT NULL,
    CONSTRAINT [PK_ClubNews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClubNews_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ClubNews_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [FundCollectionRequests] (
    [Id] int NOT NULL IDENTITY,
    [ClubId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [AmountPerMember] decimal(18,2) NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [SemesterId] int NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [PaymentMethods] nvarchar(200) NULL,
    [Notes] nvarchar(1000) NULL,
    [CreatedById] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_FundCollectionRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FundCollectionRequests_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FundCollectionRequests_Semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [Semesters] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FundCollectionRequests_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [LoggedOutTokens] (
    [Id] int NOT NULL IDENTITY,
    [TokenHash] nvarchar(64) NOT NULL,
    [TokenFull] nvarchar(2000) NULL,
    [UserId] int NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [LoggedOutAt] datetime2 NOT NULL,
    [Reason] nvarchar(200) NULL,
    CONSTRAINT [PK_LoggedOutTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LoggedOutTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Notifications] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Message] nvarchar(500) NULL,
    [Scope] nvarchar(max) NOT NULL,
    [TargetClubId] int NULL,
    [TargetRole] nvarchar(50) NULL,
    [TargetUserId] int NULL,
    [CreatedById] int NOT NULL,
    [IsRead] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_Clubs_TargetClubId] FOREIGN KEY ([TargetClubId]) REFERENCES [Clubs] ([Id]),
    CONSTRAINT [FK_Notifications_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Notifications_Users_TargetUserId] FOREIGN KEY ([TargetUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Plans] (
    [Id] int NOT NULL IDENTITY,
    [ClubId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [SubmittedAt] datetime2 NULL,
    [ApprovedById] int NULL,
    [ApprovedAt] datetime2 NULL,
    [ReportType] nvarchar(50) NULL,
    [ReportMonth] int NULL,
    [ReportYear] int NULL,
    [ReportActivityIds] nvarchar(max) NULL,
    [ReportSnapshot] nvarchar(max) NULL,
    [RejectionReason] nvarchar(max) NULL,
    [EventMediaUrls] nvarchar(max) NULL,
    [NextMonthPurposeAndSignificance] nvarchar(max) NULL,
    [ClubResponsibilities] nvarchar(max) NULL,
    CONSTRAINT [PK_Plans] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Plans_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Plans_Users_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Proposals] (
    [Id] int NOT NULL IDENTITY,
    [ClubId] int NOT NULL,
    [CreatedById] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ClosedAt] datetime2 NULL,
    CONSTRAINT [PK_Proposals] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Proposals_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Proposals_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Students] (
    [Id] int NOT NULL IDENTITY,
    [StudentCode] nvarchar(20) NOT NULL,
    [Cohort] nvarchar(10) NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [Phone] nvarchar(15) NULL,
    [DateOfBirth] datetime2 NOT NULL,
    [Gender] int NOT NULL,
    [EnrollmentDate] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [UserId] int NOT NULL,
    [MajorId] int NOT NULL,
    CONSTRAINT [PK_Students] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Students_Majors_MajorId] FOREIGN KEY ([MajorId]) REFERENCES [Majors] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Students_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SystemNews] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Content] nvarchar(max) NULL,
    [ImageUrl] nvarchar(max) NULL,
    [FacebookUrl] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [PublishedAt] datetime2 NOT NULL,
    [CreatedById] int NOT NULL,
    CONSTRAINT [PK_SystemNews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SystemNews_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [UserTokens] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [RefreshToken] nvarchar(500) NOT NULL,
    [ExpiryDate] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [Revoked] bit NOT NULL,
    [RevokedAt] datetime2 NULL,
    [DeviceInfo] nvarchar(200) NULL,
    CONSTRAINT [PK_UserTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [JoinRequests] (
    [Id] int NOT NULL IDENTITY,
    [ClubId] int NOT NULL,
    [UserId] int NOT NULL,
    [DepartmentId] int NULL,
    [Motivation] nvarchar(500) NULL,
    [CvUrl] nvarchar(255) NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ProcessedAt] datetime2 NULL,
    [ProcessedById] int NULL,
    CONSTRAINT [PK_JoinRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_JoinRequests_ClubDepartments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [ClubDepartments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_JoinRequests_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_JoinRequests_Users_ProcessedById] FOREIGN KEY ([ProcessedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_JoinRequests_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ActivityAttendances] (
    [Id] int NOT NULL IDENTITY,
    [ActivityId] int NOT NULL,
    [UserId] int NOT NULL,
    [IsPresent] bit NOT NULL,
    [ParticipationScore] int NULL,
    [CheckedAt] datetime2 NOT NULL,
    [CheckedById] int NULL,
    CONSTRAINT [PK_ActivityAttendances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ActivityAttendances_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ActivityAttendances_Users_CheckedById] FOREIGN KEY ([CheckedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ActivityAttendances_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ActivityEvaluations] (
    [Id] int NOT NULL IDENTITY,
    [ActivityId] int NOT NULL,
    [ExpectedParticipants] int NOT NULL,
    [ActualParticipants] int NOT NULL,
    [Reason] nvarchar(1000) NULL,
    [CommunicationScore] int NOT NULL,
    [OrganizationScore] int NOT NULL,
    [HostScore] int NOT NULL,
    [SpeakerScore] int NOT NULL,
    [Success] int NOT NULL,
    [Limitations] nvarchar(2000) NULL,
    [ImprovementMeasures] nvarchar(2000) NULL,
    [AverageScore] float NOT NULL,
    [CreatedById] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ActivityEvaluations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ActivityEvaluations_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ActivityEvaluations_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ActivityFeedbacks] (
    [Id] int NOT NULL IDENTITY,
    [ActivityId] int NOT NULL,
    [UserId] int NOT NULL,
    [Rating] int NOT NULL,
    [Comment] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ActivityFeedbacks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ActivityFeedbacks_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ActivityFeedbacks_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ActivityRegistrations] (
    [Id] int NOT NULL IDENTITY,
    [ActivityId] int NOT NULL,
    [UserId] int NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ActivityRegistrations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ActivityRegistrations_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ActivityRegistrations_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ActivitySchedules] (
    [Id] int NOT NULL IDENTITY,
    [ActivityId] int NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [Title] nvarchar(500) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [Notes] nvarchar(1000) NULL,
    [Order] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ActivitySchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ActivitySchedules_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ClubMovementRecordDetails] (
    [Id] int NOT NULL IDENTITY,
    [ClubMovementRecordId] int NOT NULL,
    [CriterionId] int NOT NULL,
    [ActivityId] int NULL,
    [Score] float NOT NULL,
    [ScoreType] nvarchar(20) NOT NULL,
    [Note] nvarchar(500) NULL,
    [CreatedBy] int NULL,
    [AwardedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ClubMovementRecordDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClubMovementRecordDetails_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_ClubMovementRecordDetails_ClubMovementRecords_ClubMovementRecordId] FOREIGN KEY ([ClubMovementRecordId]) REFERENCES [ClubMovementRecords] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ClubMovementRecordDetails_MovementCriteria_CriterionId] FOREIGN KEY ([CriterionId]) REFERENCES [MovementCriteria] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClubMovementRecordDetails_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [CommunicationPlans] (
    [Id] int NOT NULL IDENTITY,
    [ActivityId] int NOT NULL,
    [ClubId] int NOT NULL,
    [CreatedById] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CommunicationPlans] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CommunicationPlans_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CommunicationPlans_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CommunicationPlans_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ProposalVotes] (
    [Id] int NOT NULL IDENTITY,
    [ProposalId] int NOT NULL,
    [UserId] int NOT NULL,
    [IsAgree] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ProposalVotes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProposalVotes_Proposals_ProposalId] FOREIGN KEY ([ProposalId]) REFERENCES [Proposals] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProposalVotes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ClubMembers] (
    [Id] int NOT NULL IDENTITY,
    [ClubId] int NOT NULL,
    [StudentId] int NOT NULL,
    [RoleInClub] nvarchar(50) NOT NULL,
    [DepartmentId] int NULL,
    [IsActive] bit NOT NULL,
    [JoinedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ClubMembers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClubMembers_ClubDepartments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [ClubDepartments] ([Id]),
    CONSTRAINT [FK_ClubMembers_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ClubMembers_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Evidences] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [ActivityId] int NULL,
    [CriterionId] int NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [FilePath] nvarchar(255) NULL,
    [Status] nvarchar(50) NOT NULL,
    [ReviewerComment] nvarchar(255) NULL,
    [ReviewedById] int NULL,
    [ReviewedAt] datetime2 NULL,
    [Points] float NOT NULL,
    [SubmittedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Evidences] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Evidences_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([Id]),
    CONSTRAINT [FK_Evidences_MovementCriteria_CriterionId] FOREIGN KEY ([CriterionId]) REFERENCES [MovementCriteria] ([Id]),
    CONSTRAINT [FK_Evidences_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Evidences_Users_ReviewedById] FOREIGN KEY ([ReviewedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [MovementRecords] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [SemesterId] int NOT NULL,
    [TotalScore] float NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [LastUpdated] datetime2 NULL,
    CONSTRAINT [PK_MovementRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MovementRecords_Semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [Semesters] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MovementRecords_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PaymentTransactions] (
    [Id] int NOT NULL IDENTITY,
    [ClubId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Type] nvarchar(20) NOT NULL,
    [Category] nvarchar(50) NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Description] nvarchar(500) NULL,
    [Notes] nvarchar(2000) NULL,
    [Method] nvarchar(50) NOT NULL,
    [ReceiptUrl] nvarchar(500) NULL,
    [StudentId] int NULL,
    [ActivityId] int NULL,
    [SemesterId] int NULL,
    [TransactionDate] datetime2 NOT NULL,
    [CreatedById] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PaymentTransactions_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PaymentTransactions_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PaymentTransactions_Semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [Semesters] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_PaymentTransactions_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PaymentTransactions_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Interviews] (
    [Id] int NOT NULL IDENTITY,
    [JoinRequestId] int NOT NULL,
    [ScheduledDate] datetime2 NOT NULL,
    [Location] nvarchar(200) NOT NULL,
    [Notes] nvarchar(1000) NULL,
    [Evaluation] nvarchar(2000) NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [CreatedById] int NOT NULL,
    CONSTRAINT [PK_Interviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Interviews_JoinRequests_JoinRequestId] FOREIGN KEY ([JoinRequestId]) REFERENCES [JoinRequests] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Interviews_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ActivityScheduleAssignments] (
    [Id] int NOT NULL IDENTITY,
    [ActivityScheduleId] int NOT NULL,
    [UserId] int NULL,
    [ResponsibleName] nvarchar(200) NULL,
    [Role] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ActivityScheduleAssignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ActivityScheduleAssignments_ActivitySchedules_ActivityScheduleId] FOREIGN KEY ([ActivityScheduleId]) REFERENCES [ActivitySchedules] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ActivityScheduleAssignments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [CommunicationItems] (
    [Id] int NOT NULL IDENTITY,
    [CommunicationPlanId] int NOT NULL,
    [Order] int NOT NULL,
    [Content] nvarchar(500) NOT NULL,
    [ScheduledDate] datetime2 NOT NULL,
    [ResponsiblePerson] nvarchar(200) NULL,
    [Notes] nvarchar(1000) NULL,
    CONSTRAINT [PK_CommunicationItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CommunicationItems_CommunicationPlans_CommunicationPlanId] FOREIGN KEY ([CommunicationPlanId]) REFERENCES [CommunicationPlans] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [MovementRecordDetails] (
    [Id] int NOT NULL IDENTITY,
    [MovementRecordId] int NOT NULL,
    [CriterionId] int NOT NULL,
    [ActivityId] int NULL,
    [ScoreType] nvarchar(max) NOT NULL,
    [CreatedBy] int NULL,
    [Note] nvarchar(max) NULL,
    [Score] float NOT NULL,
    [AwardedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MovementRecordDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MovementRecordDetails_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([Id]),
    CONSTRAINT [FK_MovementRecordDetails_MovementCriteria_CriterionId] FOREIGN KEY ([CriterionId]) REFERENCES [MovementCriteria] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MovementRecordDetails_MovementRecords_MovementRecordId] FOREIGN KEY ([MovementRecordId]) REFERENCES [MovementRecords] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MovementRecordDetails_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [FundCollectionPayments] (
    [Id] int NOT NULL IDENTITY,
    [FundCollectionRequestId] int NOT NULL,
    [ClubMemberId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [PaidAt] datetime2 NULL,
    [PaymentMethod] nvarchar(50) NULL,
    [PaymentTransactionId] int NULL,
    [Notes] nvarchar(500) NULL,
    [ConfirmedById] int NULL,
    [ReminderCount] int NOT NULL,
    [LastReminderAt] datetime2 NULL,
    [VnpayTransactionDetailId] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_FundCollectionPayments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FundCollectionPayments_ClubMembers_ClubMemberId] FOREIGN KEY ([ClubMemberId]) REFERENCES [ClubMembers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FundCollectionPayments_FundCollectionRequests_FundCollectionRequestId] FOREIGN KEY ([FundCollectionRequestId]) REFERENCES [FundCollectionRequests] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_FundCollectionPayments_PaymentTransactions_PaymentTransactionId] FOREIGN KEY ([PaymentTransactionId]) REFERENCES [PaymentTransactions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FundCollectionPayments_Users_ConfirmedById] FOREIGN KEY ([ConfirmedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ActivityMemberEvaluations] (
    [Id] int NOT NULL IDENTITY,
    [ActivityScheduleAssignmentId] int NOT NULL,
    [EvaluatorId] int NOT NULL,
    [ResponsibilityScore] int NOT NULL,
    [SkillScore] int NOT NULL,
    [AttitudeScore] int NOT NULL,
    [EffectivenessScore] int NOT NULL,
    [AverageScore] float NOT NULL,
    [Comments] nvarchar(2000) NULL,
    [Strengths] nvarchar(1000) NULL,
    [Improvements] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ActivityMemberEvaluations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ActivityMemberEvaluations_ActivityScheduleAssignments_ActivityScheduleAssignmentId] FOREIGN KEY ([ActivityScheduleAssignmentId]) REFERENCES [ActivityScheduleAssignments] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ActivityMemberEvaluations_Users_EvaluatorId] FOREIGN KEY ([EvaluatorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [VnpayTransactionDetails] (
    [Id] int NOT NULL IDENTITY,
    [FundCollectionPaymentId] int NOT NULL,
    [VnpayTransactionId] bigint NOT NULL,
    [BankCode] nvarchar(20) NULL,
    [BankTransactionId] nvarchar(255) NULL,
    [ResponseCode] nvarchar(10) NOT NULL,
    [OrderInfo] nvarchar(500) NULL,
    [Amount] decimal(18,2) NOT NULL,
    [TransactionDate] datetime2 NULL,
    [TransactionStatus] nvarchar(20) NOT NULL,
    [SecureHash] nvarchar(500) NULL,
    [IpAddress] nvarchar(50) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_VnpayTransactionDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_VnpayTransactionDetails_FundCollectionPayments_FundCollectionPaymentId] FOREIGN KEY ([FundCollectionPaymentId]) REFERENCES [FundCollectionPayments] ([Id]) ON DELETE CASCADE
);
GO


CREATE INDEX [IX_Activities_ApprovedById] ON [Activities] ([ApprovedById]);
GO


CREATE INDEX [IX_Activities_ClubCollaborationId] ON [Activities] ([ClubCollaborationId]);
GO


CREATE INDEX [IX_Activities_ClubId] ON [Activities] ([ClubId]);
GO


CREATE INDEX [IX_Activities_CreatedById] ON [Activities] ([CreatedById]);
GO


CREATE UNIQUE INDEX [IX_ActivityAttendances_ActivityId_UserId] ON [ActivityAttendances] ([ActivityId], [UserId]);
GO


CREATE INDEX [IX_ActivityAttendances_CheckedById] ON [ActivityAttendances] ([CheckedById]);
GO


CREATE INDEX [IX_ActivityAttendances_UserId] ON [ActivityAttendances] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_ActivityEvaluations_ActivityId] ON [ActivityEvaluations] ([ActivityId]);
GO


CREATE INDEX [IX_ActivityEvaluations_CreatedById] ON [ActivityEvaluations] ([CreatedById]);
GO


CREATE INDEX [IX_ActivityFeedbacks_ActivityId] ON [ActivityFeedbacks] ([ActivityId]);
GO


CREATE INDEX [IX_ActivityFeedbacks_UserId] ON [ActivityFeedbacks] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_ActivityMemberEvaluations_ActivityScheduleAssignmentId] ON [ActivityMemberEvaluations] ([ActivityScheduleAssignmentId]);
GO


CREATE INDEX [IX_ActivityMemberEvaluations_EvaluatorId] ON [ActivityMemberEvaluations] ([EvaluatorId]);
GO


CREATE UNIQUE INDEX [IX_ActivityRegistrations_ActivityId_UserId] ON [ActivityRegistrations] ([ActivityId], [UserId]);
GO


CREATE INDEX [IX_ActivityRegistrations_UserId] ON [ActivityRegistrations] ([UserId]);
GO


CREATE INDEX [IX_ActivityScheduleAssignments_ActivityScheduleId] ON [ActivityScheduleAssignments] ([ActivityScheduleId]);
GO


CREATE INDEX [IX_ActivityScheduleAssignments_UserId] ON [ActivityScheduleAssignments] ([UserId]);
GO


CREATE INDEX [IX_ActivitySchedules_ActivityId_Order] ON [ActivitySchedules] ([ActivityId], [Order]);
GO


CREATE INDEX [IX_ClubAwards_ClubId] ON [ClubAwards] ([ClubId]);
GO


CREATE INDEX [IX_ClubAwards_SemesterId] ON [ClubAwards] ([SemesterId]);
GO


CREATE INDEX [IX_ClubDepartments_ClubId] ON [ClubDepartments] ([ClubId]);
GO


CREATE UNIQUE INDEX [IX_ClubMembers_ClubId_StudentId] ON [ClubMembers] ([ClubId], [StudentId]);
GO


CREATE INDEX [IX_ClubMembers_DepartmentId] ON [ClubMembers] ([DepartmentId]);
GO


CREATE INDEX [IX_ClubMembers_StudentId] ON [ClubMembers] ([StudentId]);
GO


CREATE INDEX [IX_ClubMovementRecordDetails_ActivityId] ON [ClubMovementRecordDetails] ([ActivityId]);
GO


CREATE INDEX [IX_ClubMovementRecordDetails_ClubMovementRecordId_CriterionId] ON [ClubMovementRecordDetails] ([ClubMovementRecordId], [CriterionId]);
GO


CREATE INDEX [IX_ClubMovementRecordDetails_CreatedBy] ON [ClubMovementRecordDetails] ([CreatedBy]);
GO


CREATE INDEX [IX_ClubMovementRecordDetails_CriterionId] ON [ClubMovementRecordDetails] ([CriterionId]);
GO


CREATE UNIQUE INDEX [IX_ClubMovementRecords_ClubId_SemesterId_Month] ON [ClubMovementRecords] ([ClubId], [SemesterId], [Month]);
GO


CREATE INDEX [IX_ClubMovementRecords_SemesterId] ON [ClubMovementRecords] ([SemesterId]);
GO


CREATE INDEX [IX_ClubNews_ClubId] ON [ClubNews] ([ClubId]);
GO


CREATE INDEX [IX_ClubNews_CreatedById] ON [ClubNews] ([CreatedById]);
GO


CREATE INDEX [IX_Clubs_CategoryId] ON [Clubs] ([CategoryId]);
GO


CREATE INDEX [IX_CommunicationItems_CommunicationPlanId_Order] ON [CommunicationItems] ([CommunicationPlanId], [Order]);
GO


CREATE UNIQUE INDEX [IX_CommunicationPlans_ActivityId] ON [CommunicationPlans] ([ActivityId]);
GO


CREATE INDEX [IX_CommunicationPlans_ClubId] ON [CommunicationPlans] ([ClubId]);
GO


CREATE INDEX [IX_CommunicationPlans_CreatedById] ON [CommunicationPlans] ([CreatedById]);
GO


CREATE INDEX [IX_Evidences_ActivityId] ON [Evidences] ([ActivityId]);
GO


CREATE INDEX [IX_Evidences_CriterionId] ON [Evidences] ([CriterionId]);
GO


CREATE INDEX [IX_Evidences_ReviewedById] ON [Evidences] ([ReviewedById]);
GO


CREATE INDEX [IX_Evidences_StudentId] ON [Evidences] ([StudentId]);
GO


CREATE INDEX [IX_FundCollectionPayments_ClubMemberId] ON [FundCollectionPayments] ([ClubMemberId]);
GO


CREATE INDEX [IX_FundCollectionPayments_ConfirmedById] ON [FundCollectionPayments] ([ConfirmedById]);
GO


CREATE UNIQUE INDEX [IX_FundCollectionPayments_FundCollectionRequestId_ClubMemberId] ON [FundCollectionPayments] ([FundCollectionRequestId], [ClubMemberId]);
GO


CREATE INDEX [IX_FundCollectionPayments_PaymentTransactionId] ON [FundCollectionPayments] ([PaymentTransactionId]);
GO


CREATE INDEX [IX_FundCollectionPayments_Status] ON [FundCollectionPayments] ([Status]);
GO


CREATE INDEX [IX_FundCollectionRequests_ClubId_Status] ON [FundCollectionRequests] ([ClubId], [Status]);
GO


CREATE INDEX [IX_FundCollectionRequests_CreatedById] ON [FundCollectionRequests] ([CreatedById]);
GO


CREATE INDEX [IX_FundCollectionRequests_DueDate] ON [FundCollectionRequests] ([DueDate]);
GO


CREATE INDEX [IX_FundCollectionRequests_SemesterId_Status] ON [FundCollectionRequests] ([SemesterId], [Status]);
GO


CREATE INDEX [IX_Interviews_CreatedById] ON [Interviews] ([CreatedById]);
GO


CREATE INDEX [IX_Interviews_JoinRequestId] ON [Interviews] ([JoinRequestId]);
GO


CREATE INDEX [IX_JoinRequests_ClubId] ON [JoinRequests] ([ClubId]);
GO


CREATE INDEX [IX_JoinRequests_DepartmentId] ON [JoinRequests] ([DepartmentId]);
GO


CREATE INDEX [IX_JoinRequests_ProcessedById] ON [JoinRequests] ([ProcessedById]);
GO


CREATE INDEX [IX_JoinRequests_UserId] ON [JoinRequests] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_LoggedOutTokens_TokenHash] ON [LoggedOutTokens] ([TokenHash]);
GO


CREATE INDEX [IX_LoggedOutTokens_UserId] ON [LoggedOutTokens] ([UserId]);
GO


CREATE INDEX [IX_MovementCriteria_GroupId] ON [MovementCriteria] ([GroupId]);
GO


CREATE INDEX [IX_MovementRecordDetails_ActivityId] ON [MovementRecordDetails] ([ActivityId]);
GO


CREATE INDEX [IX_MovementRecordDetails_CreatedBy] ON [MovementRecordDetails] ([CreatedBy]);
GO


CREATE INDEX [IX_MovementRecordDetails_CriterionId] ON [MovementRecordDetails] ([CriterionId]);
GO


CREATE INDEX [IX_MovementRecordDetails_MovementRecordId_CriterionId] ON [MovementRecordDetails] ([MovementRecordId], [CriterionId]);
GO


CREATE INDEX [IX_MovementRecordDetails_MovementRecordId_CriterionId_ActivityId] ON [MovementRecordDetails] ([MovementRecordId], [CriterionId], [ActivityId]);
GO


CREATE INDEX [IX_MovementRecords_SemesterId] ON [MovementRecords] ([SemesterId]);
GO


CREATE UNIQUE INDEX [IX_MovementRecords_StudentId_SemesterId] ON [MovementRecords] ([StudentId], [SemesterId]);
GO


CREATE INDEX [IX_Notifications_CreatedById] ON [Notifications] ([CreatedById]);
GO


CREATE INDEX [IX_Notifications_TargetClubId] ON [Notifications] ([TargetClubId]);
GO


CREATE INDEX [IX_Notifications_TargetUserId] ON [Notifications] ([TargetUserId]);
GO


CREATE INDEX [IX_PaymentTransactions_ActivityId] ON [PaymentTransactions] ([ActivityId]);
GO


CREATE INDEX [IX_PaymentTransactions_ClubId_TransactionDate] ON [PaymentTransactions] ([ClubId], [TransactionDate]);
GO


CREATE INDEX [IX_PaymentTransactions_CreatedById] ON [PaymentTransactions] ([CreatedById]);
GO


CREATE INDEX [IX_PaymentTransactions_SemesterId_Type_Status] ON [PaymentTransactions] ([SemesterId], [Type], [Status]);
GO


CREATE INDEX [IX_PaymentTransactions_Status] ON [PaymentTransactions] ([Status]);
GO


CREATE INDEX [IX_PaymentTransactions_StudentId] ON [PaymentTransactions] ([StudentId]);
GO


CREATE INDEX [IX_PaymentTransactions_Type] ON [PaymentTransactions] ([Type]);
GO


CREATE INDEX [IX_Plans_ApprovedById] ON [Plans] ([ApprovedById]);
GO


CREATE INDEX [IX_Plans_ClubId_ReportMonth_ReportYear_ReportType] ON [Plans] ([ClubId], [ReportMonth], [ReportYear], [ReportType]);
GO


CREATE INDEX [IX_Proposals_ClubId] ON [Proposals] ([ClubId]);
GO


CREATE INDEX [IX_Proposals_CreatedById] ON [Proposals] ([CreatedById]);
GO


CREATE UNIQUE INDEX [IX_ProposalVotes_ProposalId_UserId] ON [ProposalVotes] ([ProposalId], [UserId]);
GO


CREATE INDEX [IX_ProposalVotes_UserId] ON [ProposalVotes] ([UserId]);
GO


CREATE INDEX [IX_Students_MajorId] ON [Students] ([MajorId]);
GO


CREATE UNIQUE INDEX [IX_Students_UserId] ON [Students] ([UserId]);
GO


CREATE INDEX [IX_SystemNews_CreatedById] ON [SystemNews] ([CreatedById]);
GO


CREATE INDEX [IX_Users_RoleId] ON [Users] ([RoleId]);
GO


CREATE INDEX [IX_UserTokens_UserId] ON [UserTokens] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_VnpayTransactionDetails_FundCollectionPaymentId] ON [VnpayTransactionDetails] ([FundCollectionPaymentId]);
GO


CREATE INDEX [IX_VnpayTransactionDetails_TransactionStatus] ON [VnpayTransactionDetails] ([TransactionStatus]);
GO


CREATE UNIQUE INDEX [IX_VnpayTransactionDetails_VnpayTransactionId] ON [VnpayTransactionDetails] ([VnpayTransactionId]);
GO


