using BusinessObject.DTOs.GGLogin;
using BusinessObject.Models;
using DataAccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Repositories.Users;
using Services.GGLogin;
using System.Text;


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

            // Password hasher (PBKDF2)
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IAuthService, GoogleAuthService>();



            // JWT
            var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(opt =>
                {
                    opt.RequireHttpsMetadata = false;
                    opt.SaveToken = true;
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwt.Issuer,
                        ValidAudience = jwt.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            builder.Services.AddAuthorization();
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddCors(opt =>
            {
                opt.AddPolicy("react", p =>
                    p.WithOrigins("http://localhost:5173", "http://localhost:3000")
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

            app.UseAuthentication();


            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
