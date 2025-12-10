using BusinessObject.DTOs.GGLogin;
using DataAccess;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Repositories.Activities;
using Repositories.ActivitySchedules;
using Repositories.ActivityScheduleAssignments;
using Repositories.Clubs;
using Repositories.ClubMembers;
using Repositories.PaymentTransactions;
using Repositories.LoggedOutTokens;
using Repositories.Majors;
using Repositories.MovementCriteria;
using Repositories.Semesters;
using Repositories.Students;
using Repositories.Evidences;
using Repositories.MovementRecords;
using Repositories.Users;
using Repositories.JoinRequests;
using Repositories.Interviews;
using Repositories.FundCollectionRequests;
using Repositories.FundCollectionPayments;
using Repositories.Notifications;
using Services.Activities;
using Services.Clubs;
using Services.GGLogin;
using Services.MovementCriteria;
using Services.Semesters;
using Services.TokenCleanup;
using Services.Evidences;
using Services.MovementRecords;
using Services.UserImport;
using Services.JoinRequests;
using Services.Interviews;
using Services.FundCollections;
using Services.FinancialDashboard;
using Repositories.ClubMovementRecords;
using Services.ClubMovementRecords;
using Repositories.Proposals;
using Services.Proposals;
using Repositories.ActivityMemberEvaluations;
using Services.ActivityMemberEvaluations;
using Repositories.ActivityEvaluations;
using Repositories.CommunicationPlans;
using Services.CommunicationPlans;
using Repositories.MonthlyReports;
using Services.MonthlyReports;
using System.IdentityModel.Tokens.Jwt;
using WebAPI.Authentication;
using WebAPI.Middleware;
using Microsoft.OpenApi.Models;
using VNPAY.Extensions;
using AspNetCoreRateLimit;

namespace WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Set timezone to Vietnam (UTC+7)
            TimeZoneInfo vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            
            var builder = WebApplication.CreateBuilder(args);

            //deploy ( chạy nhớ comment lần nây )
            //builder.WebHost.UseUrls($"http://*:80");


            // DbContext
            builder.Services.AddDbContext<EduXtendContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Memory Cache
            builder.Services.AddMemoryCache();

            // Rate Limiting Configuration
            builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
            builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
            builder.Services.AddInMemoryRateLimiting();
            builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            // Options
            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
            builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection("GoogleAuth"));
            builder.Services.Configure<Services.Chatbot.GeminiAIOptions>(builder.Configuration.GetSection("GeminiAI"));

            // HttpClient for Gemini AI
            builder.Services.AddHttpClient("GeminiAI", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // VNPAY Configuration
            var vnpayConfig = builder.Configuration.GetSection("VNPAY");
            builder.Services.AddVnpayClient(config =>
            {
                config.TmnCode = vnpayConfig["TmnCode"]!;
                config.HashSecret = vnpayConfig["HashSecret"]!;
                config.CallbackUrl = vnpayConfig["CallbackUrl"]!;
                config.BaseUrl = vnpayConfig["BaseUrl"]!;
                config.Version = vnpayConfig["Version"]!;
                config.OrderType = vnpayConfig["OrderType"]!;
            });

            // Repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ISemesterRepository, SemesterRepository>();
            builder.Services.AddScoped<ILoggedOutTokenRepository, LoggedOutTokenRepository>();
            builder.Services.AddScoped<IMovementCriterionGroupRepository, MovementCriterionGroupRepository>();
            builder.Services.AddScoped<IMovementCriterionRepository, MovementCriterionRepository>();
            builder.Services.AddScoped<IStudentRepository, StudentRepository>();
            builder.Services.AddScoped<IMajorRepository, MajorRepository>();
            builder.Services.AddScoped<IEvidenceRepository, EvidenceRepository>();
            builder.Services.AddScoped<IMovementRecordRepository, MovementRecordRepository>();
            builder.Services.AddScoped<IMovementRecordDetailRepository, MovementRecordDetailRepository>();
            builder.Services.AddScoped<Repositories.Roles.IRoleRepository, Repositories.Roles.RoleRepository>();
            
            builder.Services.AddScoped<IClubRepository, ClubRepository>();
            builder.Services.AddScoped<IClubMemberRepository, ClubMemberRepository>();
            builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
            builder.Services.AddScoped<IActivityScheduleRepository, ActivityScheduleRepository>();
            builder.Services.AddScoped<IActivityScheduleAssignmentRepository, ActivityScheduleAssignmentRepository>();
            builder.Services.AddScoped<IJoinRequestRepository, JoinRequestRepository>();
            builder.Services.AddScoped<IInterviewRepository, InterviewRepository>();
            builder.Services.AddScoped<IClubMovementRecordRepository, ClubMovementRecordRepository>();
            builder.Services.AddScoped<IClubMovementRecordDetailRepository, ClubMovementRecordDetailRepository>();
            builder.Services.AddScoped<IProposalRepository, ProposalRepository>();
            builder.Services.AddScoped<IProposalVoteRepository, ProposalVoteRepository>();
            builder.Services.AddScoped<IFundCollectionRequestRepository, FundCollectionRequestRepository>();
            builder.Services.AddScoped<IFundCollectionPaymentRepository, FundCollectionPaymentRepository>();
            builder.Services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            builder.Services.AddScoped<IActivityMemberEvaluationRepository, ActivityMemberEvaluationRepository>();
            builder.Services.AddScoped<Repositories.ActivityEvaluations.IActivityEvaluationRepository, Repositories.ActivityEvaluations.ActivityEvaluationRepository>();
            builder.Services.AddScoped<ICommunicationPlanRepository, CommunicationPlanRepository>();
            builder.Services.AddScoped<IMonthlyReportRepository, MonthlyReportRepository>();

            // SignalR
            builder.Services.AddSignalR();

            // Services
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
            builder.Services.AddScoped<ISemesterService, SemesterService>();
            builder.Services.AddScoped<IMovementCriterionGroupService, MovementCriterionGroupService>();
            builder.Services.AddScoped<IMovementCriterionService, MovementCriterionService>();
            builder.Services.AddScoped<IUserImportService, UserImportService>();
            builder.Services.AddScoped<IEvidenceService, EvidenceService>();
            builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
            builder.Services.AddScoped<IMovementRecordService, MovementRecordService>();
            builder.Services.AddScoped<IMovementScoreCalculationService, MovementScoreCalculationService>();
            builder.Services.AddScoped<IClubMemberScoringService, ClubMemberScoringService>();
            builder.Services.AddScoped<Services.Students.IStudentService, Services.Students.StudentService>();
            builder.Services.AddScoped<Services.Users.IUserManagementService, Services.Users.UserManagementService>();
            builder.Services.AddScoped<IClubService, ClubService>();
            builder.Services.AddScoped<IActivityService, ActivityService>();
            builder.Services.AddScoped<IActivityExtractorService, ActivityExtractorService>();
            builder.Services.AddScoped<IJoinRequestService, JoinRequestService>();
            builder.Services.AddScoped<IInterviewService, InterviewService>();
            builder.Services.AddScoped<Services.Users.IUserProfileService, Services.Users.UserProfileService>();
            builder.Services.AddScoped<IClubScoringService, ClubScoringService>();
            builder.Services.AddScoped<IClubMovementRecordService, ClubMovementRecordService>();
            builder.Services.AddScoped<IProposalService, ProposalService>();
            builder.Services.AddScoped<IFundCollectionService, FundCollectionService>();
            builder.Services.AddScoped<IFinancialDashboardService, FinancialDashboardService>();
            builder.Services.AddScoped<Repositories.News.INewsRepository, Repositories.News.NewsRepository>();
            builder.Services.AddScoped<Services.News.INewsService, Services.News.NewsService>();
            builder.Services.AddScoped<Repositories.ClubNews.IClubNewsRepository, Repositories.ClubNews.ClubNewsRepository>();
            builder.Services.AddScoped<Services.ClubNews.IClubNewsService, Services.ClubNews.ClubNewsService>();
            builder.Services.AddScoped<Services.Notifications.INotificationService, Services.Notifications.NotificationService>();
            builder.Services.AddScoped<IActivityMemberEvaluationService, ActivityMemberEvaluationService>();
            builder.Services.AddScoped<ICommunicationPlanService, CommunicationPlanService>();
            builder.Services.AddScoped<Services.MonthlyReports.IMonthlyReportService, Services.MonthlyReports.MonthlyReportService>();
            builder.Services.AddScoped<Services.MonthlyReports.IMonthlyReportApprovalService, Services.MonthlyReports.MonthlyReportApprovalService>();
            builder.Services.AddScoped<Services.MonthlyReports.IMonthlyReportDataAggregator, Services.MonthlyReports.MonthlyReportDataAggregator>();
            builder.Services.AddScoped<Services.MonthlyReports.IMonthlyReportPdfService, Services.MonthlyReports.MonthlyReportPdfService>();
            builder.Services.AddScoped<Services.Emails.IEmailService, Services.Emails.EmailService>();
            builder.Services.AddScoped<Services.Chatbot.IGeminiAIService, Services.Chatbot.GeminiAIService>();
            builder.Services.AddScoped<Services.Chatbot.IChatbotService, Services.Chatbot.ChatbotService>();

            // Background Services
            builder.Services.AddHostedService<SemesterAutoUpdateService>();
            builder.Services.AddHostedService<TokenCleanupService>();
            builder.Services.AddHostedService<ComprehensiveAutoScoringService>();
            builder.Services.AddHostedService<WebAPI.BackgroundServices.NotificationBroadcastService>();
            builder.Services.AddHostedService<WebAPI.BackgroundServices.ActivityAutoCompleteService>();
            builder.Services.AddHostedService<WebAPI.BackgroundServices.EvaluationReminderService>();
            builder.Services.AddHostedService<WebAPI.BackgroundServices.MonthlyReportGenerationService>();
            builder.Services.AddHostedService<Services.FundCollections.PaymentReminderBackgroundService>();
            builder.Services.AddHostedService<Services.FundCollections.FundCollectionAutoCompleteService>();
            // DEPRECATED: MovementScoreAutomationService - functionality merged into ComprehensiveAutoScoringService
            // builder.Services.AddHostedService<MovementScoreAutomationService>();

            // Custom JWT Authentication (bypass library issues)
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            builder.Services.AddAuthentication("CustomJWT")
                .AddScheme<AuthenticationSchemeOptions, CustomJwtAuthenticationHandler>("CustomJWT", options => { });

            // Controllers & API
            builder.Services.AddControllers();
            builder.Services.AddHttpContextAccessor();
            
            // Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "EduXtend API", Version = "v1" });
                
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Add support for file upload operations
                options.OperationFilter<WebAPI.Swagger.FileUploadOperationFilter>();
            });

            // CORS - FIX LOGIN ISSUE
            builder.Services.AddCors(opt =>
            {
                opt.AddPolicy("react", p =>
                    p.WithOrigins(
                        "http://localhost:3000",    // WebFE HTTP
                        "https://localhost:3001",   // WebFE HTTPS
                        "http://localhost:5000",    // Backend HTTP
                        "https://localhost:5001",   // Backend HTTPS
                        "http://localhost:5173",    // React dev server
                        "https://localhost:44315"   // WebFE HTTPS (IIS Express)
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            // Always enable Swagger (dev and prod) and host at /swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "EduXtend API V1");
                c.RoutePrefix = "swagger"; // so UI is at /swagger
            });
            
            // CORS - Must be before Authentication
            app.UseCors("react");

            app.UseHttpsRedirection();
            
            // Enable serving static files (for uploaded CVs, images, etc.)
            app.UseStaticFiles();

            // Rate Limiting - Must be before Authentication
            app.UseIpRateLimiting();
            app.UseCustomRateLimitResponse();

            // Custom middleware (order matters!)
            app.UseAuthentication();     // Validate JWT and set User principal
            
            app.UseTokenBlacklist();    // Check if token is blacklisted
            
            app.UseAuthorization();      // Check policies and roles

            app.MapControllers();
            
            // Map SignalR Hub
            app.MapHub<WebAPI.Hubs.NotificationHub>("/notificationHub");

            app.Run();
        }
    }
}
