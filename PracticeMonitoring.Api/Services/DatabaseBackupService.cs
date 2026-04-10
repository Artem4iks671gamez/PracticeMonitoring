using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace PracticeMonitoring.Api.Services;

public class DatabaseBackupService
{
    private readonly IConfiguration _configuration;

    public DatabaseBackupService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<(byte[] Content, string FileName)> CreateBackupAsync()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        var pgDumpPath = _configuration["PgTools:PgDumpPath"];
        if (string.IsNullOrWhiteSpace(pgDumpPath))
            pgDumpPath = "pg_dump";

        var tempFile = Path.Combine(Path.GetTempPath(), $"practice-monitoring-backup-{DateTime.Now:yyyyMMdd-HHmmss}.dump");

        try
        {
            var args =
                $"--format=c --no-owner --no-privileges " +
                $"--host \"{builder.Host}\" " +
                $"--port \"{builder.Port}\" " +
                $"--username \"{builder.Username}\" " +
                $"--file \"{tempFile}\" " +
                $"\"{builder.Database}\"";

            var result = await RunProcessAsync(pgDumpPath, args, builder.Password);

            if (result.ExitCode != 0)
                throw new InvalidOperationException($"pg_dump failed: {result.Error}");

            var bytes = await File.ReadAllBytesAsync(tempFile);
            var fileName = $"practice-monitoring-backup-{DateTime.Now:yyyyMMdd-HHmmss}.dump";

            return (bytes, fileName);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    public async Task RestoreBackupAsync(string uploadedDumpPath)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        var pgRestorePath = _configuration["PgTools:PgRestorePath"];
        if (string.IsNullOrWhiteSpace(pgRestorePath))
            pgRestorePath = "pg_restore";

        var args =
            $"--clean --if-exists --no-owner --no-privileges " +
            $"--host \"{builder.Host}\" " +
            $"--port \"{builder.Port}\" " +
            $"--username \"{builder.Username}\" " +
            $"--dbname \"{builder.Database}\" " +
            $"\"{uploadedDumpPath}\"";

        var result = await RunProcessAsync(pgRestorePath, args, builder.Password);

        if (result.ExitCode != 0)
            throw new InvalidOperationException($"pg_restore failed: {result.Error}");
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunProcessAsync(string fileName, string arguments, string? password)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrWhiteSpace(password))
            startInfo.Environment["PGPASSWORD"] = password;

        using var process = new Process { StartInfo = startInfo };

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        return (process.ExitCode, output, error);
    }
}