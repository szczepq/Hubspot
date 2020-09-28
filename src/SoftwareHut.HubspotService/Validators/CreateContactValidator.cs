using FluentValidation;
using SoftwareHut.HubspotService.Models;

namespace SoftwareHut.HubspotService.Validators
{
    public class CreateContactValidator : AbstractValidator<CreateContact>
    {
        public CreateContactValidator()
        {
            RuleFor(contact => contact)
                .NotNull();

            RuleFor(contact => contact.FirstName)
                .NotNull()
                .NotEmpty()
                .MinimumLength(2);

            RuleFor(contact => contact.Email)
                .NotNull()
                .NotEmpty()
                .EmailAddress()
                .Must(m => m != null && m.EndsWith("@softwarehut.com"))
                .WithMessage("'{PropertyName}' should ends with @softwarehut.com");
        }
    }
}