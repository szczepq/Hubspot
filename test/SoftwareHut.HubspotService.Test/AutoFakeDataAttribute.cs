using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using AutoFixture.Xunit2;
using SoftwareHut.HubspotService.Models;
using System;
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
    }

    public class AutoFakeCustomization : CompositeCustomization
    {
        public AutoFakeCustomization()
            : base(
                new AutoFakeItEasyCustomization { GenerateDelegates = true },
                new TestCustomization())
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
        }
    }
}
