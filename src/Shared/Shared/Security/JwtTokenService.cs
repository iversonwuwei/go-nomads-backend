using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GoNomads.Shared.Security;

/// <summary>
///     JWT Token 生成和验证服务
/// </summary>
public class JwtTokenService
{
    private readonly int _accessTokenExpirationMinutes;
    private readonly string _audience;
    private readonly string _issuer;
    private readonly int _refreshTokenExpirationDays;
    private readonly string _secret;

    public JwtTokenService(IConfiguration configuration)
    {
        _secret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured");
        _issuer = configuration["Jwt:Issuer"] ?? "go-nomads";
        _audience = configuration["Jwt:Audience"] ?? "go-nomads-users";
        // Access Token 默认有效期：24小时（1440分钟）
        _accessTokenExpirationMinutes = configuration.GetValue("Jwt:AccessTokenExpirationMinutes", 1440);
        // Refresh Token 默认有效期：30天
        _refreshTokenExpirationDays = configuration.GetValue("Jwt:RefreshTokenExpirationDays", 30);
    }

    /// <summary>
    ///     生成访问令牌
    /// </summary>
    public string GenerateAccessToken(string userId, string email, string role)
    {
        var claims = new List<Claim>
        {
            new("sub", userId),
            new("email", email),
            new("role", role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _issuer,
            _audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    ///     生成刷新令牌
    /// </summary>
    public string GenerateRefreshToken(string userId)
    {
        var claims = new List<Claim>
        {
            new("sub", userId),
            new("type", "refresh"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _issuer,
            _audience,
            claims,
            expires: DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    ///     验证并解析令牌
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     从令牌中提取用户ID
    /// </summary>
    public string? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        return principal?.FindFirst("sub")?.Value;
    }

    /// <summary>
    ///     获取 Access Token 过期时间（秒）
    /// </summary>
    public int GetAccessTokenExpirationSeconds()
    {
        return _accessTokenExpirationMinutes * 60;
    }

    /// <summary>
    ///     获取 Refresh Token 过期时间（秒）
    /// </summary>
    public int GetRefreshTokenExpirationSeconds()
    {
        return _refreshTokenExpirationDays * 24 * 60 * 60;
    }
}