using Newtonsoft.Json;
using Quibble.Xunit;
using SoftwareHut.HubspotService.Models;
using System.Collections.Generic;
using SoftwareHut.HubspotService.Test.Attributes;
using Xunit;

namespace SoftwareHut.HubspotService.Test.Deserialize
{
    [UnitTests]
    public class CreateHubspotContactTests
    {
        [Fact]
        public void CreateHubspotContact_ShouldBeSerialized()
        {
            var expected = @"
            {
              ""properties"": [
                        {
                            ""property"": ""email"",
                            ""value"": ""testingapis@hubspot.com""
                        },
                        {
                            ""property"": ""firstname"",
                            ""value"": ""Adrian""
                        }
                    ]
                }
            ";

            var newContact = new CreateHubspotContact(
                new List<CreateContactProperty>
                {
                    new CreateContactProperty("email", "testingapis@hubspot.com"),
                    new CreateContactProperty("firstname", "Adrian"),
                });
            var json = JsonConvert.SerializeObject(newContact);
            
            JsonAssert.Equal(expected, json);
        }
    }
}