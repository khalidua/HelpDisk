using System;
using System.Collections.Generic;
using System.Text;

using HelpDisk.Domain.Users;

namespace HelpDisk.Application.Features.Auth.Dtos;

public sealed record TokenResponse(
    string Token,
    DateTime ExpiresAt,
    UserRole Role
    );
