using System;
using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SoftwareHut.HubspotService.Test.Attributes
{
    [TraitDiscoverer("SoftwareHut.HubspotService.Test.Attributes.UnitTestsDiscoverer",
        "SoftwareHut.HubspotService.Test")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class UnitTestsAttribute : Attribute, ITraitAttribute
    {
    }

    public class UnitTestsDiscoverer : ITraitDiscoverer
    {
        public const string KEY = "Category";
        public const string Category = "UnitTests";

        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            yield return new KeyValuePair<string, string>(KEY, Category);
        }
    }
}