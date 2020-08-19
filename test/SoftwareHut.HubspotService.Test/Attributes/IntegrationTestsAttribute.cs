using System;
using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SoftwareHut.HubspotService.Test.Attributes
{
    [TraitDiscoverer("SoftwareHut.HubspotService.Test.Attributes.IntegrationTestsDiscoverer",
        "SoftwareHut.HubspotService.Test")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class IntegrationTestsAttribute : Attribute, ITraitAttribute
    {
    }

    public class IntegrationTestsDiscoverer : ITraitDiscoverer
    {
        public const string KEY = "Category";
        public const string Category = "IntegrationTests";

        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            yield return new KeyValuePair<string, string>(KEY, Category);
        }
    }
}