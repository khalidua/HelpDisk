using System;
using System.Collections.Generic;
using System.Text;

namespace HelpDisk.Application.Features.Auth.Dtos;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    Guid CompanyId
    );