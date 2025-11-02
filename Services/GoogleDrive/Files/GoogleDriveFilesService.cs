using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;

namespace XMLParser.Services.GoogleDrive.Files;

public class GoogleDriveFilesService : IGoogleDriveFilesService
{
    DriveService? _driveService;
    GoogleCredential? _credential;

    public void UpdateCredential(GoogleCredential? credential = null)
    {
        _credential = credential;

        if (_credential is null)
        {
            _driveService = null;
        }
        else
        {
            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credential,
                ApplicationName = "XMLParserUniversityProject"
            });
        }
    }

    public async Task<List<Google.Apis.Drive.v3.Data.File>> GetFiles(string[] fileExtensions)
    {
        if (_driveService is null)
            throw new InvalidOperationException("Google Drive not initialized or not signed in");

        var request = _driveService!.Files.List();
        var fileList = await request.ExecuteAsync();
        var files = new List<Google.Apis.Drive.v3.Data.File>();

        if (fileList.Files != null && fileList.Files.Count > 0)
        {
            foreach (var file in fileList.Files)
            {
                foreach (var fileExtension in fileExtensions)
                {
                    if (!(file.FileExtension == fileExtension || file.Name.EndsWith(fileExtension)))
                    {
                        continue;
                    }
                    files.Add(file);
                }
            }
        }

        return files;
    }

    public async Task<string> DownloadFileAsync(string fileId)
    {
        if (_driveService is null)
            throw new InvalidOperationException("Google Drive not initialized or not signed in");

        using var memoryStream = new MemoryStream();

        var request = _driveService.Files.Get(fileId);
        await request.DownloadAsync(memoryStream);

        memoryStream.Position = 0;

        using var reader = new StreamReader(memoryStream);
        return await reader.ReadToEndAsync();
    }

    public async Task<Google.Apis.Drive.v3.Data.File> UploadFileAsync(string fileName, Stream content, string mimeType)
    {
        if (_driveService is null)
            throw new InvalidOperationException("Google Drive not initialized or not signed in");

        var fileMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = fileName
        };

        var request = _driveService.Files.Create(fileMetadata, content, mimeType);
        request.Fields = "id, name, mimeType, size, createdTime, modifiedTime";
        await request.UploadAsync();

        var uploadedFile = request.ResponseBody;

        if (uploadedFile is null)
            throw new HttpRequestException("File upload failed: response was null");

        return uploadedFile;
    }
    
    public async Task<Google.Apis.Drive.v3.Data.File> UploadOrReplaceFileAsync(string fileName, Stream content, string mimeType)
    {
        if (_driveService is null)
            throw new InvalidOperationException("Google Drive not initialized or not signed in");

        var listRequest = _driveService.Files.List();
        listRequest.Q = $"name='{fileName.Replace("'", "\\'")}' and trashed=false";
        listRequest.Fields = "files(id, name, mimeType, size, createdTime, modifiedTime)";
        var fileList = await listRequest.ExecuteAsync();

        Google.Apis.Drive.v3.Data.File? existingFile = fileList.Files?.FirstOrDefault();

        if (existingFile != null)
        {
            var updateRequest = _driveService.Files.Update(new Google.Apis.Drive.v3.Data.File(), existingFile.Id, content, mimeType);
            updateRequest.Fields = "id, name, mimeType, size, createdTime, modifiedTime";
            await updateRequest.UploadAsync();
            var uploadedFile = updateRequest.ResponseBody;

            if (uploadedFile is null)
                throw new HttpRequestException("File upload failed: response was null");

            return uploadedFile;
        }

        return await UploadFileAsync(fileName, content, mimeType);
    }
}