using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Net.Http;
using XMLParser.Constants;
using XMLParser.Components.TokenStorage;

namespace XMLParser.Services.GoogleDrive.Auth;

public class WindowsGoogleDriveAuthService : IGoogleDriveAuthService
{
    private static string _windowsClientId = Secrets.windowsGoogleDriveClientId; // UWP
    private static string _authURL = Literals.authURL;

    private readonly ITokenStorage _tokenStorage;
    private HttpListener? _listener = null;
    private GoogleCredential? _credential;
    private string? _email;

    public bool IsSignedIn => _credential != null;
    public string? Email => _email;

    public WindowsGoogleDriveAuthService()
    {
        _tokenStorage = new TokenStorage(Literals.authBufferSeconds);
    }

    public async Task Init()
    {
        var hasRefreshToken = await _tokenStorage.Exists("refresh_token");
        if (!IsSignedIn && hasRefreshToken)
        {
            await SignIn();
        }
    }

    public async Task<GoogleCredential?> SignIn()
    {
        try
        {
            var accessToken = await GetValidAccessToken();
            _credential = GoogleCredential.FromAccessToken(accessToken);

            await VerifyAuthorization();

            return _credential;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"SignIn failed: {ex}");
            throw;
        }
    }

    private async Task<string> GetValidAccessToken()
    {
        var tokenInfo = await _tokenStorage.GetTokenValue("access_token");
        string? accessToken = tokenInfo.Value;
        bool expired = _tokenStorage.IsExpired("access_token");
        bool hasRefreshToken = await _tokenStorage.Exists("refresh_token");

        if (string.IsNullOrEmpty(accessToken) || expired)
        {
            if (hasRefreshToken)
            {
                accessToken = await TryRefreshToken();
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                accessToken = await StartAuthCodeFlow();
            }
        }

        if (string.IsNullOrEmpty(accessToken))
            throw new InvalidOperationException("Access token missing after authorization flow");

        return accessToken;
    }

    private async Task VerifyAuthorization()
    {
        var oauth2Service = new Oauth2Service(new BaseClientService.Initializer
        {
            HttpClientInitializer = _credential,
            ApplicationName = "XMLParserUniversityProject"
        });

        try
        {
            var userInfo = await oauth2Service.Userinfo.Get().ExecuteAsync();
            _email = userInfo.Email;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            Trace.TraceWarning("Access token unauthorized, restarting auth flow...");
            DeleteTokens();

            var accessToken = await StartAuthCodeFlow();
            _credential = GoogleCredential.FromAccessToken(accessToken);

            var retryUserInfo = await oauth2Service.Userinfo.Get().ExecuteAsync();
            _email = retryUserInfo.Email;
        }
    }

    public async Task SignOut()
    {
        await RevokeTokens();
    }

    private async Task<string?> StartAuthCodeFlow()
    {
        await DoAuthCodeFlowWindows();

        return (await _tokenStorage.GetTokenValue("access_token")).Value;
    }

    private async Task DoAuthCodeFlowWindows()
    {
        Trace.TraceInformation("Starting auth code flow");
        if (DeviceInfo.Current.Platform != DevicePlatform.WinUI)
        {
            throw new NotImplementedException($"Auth flow for platform {DeviceInfo.Current.Platform} not implemented");
        }

        var authUrl = _authURL;
        var clientId = _windowsClientId;
        var localPort = Literals.authFlowPort;
        var redirectUri = $"http://localhost:{localPort}";
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        var parameters = GenerateAuthParameters(redirectUri, clientId, codeChallenge);
        var queryString = string.Join("&", parameters.Select(
            param => $"{WebUtility.UrlEncode(param.Key)}={WebUtility.UrlEncode(param.Value)}"
        ));
        var fullAuthUrl = $"{authUrl}?{queryString}";

        await Launcher.OpenAsync(fullAuthUrl);
        var authorizationCode = await StartLocalHttpServerAsync(localPort);

        await GetInitialToken(authorizationCode, redirectUri, clientId, codeVerifier);
    }

    private static Dictionary<string, string> GenerateAuthParameters(string redirectUri, string clientId, string codeChallenge)
    {
        return new Dictionary<string, string>
        {
            { "scope", string.Join(' ', [Oauth2Service.Scope.UserinfoProfile, Oauth2Service.Scope.UserinfoEmail,
                                        DriveService.Scope.Drive, DriveService.Scope.DriveFile, DriveService.Scope.DriveAppdata]) },
            { "access_type", "offline" },
            { "include_granted_scopes", "true" },
            { "response_type", "code" },
            { "redirect_uri", redirectUri },
            { "client_id", clientId },
            { "code_challenge_method", "S256" },
            { "code_challenge", codeChallenge },
        };
    }

    private async Task GetInitialToken(string authorizationCode, string redirectUri, string clientId, string codeVerifier)
    {
        var tokenEndpoint = "https://oauth2.googleapis.com/token";
        var client = new HttpClient();
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", authorizationCode),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("code_verifier", codeVerifier)
            ])
        };

        var response = await client.SendAsync(tokenRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Trace.TraceError($"Error requesting initial token: {responseBody}");
            throw new HttpRequestException("Failed to get initial token");
        }

        Trace.TraceInformation($"Access token successfully retrieved");

        var jsonToken = JsonObject.Parse(responseBody);
        var accessToken = jsonToken!["access_token"]!.ToString();
        var refreshToken = jsonToken!["refresh_token"]!.ToString();
        var accessTokenExpiresIn = int.Parse(jsonToken!["expires_in"]!.ToString());
        await _tokenStorage.SetToken("access_token", accessToken, accessTokenExpiresIn);
        await _tokenStorage.SetToken("refresh_token", refreshToken);
    }

    private async Task<string?> TryRefreshToken()
    {
        try
        {
            await RefreshToken();
            return (await _tokenStorage.GetTokenValue("access_token")).Value;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Failed to refresh token: {ex.Message}");
            DeleteTokens();
            return null;
        }
    }

    private async Task RefreshToken()
    {
        var clientId = _windowsClientId;
        var tokenEndpoint = "https://oauth2.googleapis.com/token";
        var refreshToken = (await _tokenStorage.GetTokenValue("refresh_token")).Value;
        var client = new HttpClient();
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", refreshToken!)
                ]
            )
        };

        var response = await client.SendAsync(tokenRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Trace.TraceError($"Error refreshing token: {responseBody}");
            throw new HttpRequestException("Failed to refresh token");
        }

        Trace.TraceInformation($"Refresh token successfully retrieved");

        var jsonToken = JsonObject.Parse(responseBody);
        var accessToken = jsonToken!["access_token"]!.ToString();
        var accessTokenExpiresIn = int.Parse(jsonToken!["expires_in"]!.ToString());
        await _tokenStorage.SetToken("access_token", accessToken, accessTokenExpiresIn);
    }

    private async Task RevokeTokens()
    {
        var revokeEndpoint = "https://oauth2.googleapis.com/revoke";
        var accessToken = (await _tokenStorage.GetTokenValue("access_token")).Value;
        var client = new HttpClient();
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, revokeEndpoint)
        {
            Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("token", accessToken!),
                ]
            )
        };

        var response = await client.SendAsync(tokenRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Trace.TraceError($"Error revoking token: {responseBody}");
            throw new HttpRequestException("Failed to revoke token");
        }

        Trace.TraceInformation($"Revoke token: {responseBody}");

        DeleteTokens();

        _credential = null;
    }

    private void DeleteTokens()
    {
        _tokenStorage.DeleteToken("access_token");
        _tokenStorage.DeleteToken("refresh_token");
    }

    private async Task<string> StartLocalHttpServerAsync(int port)
    {
        if (_listener is null)
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{port}/");
                _listener.Start();
            }
            catch (HttpListenerException)
            {
                Trace.TraceError($"Port {port} unavailable");
                throw;
            }
        }
        else if (!_listener.IsListening)
        {
            _listener.Start();
        }

        Trace.TraceInformation($"Listening on http://localhost:{port}/...");
        var context = await _listener.GetContextAsync();

        var code = context.Request.QueryString["code"];
        var response = context.Response;
        var responseString = "Authorization complete. You can close this window.";
        var buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.OutputStream.Close();

        _listener.Stop();
        _listener = null;

        if (code is null)
        {
            throw new HttpRequestException("Auth code not returned");
        }

        return code;
    }

    private static string GenerateCodeVerifier()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32]; // Length can vary, e.g., 43-128 characters
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}