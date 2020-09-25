using SoftwareHut.HubspotService.Facades;
using SoftwareHut.HubspotService.Mappers;
using SoftwareHut.HubspotService.Models;
using SoftwareHut.HubspotService.Repositories;
using System;
using System.Threading.Tasks;

namespace SoftwareHut.HubspotService.Services
{
    public interface IHubspotService
    {
        Task<ContactsList> GetHubspotContactsAsync(int count);
        Task<int> CreateHubspotContactAsync(CreateContact createContact);
    }

    public class HubspotService : IHubspotService
    {
        public IHubspotClientFacade HubspotClientFacade { get; }
        public IHubspotMapper HubspotMapper { get; }
        public IUserRepository UserRepository { get; }

        public HubspotService(IHubspotClientFacade hubspotClientFacade, IHubspotMapper hubspotMapper,
            IUserRepository userRepository)
        {
            HubspotClientFacade = hubspotClientFacade ?? throw new ArgumentNullException(nameof(hubspotClientFacade));
            HubspotMapper = hubspotMapper ?? throw new ArgumentNullException(nameof(hubspotMapper));
            UserRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public Task<ContactsList> GetHubspotContactsAsync(int count)
            => GetHubspotContactsInternalAsync(count);

        private async Task<ContactsList> GetHubspotContactsInternalAsync(int count)
        {
            var hubspotContacts = await HubspotClientFacade.GetContactsAsync(count);
            return HubspotMapper.FromHubspotContacts(hubspotContacts);
        }

        public Task<int> CreateHubspotContactAsync(CreateContact createContact)
        {
            if (createContact == null) throw new ArgumentNullException(nameof(createContact));
            return CreateHubspotContactInternalAsync(createContact);
        }

        private async Task<int> CreateHubspotContactInternalAsync(CreateContact createContact)
        {
            if (createContact == null) throw new ArgumentNullException(nameof(createContact));

            var hubspotContact = HubspotMapper.ToCreateHubspotContact(createContact);
            var response = await HubspotClientFacade.CreateContactsAsync(hubspotContact);
            var contact = HubspotMapper.FromHubspotContact(response);
            return await UserRepository.CreateUserAsync(contact.ExternalId, contact.Email);
        }
    }
}