using PracticeMonitoring.Mobile.Models;

namespace PracticeMonitoring.Mobile.Services;

public sealed class FileStorageService
{
    public async Task<string> SaveToCacheAsync(FileDownload file)
    {
        var safeName = string.Concat(file.FileName.Select(ch =>
            Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        var path = Path.Combine(FileSystem.CacheDirectory, string.IsNullOrWhiteSpace(safeName) ? "document.bin" : safeName);
        await File.WriteAllBytesAsync(path, file.Content);
        return path;
    }

    public async Task OpenAsync(FileDownload file)
    {
        var path = await SaveToCacheAsync(file);
        try
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest(file.FileName, new ReadOnlyFile(path)));
        }
        catch
        {
            await ShareAsync(file);
        }
    }

    public async Task ShareAsync(FileDownload file)
    {
        var path = await SaveToCacheAsync(file);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = file.FileName,
            File = new ShareFile(path)
        });
    }
}
