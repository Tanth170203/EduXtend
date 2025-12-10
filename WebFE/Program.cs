using WebFE.Middleware;
using Microsoft.EntityFrameworkCore;
using DataAccess;
using Repositories.ClubMovementRecords;
using Services.ClubMovementRecords;
using System.Text.Json;

namespace WebFE
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddHttpContextAccessor();
            
            // Add Session support for TempData
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            
            // Add Authentication and Authorization services with Cookie Authentication
            builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.AccessDeniedPath = "/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromHours(24);
                    options.SlidingExpiration = true;
                    
                    // Handle 401/403 responses properly
                    options.Events.OnRedirectToAccessDenied = context =>
                    {
                        // For AJAX requests, return status code instead of redirect
                        if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return Task.CompletedTask;
                        }
                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    };
                    
                    options.Events.OnRedirectToLogin = context =>
                    {
                        // For AJAX requests, return status code instead of redirect
                        if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        }
                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    };
                });
            
            // Add Authorization with policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("ClubManagerOrAdmin", policy => policy.RequireRole("Admin", "ClubManager"));
                options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
            });
            
            builder.Services.AddRazorPages()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });
            
            // DbContext for server-side Razor Pages that query directly
            builder.Services.AddDbContext<EduXtendContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            
            // Club scoring DI (used by Admin/ClubScoring pages)
            builder.Services.AddScoped<IClubMovementRecordRepository, ClubMovementRecordRepository>();
            builder.Services.AddScoped<IClubMovementRecordDetailRepository, ClubMovementRecordDetailRepository>();
            builder.Services.AddScoped<IClubScoringService, ClubScoringService>();

            // Note: Pages are protected by JwtAuthenticationMiddleware
            // API endpoints are protected by JWT [Authorize] attributes in WebAPI


            //deploy ( chạy nhớ comment lần nây )
            builder.WebHost.UseUrls($"http://*:80");
            // Register HttpClient with cookie forwarding
            builder.Services.AddHttpClient("ApiClient", client =>
            {
                client.BaseAddress = new Uri("https://localhost:5001"); // backend API base
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler
                {
                    UseCookies = true,
                    CookieContainer = new System.Net.CookieContainer(),
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true // ch? n�n d�ng dev mode
                };
                return handler;
            });


            // Add Antiforgery for AJAX requests
            builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "RequestVerificationToken";
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseSession();

            // JWT Authentication & Authorization Middleware - MUST be before UseAuthorization
            // This middleware validates JWT from cookie and checks role-based access
            app.UseMiddleware<JwtAuthenticationMiddleware>();
            
            app.UseAuthentication(); // Required for Cookie Authentication
            app.UseAuthorization(); // Required for [Authorize] attributes

            // API Proxy: Forward /api/* requests to WebAPI server
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    var apiBaseUrl = app.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001";
                    var targetUrl = $"{apiBaseUrl}{context.Request.Path}{context.Request.QueryString}";
                    
                    using var httpClient = new HttpClient(new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    });
                    
                    // Forward cookies
                    if (context.Request.Headers.ContainsKey("Cookie"))
                    {
                        httpClient.DefaultRequestHeaders.Add("Cookie", context.Request.Headers["Cookie"].ToString());
                    }
                    
                    var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUrl);
                    
                    // Copy request body for POST/PUT/PATCH
                    if (context.Request.Method == "POST" || context.Request.Method == "PUT" || context.Request.Method == "PATCH")
                    {
                        var ms = new MemoryStream();
                        await context.Request.Body.CopyToAsync(ms);
                        ms.Position = 0;
                        requestMessage.Content = new StreamContent(ms);
                        if (context.Request.ContentType != null)
                        {
                            requestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(context.Request.ContentType);
                        }
                    }
                    
                    var response = await httpClient.SendAsync(requestMessage);
                    
                    // Copy response
                    context.Response.StatusCode = (int)response.StatusCode;
                    foreach (var header in response.Headers)
                    {
                        if (!header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.Headers[header.Key] = header.Value.ToArray();
                        }
                    }
                    foreach (var header in response.Content.Headers)
                    {
                        if (!header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.Headers[header.Key] = header.Value.ToArray();
                        }
                    }
                    
                    await response.Content.CopyToAsync(context.Response.Body);
                }
                else
                {
                    await next();
                }
            });

            app.MapRazorPages();

            app.Run();
        }
    }
}
