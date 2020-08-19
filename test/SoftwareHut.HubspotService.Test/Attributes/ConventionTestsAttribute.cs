using System;
using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SoftwareHut.HubspotService.Test.Attributes
{
    [TraitDiscoverer("SoftwareHut.HubspotService.Test.Attributes.ConventionTestsDiscoverer",
        "SoftwareHut.HubspotService.Test")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class ConventionTestsAttribute : Attribute, ITraitAttribute
    {
    }

    public class ConventionTestsDiscoverer : ITraitDiscoverer
    {
        public const string KEY = "Category";
        public const string Category = "ConventionTests";

        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            yield return new KeyValuePair<string, string>(KEY, Category);
        }
    }
}