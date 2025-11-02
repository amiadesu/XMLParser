using System;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace XMLParser.Components.TokenStorage;

public class TokenStorage : ITokenStorage
{
    private readonly long _bufferSeconds = 0;
    private static readonly long _maxSeconds = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

    public TokenStorage(long bufferSeconds = 0)
    {
        _bufferSeconds = bufferSeconds;
    }

    public async Task SetToken(string tokenName, string tokenValue)
    {
        await SecureStorage.SetAsync(tokenName, tokenValue);
    }

    public async Task SetToken(string tokenName, string tokenValue, long expiresInSeconds)
    {
        await SetToken(tokenName, tokenValue);
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);
        Preferences.Set(ExpiresAtKey(tokenName), expiresAt.ToUnixTimeSeconds());
    }

    public void DeleteToken(string tokenName)
    {
        SecureStorage.Remove(tokenName);
        Preferences.Remove(ExpiresAtKey(tokenName));
    }

    public async Task<TokenInfo> GetTokenValue(string tokenName)
    {
        if (IsExpired(tokenName))
        {
            DeleteToken(tokenName);
            return new(null, null);
        }

        var value = await SecureStorage.GetAsync(tokenName);
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(
            Preferences.Get(ExpiresAtKey(tokenName), _maxSeconds)
        );
        return new(value, expiresAt);
    }

    public bool IsExpired(string tokenName)
    {
        var expiresAtUnix = Preferences.Get(ExpiresAtKey(tokenName), _maxSeconds);
        if (expiresAtUnix == _maxSeconds)
            return false;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var bufferAdjustedNow = now + _bufferSeconds;
        return expiresAtUnix <= bufferAdjustedNow;
    }

    public async Task<bool> Exists(string tokenName)
    {
        if (IsExpired(tokenName))
        {
            DeleteToken(tokenName);
            return false;
        }

        var value = await SecureStorage.GetAsync(tokenName);
        return value is not null;
    }

    private static string ExpiresAtKey(string tokenName)
    {
        return $"{tokenName}_expires_at";
    }
}