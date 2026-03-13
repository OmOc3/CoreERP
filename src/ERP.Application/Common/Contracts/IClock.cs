namespace ERP.Application.Common.Contracts;

public interface IClock
{
    DateTime UtcNow { get; }
}
