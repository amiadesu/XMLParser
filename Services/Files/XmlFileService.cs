using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using CommunityToolkit.Maui.Storage;
using XMLParser.Resources.Localization;
using XMLParser.Services;
using XMLParser.Utils;
using XMLParser.Services.GoogleDrive;
using XMLParser.Constants;

namespace XMLParser.FileSystem;

public class XmlFileService
{
    public async Task<string> SaveLocally(string fileData, string fileName = Literals.defaultXmlFileName)
    {
        try
        {
            var fileSaverResult = await PickAndSaveFile(fileData, fileName);
            if (fileSaverResult.IsSuccessful)
            {
                return DataProcessor.FormatResource(
                    AppResources.FileSavedSuccessfully,
                    ("Path", fileSaverResult.FilePath)
                );
            }
            return DataProcessor.FormatResource(
                AppResources.FileSavingError,
                ("Error", fileSaverResult.Exception?.Message ?? "Unknown error")
            );
        }
        catch (Exception ex)
        {
            return $"Error saving file: {ex.Message}";
        }
    }

    public async Task<string> SaveToGoogleDrive(string fileData,
        IGoogleDriveService googleDriveService, string fileName = Literals.defaultXmlFileName)
    {
        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileData));
            var file = await googleDriveService.UploadOrReplaceFileAsync(fileName, stream, "text/plain");
            return DataProcessor.FormatResource(
                AppResources.FileSavedSuccessfully,
                ("Path", $"Google Drive, {file.Name} (id: {file.Id})")
            );
        }
        catch (Exception e)
        {
            return DataProcessor.FormatResource(
                AppResources.FileSavingError,
                ("Error", e.Message)
            );
        }
    }

    public string LoadFromPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException("File not found", path);

        using var reader = new StreamReader(path);
        return reader.ReadToEnd();
    }

    public string LoadFromContentString(string content)
    {
        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(content)));
        return reader.ReadToEnd();
    }

    public async Task<string> LoadFromGoogleDrive(string fileId, IGoogleDriveService googleDriveService)
    {
        var content = await googleDriveService.DownloadFileAsync(fileId);
        return content ?? "";
    }

    public static async Task<(FileResult? result, string? errorMessage)> PickFile(string pickTitle, string extensionDescription)
    {
        var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.WinUI, new[] { extensionDescription } }
        });

        PickOptions options = new()
        {
            PickerTitle = pickTitle,
            FileTypes = customFileType,
        };

        try
        {
            var result = await FilePicker.Default.PickAsync(options);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public static async Task<FileSaverResult> PickAndSaveFile(string data, string fileName = Literals.defaultXmlFileName)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
        return await FileSaver.Default.SaveAsync(fileName, stream);
    }
}
