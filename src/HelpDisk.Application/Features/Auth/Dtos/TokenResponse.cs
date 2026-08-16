using System;
using System.Collections.Generic;
using System.Text;

namespace HelpDisk.Application.Features.Auth.Dtos;

public sealed record TokenResponse(
    string Token,
    DateTime ExpiresAt,
    string Role
    );
