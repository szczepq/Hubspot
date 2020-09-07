using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SoftwareHut.HubspotService.Models
{
    public class HubspotContacts
    {
        [JsonProperty("contacts")] 
        public List<HubspotContact> HubspotContact { get; private set; }

        [JsonConstructor]
        private HubspotContacts() { }

        public HubspotContacts(List<HubspotContact> hubspotContact)
        {
            HubspotContact = hubspotContact ?? throw new ArgumentNullException(nameof(hubspotContact));
        }
    }

    public class HubspotContact
    {
        [JsonProperty("vid")] 
        public int Id { get; private set; }

        [JsonProperty("identity-profiles")] 
        public List<HubspotProfile> Profiles { get; private set; }

        [JsonConstructor]
        private HubspotContact() { }

        public HubspotContact(int id, List<HubspotProfile> profiles)
        {
            Id = id;
            Profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        }
    }

    public class HubspotProfile
    {
        [JsonProperty("identities")] 
        public List<HubspotIdentity> Identity { get; private set; }

        [JsonConstructor]
        private HubspotProfile() { }

        public HubspotProfile(List<HubspotIdentity> identity)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }
    }

    public class HubspotIdentity
    {
        [JsonProperty("type")] 
        public string Type { get; private set; }

        [JsonProperty("value")] 
        public string Value { get; private set; }

        [JsonConstructor]
        private HubspotIdentity() { }

        public HubspotIdentity(string type, string value)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}