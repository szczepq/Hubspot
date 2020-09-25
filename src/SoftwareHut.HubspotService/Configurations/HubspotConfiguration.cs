using System.ComponentModel.DataAnnotations;

namespace SoftwareHut.HubspotService.Configurations
{
    public interface IHubspotConfiguration
    {
        string HapiKey { get; }
        string BaseUrl { get; }

    }
    public class HubspotConfiguration : IHubspotConfiguration
    {
        public const string SectionName = "Hubspot";

        [Required]
        public string HapiKey { get; set; }

        [Required]
        public string BaseUrl { get; set; }
    }
}
