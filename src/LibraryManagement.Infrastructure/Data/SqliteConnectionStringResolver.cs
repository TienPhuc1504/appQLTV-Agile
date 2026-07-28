using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace LibraryManagement.Infrastructure.Data;

internal static class SqliteConnectionStringResolver
{
    private const string ConnectionStringName = "LibraryDatabase";

    public static string Resolve(IConfiguration configuration, string baseDirectory)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        string connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Không tìm thấy connection string '{ConnectionStringName}'.");

        var builder = new SqliteConnectionStringBuilder(connectionString);
        string expandedDataSource = Environment.ExpandEnvironmentVariables(builder.DataSource);

        if (!Path.IsPathRooted(expandedDataSource))
        {
            expandedDataSource = Path.GetFullPath(
                Path.Combine(baseDirectory, expandedDataSource));
        }

        builder.DataSource = expandedDataSource;
        builder.ForeignKeys = true;

        return builder.ToString();
    }
}
