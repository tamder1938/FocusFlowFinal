using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public interface IMoodPhotoService
{
    /// <summary>Копирует файл в хранилище, возвращает имя файла (без пути).</summary>
    string CopyPhoto(string sourcePath);

    /// <summary>Полный путь к файлу фото по имени.</summary>
    string GetPhotoPath(string fileName);

    /// <summary>Удаляет файлы из хранилища.</summary>
    void DeletePhotos(IEnumerable<string> fileNames);
}
