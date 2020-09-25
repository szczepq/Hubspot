using FakeItEasy;
using FluentAssertions;
using SoftwareHut.HubspotService.Models;
using SoftwareHut.HubspotService.Test.Deserialize;
using System.Threading.Tasks;
using SoftwareHut.HubspotService.Test.Attributes;
using Xunit;


namespace SoftwareHut.HubspotService.Test.Services
{
    [UnitTests]
    public class HubspotServiceTests : BaseAssertion<HubspotService.Services.HubspotService>
    {
        [Theory, AutoFakeData]
        public async Task GetHubspotContactsAsync_Ok(
            HubspotService.Services.HubspotService sut,
            int count,
            HubspotContacts hubspotContacts,
            ContactsList contactsList)
        {
            A.CallTo(() => sut.HubspotClientFacade.GetContactsAsync(count))
                .Returns(hubspotContacts);
            A.CallTo(() => sut.HubspotMapper.FromHubspotContacts(hubspotContacts))
                .Returns(contactsList);

            var response = await sut.GetHubspotContactsAsync(count);

            A.CallTo(() => sut.HubspotClientFacade.GetContactsAsync(count))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => sut.HubspotMapper.FromHubspotContacts(hubspotContacts))
                .MustHaveHappenedOnceExactly();

            response.Should().BeEquivalentTo(contactsList);
        }

        [Theory, AutoFakeData]
        public async Task CreateHubspotContactAsync_Ok(
            HubspotService.Services.HubspotService sut,
            CreateContact createContact,
            CreateHubspotContact createHubspotContact,
            HubspotContact hubspotContact,
            Contact contact,
            int id
        )
        {
            A.CallTo(() => sut.HubspotMapper.ToCreateHubspotContact(createContact))
                .Returns(createHubspotContact);
            A.CallTo(() => sut.HubspotClientFacade.CreateContactsAsync(createHubspotContact))
                .Returns(hubspotContact);
            A.CallTo(() => sut.HubspotMapper.FromHubspotContact(hubspotContact))
                .Returns(contact);
            A.CallTo(() => sut.UserRepository.CreateUserAsync(contact.ExternalId, contact.Email))
                .Returns(id);

            var response = await sut.CreateHubspotContactAsync(createContact);

            A.CallTo(() => sut.HubspotMapper.ToCreateHubspotContact(createContact))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => sut.HubspotClientFacade.CreateContactsAsync(createHubspotContact))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => sut.HubspotMapper.FromHubspotContact(hubspotContact))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => sut.UserRepository.CreateUserAsync(contact.ExternalId, contact.Email))
                .MustHaveHappenedOnceExactly();

            Assert.Equal(id, response);
        }
    }
}