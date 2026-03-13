namespace ERP.Application.Common.Contracts;

public interface IInventoryPolicy
{
    bool AllowNegativeStock { get; }
}
