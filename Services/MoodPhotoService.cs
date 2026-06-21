using System;
using System.Collections.Generic;
using System.IO;

namespace FocusFlowFinal.Services;

public class MoodPhotoService : IMoodPhotoService
{
    private readonly string _dir;

    public MoodPhotoService()
    {
        _dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FocusFlow", "MoodPhotos");
        Directory.CreateDirectory(_dir);
    }

    public string CopyPhoto(string sourcePath)
    {
        var ext  = Path.GetExtension(sourcePath);
        var name = $"{Guid.NewGuid():N}{ext}";
        File.Copy(sourcePath, Path.Combine(_dir, name), overwrite: false);
        return name;
    }

    public string GetPhotoPath(string fileName) => Path.Combine(_dir, fileName);

    public void DeletePhotos(IEnumerable<string> fileNames)
    {
        foreach (var name in fileNames)
        {
            var path = GetPhotoPath(name);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
