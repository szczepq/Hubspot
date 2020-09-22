using System.Linq;
using AutoFixture.Xunit2;
using Xunit;
using SoftwareHut.HubspotService.Mappers;
using SoftwareHut.HubspotService.Models;
using SoftwareHut.HubspotService.Test.Attributes;
using SoftwareHut.HubspotService.Test.Deserialize;

namespace SoftwareHut.HubspotService.Test.Mappers
{
    [IntegrationTests]
    public class HubspotMapperTests : BaseAssertion<HubspotMapper>
    {
        [Theory, AutoData]
        public void ToCreateHubspotContact_Ok(
            HubspotMapper sut,
            CreateContact contact)
        {
            var createHubspotContact = sut.ToCreateHubspotContact(contact);

            Assert.NotNull(createHubspotContact);
            Assert.Equal(2, createHubspotContact.Properties.Count);

            var email = createHubspotContact.Properties
                .FirstOrDefault(x => x.Property == "email");
            Assert.NotNull(email);
            Assert.Equal(contact.Email, email.Value);

            var firstName = createHubspotContact.Properties
                .FirstOrDefault(x => x.Property == "firstname");
            Assert.NotNull(firstName);
            Assert.Equal(contact.FirstName, firstName.Value);
        }

        [Theory, AutoFakeData]
        public void FromHubspotContact_Ok(
            HubspotMapper sut,
            HubspotContact hubspotContact)
        {
            var contact = sut.FromHubspotContact(hubspotContact);
            var email = hubspotContact.Profiles.First().Identity.First(x => x.Type == "EMAIL").Value;

            Assert.NotNull(contact);
            Assert.Equal(hubspotContact.Id, contact.ExternalId);
            Assert.Equal(email, contact.Email);
        }

        [Theory, AutoFakeData]
        public void FromHubspotContacts_Ok(
            HubspotMapper sut,
            HubspotContacts hubspotContacts)
        {
            var contactsList = sut.FromHubspotContacts(hubspotContacts);

            Assert.NotNull(contactsList);
            Assert.Equal(hubspotContacts.HubspotContact.Count, contactsList.Contacts.Count);
            Assert.True(contactsList.Contacts.All(x => x != null));
        }
    }
}
