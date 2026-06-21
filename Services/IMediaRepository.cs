using FocusFlowFinal.Models.Media;
using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public class MediaFilter
{
    public MediaType?   Type     { get; set; }
    public MediaStatus? Status   { get; set; }
    public string?      Query    { get; set; }
}

public interface IMediaRepository
{
    IEnumerable<MediaItem> GetAll();
    IEnumerable<MediaItem> GetFiltered(MediaFilter filter);
    MediaItem?             GetById(int id);
    int                    Upsert(MediaItem item);
    void                   Delete(int id);
    MediaStatistics        GetStatistics();
    IEnumerable<string>    GetAllGenres();
}
