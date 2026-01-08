using AuthMicroservice.Application.Dtos;
using FluentValidation;

namespace AuthMicroservice.Application.Validators
{
    public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
    {
        public CreateUserDtoValidator()
        {
            
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters")
                .MaximumLength(20).WithMessage("Username must not exceed 20 characters")
                .Matches("^[a-zA-Z0-9._]+$")
                .WithMessage("Username can contain only letters, numbers, dots and underscores");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Email format is invalid")
                .MaximumLength(100).WithMessage("Email must not exceed 100 characters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character")
                .Must(p => !p.Contains(" "))
                .WithMessage("Password must not contain spaces");

            RuleFor(x => x.Role)
                .Must(BeValidRole)
                .When(x => !string.IsNullOrWhiteSpace(x.Role))
                .WithMessage("Role must be either Admin or User");
        }

        private bool BeValidRole(string role)
        {
            var allowedRoles = new[] { "Admin", "User" ,"Engineer","Operator"};
            return allowedRoles.Contains(role);
        }
    }
}
