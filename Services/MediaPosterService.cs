using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

public class MediaPosterService : IMediaPosterService
{
    private readonly string _dir;
    private static readonly HttpClient _http = new();

    public MediaPosterService()
    {
        _dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FocusFlow", "Posters");
        Directory.CreateDirectory(_dir);
    }

    public async Task<string?> CopyPosterAsync(string sourcePath)
    {
        if (!File.Exists(sourcePath)) return null;
        var ext  = Path.GetExtension(sourcePath);
        var name = $"{Guid.NewGuid():N}{ext}";
        var dest = Path.Combine(_dir, name);
        await Task.Run(() => File.Copy(sourcePath, dest, overwrite: true));
        return name;
    }

    public async Task<string?> DownloadPosterAsync(string url)
    {
        try
        {
            var ext  = Path.GetExtension(new Uri(url).LocalPath);
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";
            var name = $"{Guid.NewGuid():N}{ext}";
            var dest = Path.Combine(_dir, name);
            var bytes = await _http.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(dest, bytes);
            return name;
        }
        catch { return null; }
    }

    public string GetPosterPath(string fileName) =>
        Path.Combine(_dir, fileName);

    public void DeletePoster(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return;
        var path = GetPosterPath(fileName);
        if (File.Exists(path)) File.Delete(path);
    }
}
