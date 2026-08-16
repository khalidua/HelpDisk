using System;
using System.Collections.Generic;
using System.Text;

using HelpDisk.Application.Abstractions;
using HelpDisk.Application.Features.Auth.Dtos;
using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Features.Auth;

public class AuthService : IAuthService
{
    private readonly IIdentityService _identityService;
    private readonly ITokenProvider _tokenProvider;

    public AuthService(IIdentityService identityService,ITokenProvider tokenProvider)
    {
        _identityService = identityService;
        _tokenProvider = tokenProvider;
    }
    public async Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _identityService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error;
        }
        return await _tokenProvider.GenerateTokenAsync(result.Value, cancellationToken);
    }

    public async Task<Result<string>> RegisterAsync( RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _identityService.CreateCustomerAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            cancellationToken);

        return result;
    }
}
