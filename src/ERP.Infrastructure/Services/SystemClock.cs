using ERP.Application.Common.Contracts;

namespace ERP.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
