namespace ERP.Application.Common.Contracts;

public interface INumberSequenceService
{
    Task<string> NextAsync(string prefix, CancellationToken cancellationToken);
}
