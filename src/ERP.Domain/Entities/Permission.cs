using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class Permission : BaseEntity, IAggregateRoot
{
    private Permission()
    {
    }

    public Permission(string module, string code, string name, string? description)
    {
        Module = module.Trim();
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description?.Trim();
    }

    public string Module { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
}
