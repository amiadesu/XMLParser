using System;
using System.Threading.Tasks;

namespace XMLParser.Components.TokenStorage;

public record TokenInfo(string? Value, DateTimeOffset? ExpiresAt);

public interface ITokenStorage
{
    public Task SetToken(string tokenName, string tokenValue);
    public Task SetToken(string tokenName, string tokenValue, long expiresInSeconds);
    public void DeleteToken(string tokenName);
    public Task<TokenInfo> GetTokenValue(string tokenName);
    public bool IsExpired(string tokenName);
    public Task<bool> Exists(string tokenName);
}