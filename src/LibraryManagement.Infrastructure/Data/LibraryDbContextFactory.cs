using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LibraryManagement.Infrastructure.Data;

public sealed class LibraryDbContextFactory : IDesignTimeDbContextFactory<LibraryDbContext>
{
    public LibraryDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = BuildConfiguration();
        string connectionString = SqliteConnectionStringResolver.Resolve(
            configuration,
            AppContext.BaseDirectory);

        SQLitePCL.Batteries_V2.Init();

        var optionsBuilder = new DbContextOptionsBuilder<LibraryDbContext>();
        optionsBuilder
            .UseSqlite(connectionString)
            .EnableDetailedErrors();

        return new LibraryDbContext(optionsBuilder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        string[] candidates =
        [
            Path.Combine(currentDirectory, "appsettings.json"),
            Path.Combine(currentDirectory, "src", "LibraryManagement.App", "appsettings.json"),
            Path.GetFullPath(
                Path.Combine(currentDirectory, "..", "LibraryManagement.App", "appsettings.json"))
        ];

        string configurationPath = candidates.FirstOrDefault(File.Exists)
            ?? throw new InvalidOperationException(
                "Không tìm thấy appsettings.json để tạo LibraryDbContext tại design time.");

        return new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(configurationPath)!)
            .AddJsonFile(Path.GetFileName(configurationPath), optional: false)
            .Build();
    }
}
