IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [Exam] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Code] nvarchar(6) NOT NULL,
        [Slug] nvarchar(max) NOT NULL,
        [QuestionsCount] int NOT NULL,
        [DurationMinutes] int NULL,
        [PassingPercentage] int NOT NULL DEFAULT 70,
        [Status] int NOT NULL,
        [CreatedOnUtc] datetime2 NOT NULL,
        [LastModifiedOnUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedOnUtc] datetime2 NULL,
        CONSTRAINT [PK_Exam] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [ExamAttempts] (
        [Id] uniqueidentifier NOT NULL,
        [ExamId] uniqueidentifier NOT NULL,
        [ExamNameSnapshot] nvarchar(500) NOT NULL,
        [ExamDescriptionSnapshot] nvarchar(max) NOT NULL,
        [ExamSlugSnapshot] nvarchar(500) NOT NULL,
        [Culture] nvarchar(2) NOT NULL DEFAULT N'fr',
        [PassingPercentageSnapshot] int NOT NULL,
        [UserId] nvarchar(450) NULL,
        [GuestEmail] nvarchar(256) NULL,
        [AnonymousTokenHash] nvarchar(128) NULL,
        [StartedAtUtc] datetime2 NOT NULL,
        [ExpiresAtUtc] datetime2 NULL,
        [CompletedAtUtc] datetime2 NULL,
        [Status] int NOT NULL,
        [Score] decimal(18,2) NULL,
        [MaxScore] decimal(18,2) NULL,
        [Percentage] decimal(18,2) NULL,
        [Passed] bit NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedOnUtc] datetime2 NOT NULL,
        [LastModifiedOnUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedOnUtc] datetime2 NULL,
        CONSTRAINT [PK_ExamAttempts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExamAttempts_Exam_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exam] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [ExamTranslations] (
        [Id] uniqueidentifier NOT NULL,
        [Culture] nvarchar(2) NOT NULL,
        [ExamId] uniqueidentifier NOT NULL,
        [Name] nvarchar(160) NULL,
        [Description] nvarchar(2000) NULL,
        [Slug] nvarchar(180) NULL,
        [CreatedOnUtc] datetime2 NOT NULL,
        [LastModifiedOnUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedOnUtc] datetime2 NULL,
        CONSTRAINT [PK_ExamTranslations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExamTranslations_Exam_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exam] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [Skill] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Pourcentage] int NOT NULL,
        [ExamId] uniqueidentifier NULL,
        [CreatedOnUtc] datetime2 NOT NULL,
        [LastModifiedOnUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedOnUtc] datetime2 NULL,
        CONSTRAINT [PK_Skill] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Skill_Exam_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exam] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [ExamAttemptQuestions] (
        [Id] uniqueidentifier NOT NULL,
        [ExamAttemptId] uniqueidentifier NOT NULL,
        [SourceQuestionId] uniqueidentifier NOT NULL,
        [Order] int NOT NULL,
        [Points] decimal(18,2) NOT NULL,
        [QuestionTextSnapshot] nvarchar(4000) NOT NULL,
        [ExplanationSnapshot] nvarchar(4000) NULL,
        [QuestionTypeSnapshot] int NOT NULL,
        [SkillNameSnapshot] nvarchar(256) NULL,
        [CreatedOnUtc] datetime2 NOT NULL,
        [LastModifiedOnUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedOnUtc] datetime2 NULL,
        CONSTRAINT [PK_ExamAttemptQuestions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExamAttemptQuestions_ExamAttempts_ExamAttemptId] FOREIGN KEY ([ExamAttemptId]) REFERENCES [ExamAttempts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [Question] (
        [Id] uniqueidentifier NOT NULL,
        [Description] nvarchar(max) NULL,
        [Explication] nvarchar(max) NULL,
        [FillInBlank] nvarchar(max) NULL,
        [QuestionType] int NOT NULL,
        [Status] int NOT NULL,
        [ExamId] uniqueidentifier NULL,
        [SkillId] uniqueidentifier NULL,
        [CreatedOnUtc] datetime2 NOT NULL,
        [LastModifiedOnUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedOnUtc] datetime2 NULL,
        CONSTRAINT [PK_Question] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Question_Exam_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exam] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Question_Skill_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [Skill] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [SkillTranslations] (
        [Id] uniqueidentifier NOT NULL,
        [Culture] nvarchar(2) NOT NULL,
        [SkillId] uniqueidentifier NOT NULL,
        [Name] nvarchar(140) NULL,
        [Description] nvarchar(1000) NULL,
        [CreatedOnUtc] datetime2 NOT NULL,
        [LastModifiedOnUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedOnUtc] datetime2 NULL,
        CONSTRAINT [PK_SkillTranslations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SkillTranslations_Skill_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [Skill] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [ExamAnswers] (
        [Id] uniqueidentifier NOT NULL,
        [ExamAttemptQuestionId] uniqueidentifier NOT NULL,
        [SelectedChoiceIdsJson] nvarchar(max) NULL,
        [TextAnswer] nvarchar(4000) NULL,
        [SavedAtUtc] datetime2 NOT NULL,
        [IsCorrect] bit NULL,
        [ScoreAwarded] decimal(18,2) NOT NULL,
        [CreatedOnUtc] datetime2 NOT NULL,
        [LastModifiedOnUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedOnUtc] datetime2 NULL,
        CONSTRAINT [PK_ExamAnswers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExamAnswers_ExamAttemptQuestions_ExamAttemptQuestionId] FOREIGN KEY ([ExamAttemptQuestionId]) REFERENCES [ExamAttemptQuestions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [ExamAttemptChoices] (
        [Id] uniqueidentifier NOT NULL,
        [ExamAttemptQuestionId] uniqueidentifier NOT NULL,
        [SourceChoiceId] uniqueidentifier NOT NULL,
        [Order] int NOT NULL,
        [ChoiceTextSnapshot] nvarchar(2000) NOT NULL,
        [GroupBySnapshot] nvarchar(500) NULL,
        [IsCorrectSnapshot] bit NOT NULL,
        [CreatedOnUtc] datetime2 NOT NULL,
        [LastModifiedOnUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedOnUtc] datetime2 NULL,
        CONSTRAINT [PK_ExamAttemptChoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExamAttemptChoices_ExamAttemptQuestions_ExamAttemptQuestionId] FOREIGN KEY ([ExamAttemptQuestionId]) REFERENCES [ExamAttemptQuestions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [Choice] (
        [Id] uniqueidentifier NOT NULL,
        [ChoiceText] nvarchar(max) NULL,
        [IsCorrect] bit NOT NULL,
        [GroupBy] nvarchar(max) NULL,
        [QuestionId] uniqueidentifier NULL,
        [CreatedOnUtc] datetime2 NOT NULL,
        [LastModifiedOnUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedOnUtc] datetime2 NULL,
        CONSTRAINT [PK_Choice] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Choice_Question_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Question] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [QuestionTranslations] (
        [Id] uniqueidentifier NOT NULL,
        [Culture] nvarchar(2) NOT NULL,
        [QuestionId] uniqueidentifier NOT NULL,
        [Description] nvarchar(4000) NULL,
        [Explanation] nvarchar(4000) NULL,
        [FillInBlank] nvarchar(1000) NULL,
        [CreatedOnUtc] datetime2 NOT NULL,
        [LastModifiedOnUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedOnUtc] datetime2 NULL,
        CONSTRAINT [PK_QuestionTranslations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuestionTranslations_Question_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Question] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE TABLE [ChoiceTranslations] (
        [Id] uniqueidentifier NOT NULL,
        [Culture] nvarchar(2) NOT NULL,
        [ChoiceId] uniqueidentifier NOT NULL,
        [ChoiceText] nvarchar(1000) NULL,
        [GroupBy] nvarchar(200) NULL,
        [CreatedOnUtc] datetime2 NOT NULL,
        [LastModifiedOnUtc] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedOnUtc] datetime2 NULL,
        CONSTRAINT [PK_ChoiceTranslations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChoiceTranslations_Choice_ChoiceId] FOREIGN KEY ([ChoiceId]) REFERENCES [Choice] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE INDEX [IX_Choice_QuestionId] ON [Choice] ([QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ChoiceTranslations_ChoiceId_Culture] ON [ChoiceTranslations] ([ChoiceId], [Culture]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExamAnswers_ExamAttemptQuestionId] ON [ExamAnswers] ([ExamAttemptQuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExamAttemptChoices_ExamAttemptQuestionId_Order] ON [ExamAttemptChoices] ([ExamAttemptQuestionId], [Order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExamAttemptQuestions_ExamAttemptId_Order] ON [ExamAttemptQuestions] ([ExamAttemptId], [Order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE INDEX [IX_ExamAttempts_AnonymousTokenHash] ON [ExamAttempts] ([AnonymousTokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE INDEX [IX_ExamAttempts_ExamId_Status] ON [ExamAttempts] ([ExamId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE INDEX [IX_ExamAttempts_UserId_Status] ON [ExamAttempts] ([UserId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ExamTranslations_Culture_Slug] ON [ExamTranslations] ([Culture], [Slug]) WHERE [Slug] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExamTranslations_ExamId_Culture] ON [ExamTranslations] ([ExamId], [Culture]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE INDEX [IX_Question_ExamId] ON [Question] ([ExamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE INDEX [IX_Question_SkillId] ON [Question] ([SkillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_QuestionTranslations_QuestionId_Culture] ON [QuestionTranslations] ([QuestionId], [Culture]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE INDEX [IX_Skill_ExamId] ON [Skill] ([ExamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SkillTranslations_SkillId_Culture] ON [SkillTranslations] ([SkillId], [Culture]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914230047_InitialCertivaSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260914230047_InitialCertivaSchema', N'9.0.5');
END;

COMMIT;
GO

