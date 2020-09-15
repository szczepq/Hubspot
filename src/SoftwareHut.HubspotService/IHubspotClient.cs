using Refit;
using SoftwareHut.HubspotService.Models;
using System.Threading.Tasks;

namespace SoftwareHut.HubspotService
{
    public interface IHubspotClient
    {
        [Get("/contacts/v1/lists/all/contacts/all?hapikey={hapikey}&count={count}")]
        Task<HubspotContacts> GetContacts(
            string hapikey, 
            int count);

        [Post("/contacts/v1/contact?hapikey={hapikey}")]
        Task<HubspotContacts> GetContacts(
            string hapikey,
            CreateHubspotContact contacts);
    }
}