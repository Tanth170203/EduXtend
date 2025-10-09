using BusinessObject.DTOs.GGLogin;
using BusinessObject.Models;
using DataAccess;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Repositories.LoggedOutTokens;
using Repositories.Semesters;
using Repositories.Users;
using Services.GGLogin;
using Services.Semesters;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using WebAPI.Authentication;
using WebAPI.Middleware;


namespace WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // DbContext
            builder.Services.AddDbContext<EduXtendContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            // Add services to the container.


            // Options
            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
            builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection("GoogleAuth"));

            // Repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ISemesterRepository, SemesterRepository>();
            builder.Services.AddScoped<ILoggedOutTokenRepository, LoggedOutTokenRepository>();
            builder.Services.AddScoped<Repositories.Students.IStudentRepository, Repositories.Students.StudentRepository>();
            builder.Services.AddScoped<Repositories.Majors.IMajorRepository, Repositories.Majors.MajorRepository>();
            builder.Services.AddScoped<Repositories.Roles.IRoleRepository, Repositories.Roles.RoleRepository>();
            
            // Services
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
            builder.Services.AddScoped<ISemesterService, SemesterService>();
            builder.Services.AddScoped<Services.Students.IStudentService, Services.Students.StudentService>();
            builder.Services.AddScoped<Services.Users.IUserService, Services.Users.UserService>();

            // Background Services
            builder.Services.AddHostedService<SemesterAutoUpdateService>();



            // Custom JWT Authentication (bypass library issues)
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            builder.Services.AddAuthentication("CustomJWT")
                .AddScheme<AuthenticationSchemeOptions, CustomJwtAuthenticationHandler>("CustomJWT", options => { });


            builder.Services.AddAuthorization(options =>
            {
                // Role-based policies
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
                options.AddPolicy("ClubManagerOnly", policy => policy.RequireRole("ClubManager"));
                options.AddPolicy("ClubMemberOnly", policy => policy.RequireRole("ClubMember"));
                
                // Combined policies
                options.AddPolicy("ClubManagement", policy => 
                    policy.RequireRole("Admin", "ClubManager"));
                options.AddPolicy("ClubAccess", policy => 
                    policy.RequireRole("Admin", "ClubManager", "ClubMember"));
                options.AddPolicy("AllUsers", policy => 
                    policy.RequireRole("Admin", "Student", "ClubManager", "ClubMember"));
            });
            
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            builder.Services.AddCors(opt =>
            {
                opt.AddPolicy("react", p =>
                    p.WithOrigins(
                        "http://localhost:3000",    // WebFE HTTP
                        "https://localhost:3001",  // WebFE HTTPS
                        "http://localhost:5000",   // Backend HTTP
                        "https://localhost:5001",  // Backend HTTPS
                        "http://localhost:5173"    // React dev server
                    )
                     .AllowAnyHeader().AllowAnyMethod().AllowCredentials());
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseCors("react");

            app.UseHttpsRedirection();

            // Custom middleware (order matters!)
            // app.UseAutoRefreshToken();  // DISABLED: Let frontend handle token refresh
            
            app.UseAuthentication();     // Validate JWT and set User principal
            
            app.UseTokenBlacklist();    // Check if token is blacklisted
            
            app.UseAuthorization();      // Check policies and roles


            app.MapControllers();

            app.Run();
        }
    }
}
