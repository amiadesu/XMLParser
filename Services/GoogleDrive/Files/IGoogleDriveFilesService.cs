using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;

namespace XMLParser.Services.GoogleDrive.Files;

public interface IGoogleDriveFilesService
{
    public void UpdateCredential(GoogleCredential? credential = null);
    public Task<List<Google.Apis.Drive.v3.Data.File>> GetFiles(string[] fileExtensions);
    public Task<string> DownloadFileAsync(string fileId);
    public Task<Google.Apis.Drive.v3.Data.File> UploadFileAsync(string fileName, Stream content, string mimeType);
    public Task<Google.Apis.Drive.v3.Data.File> UploadOrReplaceFileAsync(string fileName, Stream content, string mimeType);
}