using FluentValidation.TestHelper;
using FluentValidation.Validators;
using FluentValidation.Validators.UnitTestExtension.Composer;
using FluentValidation.Validators.UnitTestExtension.Core;
using SoftwareHut.HubspotService.Models;
using SoftwareHut.HubspotService.Test.Attributes;
using SoftwareHut.HubspotService.Validators;
using Xunit;

namespace SoftwareHut.HubspotService.Test.Validators
{
    [UnitTests]
    public class CreateContactValidatorTests
    {
        private readonly CreateContactValidator sut = new CreateContactValidator();

        [Fact]
        public void Class_IsConfiguredCorrectly()
        {
            sut.ShouldHaveRules(x => x,
                BaseVerifiersSetComposer.Build()
                    .AddPropertyValidatorVerifier<NotNullValidator>()
                    .Create());
        }

        [Fact]
        public void FirstName_IsConfiguredCorrectly()
        {
            sut.ShouldHaveRules(x => x.FirstName,
                BaseVerifiersSetComposer.Build()
                    .AddPropertyValidatorVerifier<NotNullValidator>()
                    .AddPropertyValidatorVerifier<NotEmptyValidator>()
                    .AddMinimumLengthValidatorVerifier(2)
                    .Create());
        }

        [Fact]
        public void Email_IsConfiguredCorrectly()
        {
            sut.ShouldHaveRules(x => x.Email,
                BaseVerifiersSetComposer.Build()
                    .AddPropertyValidatorVerifier<NotNullValidator>()
                    .AddPropertyValidatorVerifier<NotEmptyValidator>()
                    .AddPropertyValidatorVerifier<AspNetCoreCompatibleEmailValidator>()
                    .AddPropertyValidatorVerifier<PredicateValidator>()
                    .Create());
        }

        [Theory]
        [AutoFakeData(typeof(CreateContact), "Email", "test@softwarehut.com")]
        [AutoFakeData(typeof(CreateContact), "Email", "test.test@softwarehut.com")]
        [AutoFakeData(typeof(CreateContact), "Email", "test_test@softwarehut.com")]
        [AutoFakeData(typeof(CreateContact), "Email", "test2@softwarehut.com")]
        public void ValidEmail_PassesValidation(CreateContact contact)
        {
            sut.ShouldNotHaveValidationErrorFor(x => x.Email, contact);
        }

        [Theory]
        [AutoFakeData(typeof(CreateContact), "Email", "test@s_oftwarehut.com")]
        [AutoFakeData(typeof(CreateContact), "Email", "test@softwarehut.pl")]
        [AutoFakeData(typeof(CreateContact), "Email", "test@hut.cpm")]
        public void InvalidEmail_PassesValidation(CreateContact contact)
        {
            sut.ShouldHaveValidationErrorFor(x => x.Email, contact);
        }
    }
}