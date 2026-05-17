using System.ComponentModel;
using System.Diagnostics;

namespace PracticeMonitoring.Web.Services;

public class DocxPdfConversionService
{
    private static readonly TimeSpan ConversionTimeout = TimeSpan.FromSeconds(90);
    private readonly IConfiguration _configuration;

    public DocxPdfConversionService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<byte[]> ConvertDocxToPdfAsync(byte[] docxContent, string sourceFileName, CancellationToken cancellationToken = default)
    {
        if (docxContent.Length == 0)
            throw new InvalidOperationException("DOCX документ пустой.");

        var tempRoot = Path.Combine(Path.GetTempPath(), $"practice-monitoring-docx-pdf-{Guid.NewGuid():N}");
        var outputDir = Path.Combine(tempRoot, "out");
        var profileDir = Path.Combine(tempRoot, "profile");
        var inputFileName = Path.ChangeExtension(SafeFileName(sourceFileName), ".docx");
        var inputPath = Path.Combine(tempRoot, inputFileName);
        var outputPath = Path.Combine(outputDir, Path.ChangeExtension(inputFileName, ".pdf"));

        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(profileDir);

        try
        {
            await File.WriteAllBytesAsync(inputPath, docxContent, cancellationToken);
            var result = await RunLibreOfficeAsync(inputPath, outputDir, profileDir, cancellationToken);

            if (result.ExitCode != 0)
            {
                var error = FirstNotEmpty(result.Error, result.Output, "LibreOffice завершил конвертацию с ошибкой.");
                throw new InvalidOperationException($"Не удалось конвертировать DOCX в PDF: {error}");
            }

            if (!File.Exists(outputPath))
            {
                var actualPdf = Directory.GetFiles(outputDir, "*.pdf").FirstOrDefault();
                if (actualPdf is null)
                    throw new InvalidOperationException("LibreOffice не создал PDF файл.");

                outputPath = actualPdf;
            }

            return await File.ReadAllBytesAsync(outputPath, cancellationToken);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                try
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }

    private async Task<(int ExitCode, string Output, string Error)> RunLibreOfficeAsync(
        string inputPath,
        string outputDir,
        string profileDir,
        CancellationToken cancellationToken)
    {
        var attempted = new List<string>();
        var arguments =
            "--headless --nologo --nolockcheck --nodefault --nofirststartwizard " +
            $"-env:UserInstallation={Quote(ToFileUri(profileDir))} " +
            "--convert-to pdf " +
            $"--outdir {Quote(outputDir)} " +
            Quote(inputPath);

        foreach (var executable in GetExecutableCandidates())
        {
            attempted.Add(executable);

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = executable,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
                var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
                await Task.WhenAny(process.WaitForExitAsync(cancellationToken), Task.Delay(ConversionTimeout, cancellationToken));

                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    throw new InvalidOperationException($"LibreOffice не успел сконвертировать DOCX в PDF за {ConversionTimeout.TotalSeconds:0} секунд.");
                }

                return (process.ExitCode, await outputTask, await errorTask);
            }
            catch (Win32Exception)
            {
                continue;
            }
        }

        throw new InvalidOperationException($"LibreOffice/soffice не найден. Проверенные команды: {string.Join(", ", attempted)}.");
    }

    private IEnumerable<string> GetExecutableCandidates()
    {
        var configured = _configuration["OfficeConverter:SofficePath"];
        if (!string.IsNullOrWhiteSpace(configured))
            yield return configured.Trim();

        yield return "soffice";
        yield return "libreoffice";
        yield return "/usr/bin/soffice";
        yield return "/usr/bin/libreoffice";
        yield return @"C:\Program Files\LibreOffice\program\soffice.exe";
        yield return @"C:\Program Files (x86)\LibreOffice\program\soffice.exe";
    }

    private static string SafeFileName(string fileName)
    {
        var value = string.IsNullOrWhiteSpace(fileName) ? "document.docx" : Path.GetFileName(fileName);
        foreach (var invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return string.IsNullOrWhiteSpace(value) ? "document.docx" : value;
    }

    private static string ToFileUri(string path)
    {
        Directory.CreateDirectory(path);
        return new Uri(Path.GetFullPath(path) + Path.DirectorySeparatorChar).AbsoluteUri;
    }

    private static string Quote(string value)
    {
        return $"\"{value.Replace("\"", "\\\"")}\"";
    }

    private static string FirstNotEmpty(params string[] values)
    {
        return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? string.Empty;
    }
}
