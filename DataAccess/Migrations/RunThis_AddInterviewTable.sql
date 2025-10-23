-- Run this SQL script in your database to create Interviews table

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Interviews')
BEGIN
    CREATE TABLE [dbo].[Interviews](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [JoinRequestId] [int] NOT NULL,
        [ScheduledDate] [datetime2](7) NOT NULL,
        [Location] [nvarchar](200) NOT NULL,
        [Notes] [nvarchar](1000) NULL,
        [Evaluation] [nvarchar](2000) NULL,
        [Status] [nvarchar](50) NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        [CompletedAt] [datetime2](7) NULL,
        [CreatedById] [int] NOT NULL,
        CONSTRAINT [PK_Interviews] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_Interviews_JoinRequestId] ON [dbo].[Interviews]
    (
        [JoinRequestId] ASC
    );

    CREATE NONCLUSTERED INDEX [IX_Interviews_CreatedById] ON [dbo].[Interviews]
    (
        [CreatedById] ASC
    );

    ALTER TABLE [dbo].[Interviews] WITH CHECK ADD CONSTRAINT [FK_Interviews_JoinRequests_JoinRequestId] 
    FOREIGN KEY([JoinRequestId]) REFERENCES [dbo].[JoinRequests] ([Id]);

    ALTER TABLE [dbo].[Interviews] CHECK CONSTRAINT [FK_Interviews_JoinRequests_JoinRequestId];

    ALTER TABLE [dbo].[Interviews] WITH CHECK ADD CONSTRAINT [FK_Interviews_Users_CreatedById] 
    FOREIGN KEY([CreatedById]) REFERENCES [dbo].[Users] ([Id]);

    ALTER TABLE [dbo].[Interviews] CHECK CONSTRAINT [FK_Interviews_Users_CreatedById];

    PRINT 'Interviews table created successfully!';
END
ELSE
BEGIN
    PRINT 'Interviews table already exists.';
END

-- Insert migration history record
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251023020000_AddInterviewTable')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251023020000_AddInterviewTable', N'6.0.0');
    PRINT 'Migration history updated.';
END

