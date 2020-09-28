using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using AutoFixture.Xunit2;
using SoftwareHut.HubspotService.Models;
using System;
using System.Linq;
using System.Reflection;
using SoftwareHut.HubspotService.Test.ArgumentSpecimens;

namespace SoftwareHut.HubspotService.Test
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AutoFakeDataAttribute : AutoDataAttribute
    {
        public AutoFakeDataAttribute()
            : base(() => new Fixture().Customize(new AutoFakeCustomization()))
        {
        }

        protected AutoFakeDataAttribute(params ICustomization[] customizations)
            : base(() => new Fixture().Customize(new AutoFakeCustomization(customizations)))

        {
        }
        public AutoFakeDataAttribute(Type targetType, string argumentName, object argumentValue)
            : this(new CustomizationsWithTargetValue(targetType, argumentName, argumentValue))
        {
        }
    }

    public class AutoFakeCustomization : CompositeCustomization
    {
        public AutoFakeCustomization()
            : base(
                new AutoFakeItEasyCustomization { GenerateDelegates = true },
                new TestCustomization())
        {
        }

        public AutoFakeCustomization(params ICustomization[] customizations)
            : base(customizations.Concat(new ICustomization[]
            {
                new AutoFakeItEasyCustomization {GenerateDelegates = true},
                new TestCustomization()
            }))
        {
        }
    }

    public class TestCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customizations.Add(
                new ConstructorArgumentSpecimen<HubspotIdentity, string>(
                    "type", ()=> "EMAIL"));
            fixture.Customizations.Add(
                new ConstructorArgumentSpecimen<HubspotIdentity, string>(
                    "value", Faker.Internet.Email));

            fixture.Customizations.Add(
                new ConstructorArgumentSpecimen<CreateContact, string>(
                    "email", () => $"{Faker.Name.First()}@softwarehut.com"));
        }
    }
}
