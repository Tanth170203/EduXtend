using WebFE.Middleware;

namespace WebFE
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddHttpContextAccessor();
            
            builder.Services.AddRazorPages();
            
            // Note: Pages are protected by JwtAuthenticationMiddleware
            // API endpoints are protected by JWT [Authorize] attributes in WebAPI

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

            // JWT Authentication & Authorization Middleware
            app.UseJwtAuthentication();

            app.MapRazorPages();

            app.Run();
        }
    }
}
