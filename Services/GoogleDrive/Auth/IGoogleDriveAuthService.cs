using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;

namespace XMLParser.Services.GoogleDrive.Auth;

public interface IGoogleDriveAuthService
{
    public bool IsSignedIn { get; }
    public string? Email { get; }

    public Task Init();
    public Task<GoogleCredential?> SignIn();
    public Task SignOut();
}