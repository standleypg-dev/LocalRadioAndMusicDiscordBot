using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Api;

public interface IJwtTokenGenerator
{
    string GenerateToken(LoginRequest request);
}

public class JwtTokenGenerator(IConfiguration configuration): IJwtTokenGenerator
{
    public string GenerateToken(LoginRequest request)
    {
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>("JwtSettings:Secret") ?? string.Empty)),
            SecurityAlgorithms.HmacSha256);
        
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Name, request.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        var token = new JwtSecurityToken(
            issuer: configuration.GetValue<string>("JwtSettings:Issuer"),
            audience: configuration.GetValue<string>("JwtSettings:Audience"),
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: signingCredentials);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}