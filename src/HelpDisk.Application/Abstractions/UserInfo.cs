using System;
using System.Collections.Generic;
using System.Text;

using HelpDisk.Domain.Users;

namespace HelpDisk.Application.Abstractions;

public sealed record UserInfo(string UserId, string? Email, string FirstName, string LastName, UserRole Role);
