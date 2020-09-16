using System.Threading.Tasks;
using Refit;
using SoftwareHut.HubspotService.Models;

namespace SoftwareHut.HubspotService.Clients
{
    public interface IHubspotClient
    {
        [Get("/contacts/v1/lists/all/contacts/all?hapikey={hapikey}&count={count}")]
        Task<HubspotContacts> GetContactsAsync(
            string hapikey, 
            int count);

        [Post("/contacts/v1/contact?hapikey={hapikey}")]
        Task<HubspotProfile> CreateContactsAsync(
            string hapikey,
            CreateHubspotContact contacts);
    }
}