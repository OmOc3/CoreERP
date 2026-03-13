using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class Branch : BaseEntity, IAggregateRoot
{
    private Branch()
    {
    }

    public Branch(string code, string name, string? address, string? phone, string? email)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Address = address?.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim();
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string code, string name, string? address, string? phone, string? email, bool isActive)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Address = address?.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim();
        IsActive = isActive;
    }
}
