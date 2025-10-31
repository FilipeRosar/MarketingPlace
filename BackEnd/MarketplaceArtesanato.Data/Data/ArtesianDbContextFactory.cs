using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using System.IO;



namespace MarketplaceArtesanato.Data.Data
{
    public class ArtesianDbContextFactory : IDesignTimeDbContextFactory<ArtesianDbContext> 
    {
        public ArtesianDbContext CreateDbContext(string[] args)
        {
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "MarketplaceArtesanato.API");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
              ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada.");

            var optionsBuilder = new DbContextOptionsBuilder<ArtesianDbContext>();
            optionsBuilder.UseSqlServer(connectionString);
            return new ArtesianDbContext(optionsBuilder.Options);
        }
    }
}
