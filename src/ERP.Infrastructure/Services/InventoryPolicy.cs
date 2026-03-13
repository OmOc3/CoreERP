using ERP.Application.Common.Contracts;
using Microsoft.Extensions.Options;

namespace ERP.Infrastructure.Services;

public sealed class InventoryPolicy : IInventoryPolicy
{
    private readonly IOptions<ErpOptions> _options;

    public InventoryPolicy(IOptions<ErpOptions> options)
    {
        _options = options;
    }

    public bool AllowNegativeStock => _options.Value.Inventory.AllowNegativeStock;
}
