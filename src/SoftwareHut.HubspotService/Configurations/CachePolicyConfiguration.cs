using System;

namespace SoftwareHut.HubspotService.Configurations
{
    public interface ICachePolicyConfiguration
    {
        TimeSpan CacheDuration { get; }
    }

    public class CachePolicyConfiguration : ICachePolicyConfiguration
    {
        public const string SectionName = "CachePolicy";
        public int? CacheDurationSec { get; set; }

        public TimeSpan CacheDuration =>
            TimeSpan.FromSeconds(CacheDurationSec ?? 10);
    }
}