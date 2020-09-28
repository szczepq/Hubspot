using Microsoft.AspNetCore.Mvc;
using SoftwareHut.HubspotService.Models;
using SoftwareHut.HubspotService.Services;
using System;
using System.Threading.Tasks;

namespace SoftwareHut.HubspotService.Controllers
{
    [Route("api/hubspot")]
    [ApiController]
    public class HubspotController : ControllerBase
    {
        public IHubspotService HubspotService { get; }

        public HubspotController(IHubspotService hubspotService)
        {
            HubspotService = hubspotService ?? throw new ArgumentNullException(nameof(hubspotService));
        }

        [HttpGet]
        public async Task<IActionResult> GetContactsAsync(int count)
        {
            return Ok(await HubspotService.GetHubspotContactsAsync(count));
        }

        [HttpPost]
        public async Task<IActionResult> CreateContactAsync(CreateContact createContact)
        {
            await HubspotService.CreateHubspotContactAsync(createContact);
            return Ok();
        }
    }
}