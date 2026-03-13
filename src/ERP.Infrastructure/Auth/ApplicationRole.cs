using Microsoft.AspNetCore.Identity;

namespace ERP.Infrastructure.Auth;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
}
