using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public sealed class JwtTokenService
{
    private const string Issuer = "bankAPP";
    private const string Audience = "bankAPP.console";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);

    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public JwtTokenService(IConfiguration configuration)
    {
        string? configuredKey = configuration["Jwt:SigningKey"];
        byte[] keyBytes;
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            keyBytes = RandomNumberGenerator.GetBytes(32);
            Console.WriteLine("No Jwt:SigningKey configured; using a temporary key for this run. Tokens will not survive a restart.");
        }
        else
        {
            keyBytes = Encoding.UTF8.GetBytes(configuredKey);
        }

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes long.");
        }

        var signingKey = new SymmetricSecurityKey(keyBytes);
        _signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };
    }

    public string CreateToken(string username, string role, int? accountNumber = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role)
        };

        if (accountNumber.HasValue)
        {
            claims.Add(new Claim("account_number", accountNumber.Value.ToString()));
        }

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: now,
            expires: now.Add(TokenLifetime),
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ValidateToken(token, _validationParameters, out _);
    }
}