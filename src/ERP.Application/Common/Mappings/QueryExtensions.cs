using ERP.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Common.Mappings;

public static class QueryExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        ListQuery request,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.NormalizedPageNumber - 1) * request.NormalizedPageSize)
            .Take(request.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            PageNumber = request.NormalizedPageNumber,
            PageSize = request.NormalizedPageSize,
            TotalCount = totalCount
        };
    }
}
