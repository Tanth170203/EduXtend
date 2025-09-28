using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class initialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicYears",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicYears", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Faculties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faculties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingCriteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MaxPoints = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingCriteria_TrainingCriteria_ParentId",
                        column: x => x.ParentId,
                        principalTable: "TrainingCriteria",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    GoogleSubject = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Semesters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AcademicYearId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semesters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Semesters_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Clubs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mission = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Achievements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ContactFacebook = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ContactOther = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FoundingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ManagerUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clubs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clubs_Users_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "News",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AuthorUserId = table.Column<int>(type: "int", nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_News", x => x.Id);
                    table.ForeignKey(
                        name: "FK_News_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Staffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    FacultyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Staffs_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Staffs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Revoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeviceInfo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassMeetings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    OrganizedById = table.Column<int>(type: "int", nullable: false),
                    MeetingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MeetingUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassMeetings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassMeetings_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassMeetings_Users_OrganizedById",
                        column: x => x.OrganizedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingEvaluationPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    SelfEvalStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SelfEvalEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClassReviewStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClassReviewEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdvisorReviewStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdvisorReviewEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinalizationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxActivityPointsPerStudent = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingEvaluationPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingEvaluationPeriods_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxParticipants = table.Column<int>(type: "int", nullable: true),
                    ActivityType = table.Column<int>(type: "int", nullable: false),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    ClubId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MaxScoreImpact = table.Column<int>(type: "int", nullable: true),
                    CriteriaReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Activities_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    StaffId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityApprovals_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityApprovals_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityPointRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: true),
                    ActivityTypeKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CriterionId = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    RequiresAttendance = table.Column<bool>(type: "bit", nullable: false),
                    RequiresEvidenceApproval = table.Column<bool>(type: "bit", nullable: false),
                    MaxPointsPerStudent = table.Column<int>(type: "int", nullable: true),
                    MaxTimesPerStudent = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityPointRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityPointRules_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActivityPointRules_TrainingCriteria_CriterionId",
                        column: x => x.CriterionId,
                        principalTable: "TrainingCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Present = table.Column<bool>(type: "bit", nullable: false),
                    CheckInAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckOutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckInMethod = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityAttendances_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityPointTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    CriterionId = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovedByStaffId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityPointTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityPointTransactions_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityPointTransactions_Staffs_ApprovedByStaffId",
                        column: x => x.ApprovedByStaffId,
                        principalTable: "Staffs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActivityPointTransactions_TrainingCriteria_CriterionId",
                        column: x => x.CriterionId,
                        principalTable: "TrainingCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    IsAttended = table.Column<bool>(type: "bit", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    ScoreImpact = table.Column<int>(type: "int", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityRegistrations_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Appeals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appeals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FacultyId = table.Column<int>(type: "int", nullable: false),
                    MonitorStudentId = table.Column<int>(type: "int", nullable: true),
                    StaffId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Classes_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Classes_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    EnrollmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    FacultyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Students_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Students_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Students_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClubMemberships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RoleInClub = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubMemberships_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClubMemberships_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    PeriodId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalSelfScore = table.Column<int>(type: "int", nullable: true),
                    TotalClassScore = table.Column<int>(type: "int", nullable: true),
                    TotalAdvisorScore = table.Column<int>(type: "int", nullable: true),
                    TotalFinalScore = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingEvaluations_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingEvaluations_TrainingEvaluationPeriods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "TrainingEvaluationPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationId = table.Column<int>(type: "int", nullable: false),
                    ChangedById = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationAuditLogs_TrainingEvaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "TrainingEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvaluationAuditLogs_Users_ChangedById",
                        column: x => x.ChangedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingEvaluationLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationId = table.Column<int>(type: "int", nullable: false),
                    CriterionId = table.Column<int>(type: "int", nullable: false),
                    SelfScore = table.Column<int>(type: "int", nullable: true),
                    ClassScore = table.Column<int>(type: "int", nullable: true),
                    AdvisorScore = table.Column<int>(type: "int", nullable: true),
                    FinalScore = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingEvaluationLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingEvaluationLines_TrainingCriteria_CriterionId",
                        column: x => x.CriterionId,
                        principalTable: "TrainingCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingEvaluationLines_TrainingEvaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "TrainingEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    EvaluationLineId = table.Column<int>(type: "int", nullable: true),
                    ActivityRegistrationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvidenceDocuments_ActivityRegistrations_ActivityRegistrationId",
                        column: x => x.ActivityRegistrationId,
                        principalTable: "ActivityRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvidenceDocuments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvidenceDocuments_TrainingEvaluationLines_EvaluationLineId",
                        column: x => x.EvaluationLineId,
                        principalTable: "TrainingEvaluationLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ClubId",
                table: "Activities",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_SemesterId",
                table: "Activities",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityApprovals_ActivityId",
                table: "ActivityApprovals",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityApprovals_StaffId",
                table: "ActivityApprovals",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityAttendances_ActivityId_StudentId",
                table: "ActivityAttendances",
                columns: new[] { "ActivityId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityAttendances_StudentId",
                table: "ActivityAttendances",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityPointRules_ActivityId",
                table: "ActivityPointRules",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityPointRules_CriterionId",
                table: "ActivityPointRules",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityPointTransactions_ActivityId",
                table: "ActivityPointTransactions",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityPointTransactions_ApprovedByStaffId",
                table: "ActivityPointTransactions",
                column: "ApprovedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityPointTransactions_CriterionId",
                table: "ActivityPointTransactions",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityPointTransactions_EvaluationId",
                table: "ActivityPointTransactions",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityPointTransactions_StudentId_ActivityId_CriterionId",
                table: "ActivityPointTransactions",
                columns: new[] { "StudentId", "ActivityId", "CriterionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityRegistrations_ActivityId_StudentId",
                table: "ActivityRegistrations",
                columns: new[] { "ActivityId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityRegistrations_StudentId",
                table: "ActivityRegistrations",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_EvaluationId",
                table: "Appeals",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_StudentId",
                table: "Appeals",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_FacultyId",
                table: "Classes",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_MonitorStudentId",
                table: "Classes",
                column: "MonitorStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_StaffId",
                table: "Classes",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassMeetings_OrganizedById",
                table: "ClassMeetings",
                column: "OrganizedById");

            migrationBuilder.CreateIndex(
                name: "IX_ClassMeetings_SemesterId",
                table: "ClassMeetings",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMemberships_ClubId_StudentId",
                table: "ClubMemberships",
                columns: new[] { "ClubId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubMemberships_StudentId",
                table: "ClubMemberships",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_ManagerUserId",
                table: "Clubs",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationAuditLogs_ChangedById",
                table: "EvaluationAuditLogs",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationAuditLogs_EvaluationId",
                table: "EvaluationAuditLogs",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_ActivityRegistrationId",
                table: "EvidenceDocuments",
                column: "ActivityRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_EvaluationLineId",
                table: "EvidenceDocuments",
                column: "EvaluationLineId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_StudentId",
                table: "EvidenceDocuments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_News_AuthorUserId",
                table: "News",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ActivityId",
                table: "Notifications",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Semesters_AcademicYearId",
                table: "Semesters",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_FacultyId",
                table: "Staffs",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_UserId",
                table: "Staffs",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Students_ClassId",
                table: "Students",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_FacultyId",
                table: "Students",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_UserId",
                table: "Students",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingCriteria_ParentId",
                table: "TrainingCriteria",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingEvaluationLines_CriterionId",
                table: "TrainingEvaluationLines",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingEvaluationLines_EvaluationId_CriterionId",
                table: "TrainingEvaluationLines",
                columns: new[] { "EvaluationId", "CriterionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingEvaluationPeriods_SemesterId",
                table: "TrainingEvaluationPeriods",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingEvaluations_PeriodId",
                table: "TrainingEvaluations",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingEvaluations_StudentId",
                table: "TrainingEvaluations",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTokens_UserId",
                table: "UserTokens",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityAttendances_Students_StudentId",
                table: "ActivityAttendances",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityPointTransactions_Students_StudentId",
                table: "ActivityPointTransactions",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityPointTransactions_TrainingEvaluations_EvaluationId",
                table: "ActivityPointTransactions",
                column: "EvaluationId",
                principalTable: "TrainingEvaluations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityRegistrations_Students_StudentId",
                table: "ActivityRegistrations",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Appeals_Students_StudentId",
                table: "Appeals",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Appeals_TrainingEvaluations_EvaluationId",
                table: "Appeals",
                column: "EvaluationId",
                principalTable: "TrainingEvaluations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Students_MonitorStudentId",
                table: "Classes",
                column: "MonitorStudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Staffs_StaffId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Students_MonitorStudentId",
                table: "Classes");

            migrationBuilder.DropTable(
                name: "ActivityApprovals");

            migrationBuilder.DropTable(
                name: "ActivityAttendances");

            migrationBuilder.DropTable(
                name: "ActivityPointRules");

            migrationBuilder.DropTable(
                name: "ActivityPointTransactions");

            migrationBuilder.DropTable(
                name: "Appeals");

            migrationBuilder.DropTable(
                name: "ClassMeetings");

            migrationBuilder.DropTable(
                name: "ClubMemberships");

            migrationBuilder.DropTable(
                name: "EvaluationAuditLogs");

            migrationBuilder.DropTable(
                name: "EvidenceDocuments");

            migrationBuilder.DropTable(
                name: "News");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "ActivityRegistrations");

            migrationBuilder.DropTable(
                name: "TrainingEvaluationLines");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "TrainingCriteria");

            migrationBuilder.DropTable(
                name: "TrainingEvaluations");

            migrationBuilder.DropTable(
                name: "Clubs");

            migrationBuilder.DropTable(
                name: "TrainingEvaluationPeriods");

            migrationBuilder.DropTable(
                name: "Semesters");

            migrationBuilder.DropTable(
                name: "AcademicYears");

            migrationBuilder.DropTable(
                name: "Staffs");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Classes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Faculties");
        }
    }
}
