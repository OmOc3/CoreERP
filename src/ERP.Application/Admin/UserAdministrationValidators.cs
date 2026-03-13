using FluentValidation;

namespace ERP.Application.Admin;

public sealed class SaveUserRequestValidator : AbstractValidator<SaveUserRequest>
{
    public SaveUserRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Email).MaximumLength(128).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Roles).NotEmpty();
        RuleFor(x => x.Password)
            .MinimumLength(8)
            .When(x => !string.IsNullOrWhiteSpace(x.Password));
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}

public sealed class SaveRoleRequestValidator : AbstractValidator<SaveRoleRequest>
{
    public SaveRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Description).MaximumLength(256);
    }
}
