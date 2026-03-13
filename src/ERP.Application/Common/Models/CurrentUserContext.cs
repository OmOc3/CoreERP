namespace ERP.Application.Common.Models;

public sealed class CurrentUserContext
{
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string? Email { get; init; }
    public string? IpAddress { get; init; }
    public bool IsAuthenticated => UserId.HasValue;
    public bool IsAdministrator { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Permissions { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<Guid> BranchIds { get; init; } = Array.Empty<Guid>();
    public Guid? DefaultBranchId { get; init; }
}
