using System;
using Polly.Caching;
using SoftwareHut.HubspotService.Configurations;
using SoftwareHut.HubspotService.Models;

namespace SoftwareHut.HubspotService.Policies
{
    public interface IHubspotContactsCachePolicy : ICachePolicy<HubspotContacts>
    {
    }
    public class HubspotContactsCachePolicy : CachePolicy<HubspotContacts>,
        IHubspotContactsCachePolicy
    {
        public ICachePolicyConfiguration CachePolicyConfiguration { get; }

        public HubspotContactsCachePolicy(
            IAsyncCacheProvider asyncCacheProvider,
            ICachePolicyConfiguration cachePolicyConfiguration)
            : base(
                asyncCacheProvider ?? throw new ArgumentNullException(nameof(asyncCacheProvider)),
                cachePolicyConfiguration?.CacheDuration ?? throw new ArgumentNullException(nameof(cachePolicyConfiguration)))
        {
            CachePolicyConfiguration = cachePolicyConfiguration;
        }
    }
}