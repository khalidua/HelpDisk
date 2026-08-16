using System;
using System.Collections.Generic;
using System.Text;

using HelpDisk.Application.Features.Auth.Dtos;
using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Features.Auth;

public interface IAuthService
{
    Task<Result<string>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
