using Microsoft.Maui.Devices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using XMLParser.Services.GoogleDrive.Auth;
using XMLParser.Services.GoogleDrive.Files;

namespace XMLParser.Services.GoogleDrive;

public class GoogleDriveService : IGoogleDriveService
{
    private readonly IGoogleDriveAuthService? _googleDriveAuthService;
    private readonly IGoogleDriveFilesService _googleDriveFilesService;

    public bool IsSignedIn => _googleDriveAuthService?.IsSignedIn ?? false;
    public string? Email =>  _googleDriveAuthService?.Email;

    public GoogleDriveService()
    {
        if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
        {
            _googleDriveAuthService = new WindowsGoogleDriveAuthService();
        }
        else
        {
            _googleDriveAuthService = null;
        }
        _googleDriveFilesService = new GoogleDriveFilesService();
    }

    public async Task Init()
    {
        if (_googleDriveAuthService is null)
            throw new NotImplementedException($"Auth flow for platform {DeviceInfo.Current.Platform} not implemented");

        await _googleDriveAuthService.Init();
        if (IsSignedIn)
        {
            var credential = await _googleDriveAuthService.SignIn();
            _googleDriveFilesService.UpdateCredential(credential);
        }
    }

    public async Task SignIn()
    {
        if (_googleDriveAuthService is null)
            throw new NotImplementedException($"Auth flow for platform {DeviceInfo.Current.Platform} not implemented");

        var credential = await _googleDriveAuthService.SignIn();
        _googleDriveFilesService.UpdateCredential(credential);
    }

    public async Task<List<Google.Apis.Drive.v3.Data.File>> GetFiles(string[] fileExtensions)
    {
        return await _googleDriveFilesService.GetFiles(fileExtensions);
    }

    public async Task<string> DownloadFileAsync(string fileId)
    {
        return await _googleDriveFilesService.DownloadFileAsync(fileId);
    }

    public async Task<Google.Apis.Drive.v3.Data.File> UploadFileAsync(string fileName, Stream content, string mimeType)
    {
        return await _googleDriveFilesService.UploadFileAsync(fileName, content, mimeType);
    }
    
    public async Task<Google.Apis.Drive.v3.Data.File> UploadOrReplaceFileAsync(string fileName, Stream content, string mimeType)
    {
        return await _googleDriveFilesService.UploadOrReplaceFileAsync(fileName, content, mimeType);
    }

    public async Task SignOut()
    {
        if (_googleDriveAuthService is null)
            throw new NotImplementedException($"Auth flow for platform {DeviceInfo.Current.Platform} not implemented");

        await _googleDriveAuthService.SignOut();
        
        _googleDriveFilesService.UpdateCredential(null);
    }
}