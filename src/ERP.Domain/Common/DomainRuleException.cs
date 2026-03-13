namespace ERP.Domain.Common;

public sealed class DomainRuleException : Exception
{
    public DomainRuleException(string message)
        : base(message)
    {
    }
}
