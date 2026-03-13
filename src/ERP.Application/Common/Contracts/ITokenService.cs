namespace ERP.Application.Common.Contracts;

public interface ITokenService
{
    Task<Auth.TokenEnvelope> CreateTokenAsync(
        Guid userId,
        string userName,
        string? email,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        IReadOnlyCollection<Guid> branchIds,
        Guid? defaultBranchId,
        CancellationToken cancellationToken);
    string GenerateRefreshToken();
}
