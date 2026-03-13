namespace ERP.Infrastructure.Services;

public sealed class ErpOptions
{
    public JwtOptions Jwt { get; init; } = new();
    public InventoryOptions Inventory { get; init; } = new();

    public sealed class JwtOptions
    {
        public string Issuer { get; init; } = "ERP.Api";
        public string Audience { get; init; } = "ERP.Client";
        public string Key { get; init; } = "ReplaceThisDevelopmentKeyForJwtTokens12345";
        public int AccessTokenMinutes { get; init; } = 60;
        public int RefreshTokenDays { get; init; } = 7;
    }

    public sealed class InventoryOptions
    {
        public bool AllowNegativeStock { get; init; }
    }
}
