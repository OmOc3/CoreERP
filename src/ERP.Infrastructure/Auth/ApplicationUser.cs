using Microsoft.AspNetCore.Identity;

namespace ERP.Infrastructure.Auth;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
    public Guid? DefaultBranchId { get; set; }
}
