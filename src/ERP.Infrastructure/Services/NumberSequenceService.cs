using ERP.Application.Common.Contracts;
using ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Services;

public sealed class NumberSequenceService : INumberSequenceService
{
    private readonly ErpDbContext _dbContext;
    private readonly IClock _clock;

    public NumberSequenceService(ErpDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<string> NextAsync(string prefix, CancellationToken cancellationToken)
    {
        var sequence = await _dbContext.NumberSequences.SingleOrDefaultAsync(x => x.Prefix == prefix, cancellationToken);
        if (sequence == null)
        {
            sequence = new NumberSequence
            {
                Prefix = prefix,
                CurrentValue = 0,
                LastUpdatedUtc = _clock.UtcNow
            };
            _dbContext.NumberSequences.Add(sequence);
        }

        sequence.CurrentValue += 1;
        sequence.LastUpdatedUtc = _clock.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return $"{prefix}-{_clock.UtcNow:yyyyMMdd}-{sequence.CurrentValue:D5}";
    }
}
