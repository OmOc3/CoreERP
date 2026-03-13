using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class Customer : BaseEntity, IAggregateRoot
{
    private Customer()
    {
    }

    public Customer(
        string code,
        string name,
        string? taxNumber,
        string? email,
        string? phone,
        string? address,
        decimal creditLimit,
        int paymentTermsDays)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        TaxNumber = taxNumber?.Trim();
        Email = email?.Trim();
        Phone = phone?.Trim();
        Address = address?.Trim();
        CreditLimit = creditLimit;
        PaymentTermsDays = paymentTermsDays;
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? TaxNumber { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public decimal CreditLimit { get; private set; }
    public int PaymentTermsDays { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        string code,
        string name,
        string? taxNumber,
        string? email,
        string? phone,
        string? address,
        decimal creditLimit,
        int paymentTermsDays,
        bool isActive)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        TaxNumber = taxNumber?.Trim();
        Email = email?.Trim();
        Phone = phone?.Trim();
        Address = address?.Trim();
        CreditLimit = creditLimit;
        PaymentTermsDays = paymentTermsDays;
        IsActive = isActive;
    }
}
