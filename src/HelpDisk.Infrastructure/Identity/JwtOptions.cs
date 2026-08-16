using System;
using System.Collections.Generic;
using System.Text;

namespace HelpDisk.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    public int ExpirationMinutes { get; set; }
}
