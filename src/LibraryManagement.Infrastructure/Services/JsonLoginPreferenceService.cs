using System.Text.Json;
using LibraryManagement.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Services;

public sealed class JsonLoginPreferenceService(
    string filePath,
    ILogger<JsonLoginPreferenceService> logger) : ILoginPreferenceService
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly string _filePath = ValidateFilePath(filePath);

    public async Task<string?> GetRememberedUsernameAsync(
        CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            await using FileStream stream = new(
                _filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);
            LoginPreference? preference =
                await JsonSerializer.DeserializeAsync<LoginPreference>(
                    stream,
                    SerializerOptions,
                    cancellationToken);

            return string.IsNullOrWhiteSpace(preference?.Username)
                ? null
                : preference.Username.Trim();
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException
                or JsonException)
        {
            logger.LogWarning(
                exception,
                "Không thể đọc tùy chọn ghi nhớ tên đăng nhập.");
            return null;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveRememberedUsernameAsync(
        string? username,
        CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);

        try
        {
            string? directoryPath = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }

                return;
            }

            await using FileStream stream = new(
                _filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true);
            await JsonSerializer.SerializeAsync(
                stream,
                new LoginPreference(username.Trim()),
                SerializerOptions,
                cancellationToken);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(
                exception,
                "Không thể lưu tùy chọn ghi nhớ tên đăng nhập.");
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private static string ValidateFilePath(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return Path.GetFullPath(
            Environment.ExpandEnvironmentVariables(filePath));
    }

    private sealed record LoginPreference(string Username);
}
