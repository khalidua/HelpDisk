using System;
using System.Collections.Generic;
using System.Text;

using HelpDisk.Application.Features.Auth.Dtos;
using HelpDisk.Domain.Shared;

namespace HelpDisk.Application.Abstractions;

public interface ITokenProvider
{
    Task<Result<TokenResponse>> GenerateTokenAsync(UserInfo userInfo, CancellationToken cancellationToken = default);
}
