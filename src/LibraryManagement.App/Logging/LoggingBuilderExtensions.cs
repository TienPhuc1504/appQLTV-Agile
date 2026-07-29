using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.App.Logging;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddDailyFile(
        this ILoggingBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        string logDirectory = configuration["Logging:FileDirectory"]
            ?? throw new InvalidOperationException(
                "Không tìm thấy cấu hình 'Logging:FileDirectory'.");
        builder.Services.AddSingleton<ILoggerProvider>(
            new DailyFileLoggerProvider(logDirectory));
        return builder;
    }
}
