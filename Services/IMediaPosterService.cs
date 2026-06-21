using System.Threading.Tasks;

namespace FocusFlowFinal.Services;

public interface IMediaPosterService
{
    Task<string?> CopyPosterAsync(string sourcePath);
    Task<string?> DownloadPosterAsync(string url);
    string        GetPosterPath(string fileName);
    void          DeletePoster(string? fileName);
}
