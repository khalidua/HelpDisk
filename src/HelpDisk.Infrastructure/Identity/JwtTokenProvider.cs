using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using HelpDisk.Application.Abstractions;
using HelpDisk.Application.Features.Auth.Dtos;
using HelpDisk.Domain.Shared;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HelpDisk.Infrastructure.Identity;

public sealed class JwtTokenProvider : ITokenProvider
{
    private readonly JwtOptions _options;
    private readonly IDateTimeProvider _dateTime;

    public JwtTokenProvider(IOptions<JwtOptions> options, IDateTimeProvider dateTime)
    {
        _options = options.Value;
        _dateTime = dateTime;
    }
    public Task<Result<TokenResponse>> GenerateTokenAsync(UserInfo userInfo, CancellationToken cancellationToken = default)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userInfo.UserId),
            new(ClaimTypes.Name, $"{userInfo.FirstName} {userInfo.LastName}"),
            new(ClaimTypes.Email, userInfo.Email ?? string.Empty),
            new(ClaimTypes.Role, userInfo.Role),
            new(ClaimTypes.GroupSid, userInfo.CompanyId?.ToString() ?? string.Empty)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAt = _dateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);

        var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenString = tokenHandler.WriteToken(token);
        var response = new TokenResponse(
                        tokenString,
                        expiresAt,
                        userInfo.Role);

        return Task.FromResult(Result.Success(response));
    }


}
