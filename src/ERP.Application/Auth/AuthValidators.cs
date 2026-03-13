using FluentValidation;

namespace ERP.Application.Auth;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UserNameOrEmail).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
