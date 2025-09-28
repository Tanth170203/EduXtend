using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace DataAccess
{
    public class EduXtendContextFactory : IDesignTimeDbContextFactory<EduXtendContext>
    {
        public EduXtendContext CreateDbContext(string[] args)
        {
            // Build configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            // Setup DbContextOptions
            var optionsBuilder = new DbContextOptionsBuilder<EduXtendContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseSqlServer(connectionString,
                sqlOptions => sqlOptions.MigrationsAssembly("DataAccess")); // Thay "DataAccess" bằng tên project chứa Migrations

            return new EduXtendContext(optionsBuilder.Options);
        }
    }
}