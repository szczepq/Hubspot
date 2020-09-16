using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SoftwareHut.HubspotService.Models
{
    public class CreateHubspotContact
    {
        [JsonProperty("properties")]
        public List<CreateContactProperty> Properties { get;  }

        public CreateHubspotContact(List<CreateContactProperty> properties)
        {
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }
    }

    public class CreateContactProperty
    {
        [JsonProperty("property")]
        public string Property { get; }

        [JsonProperty("value")]
        public string Value { get; }

        public CreateContactProperty(string property, string value)
        {
            Property = property ?? throw new ArgumentNullException(nameof(property));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}