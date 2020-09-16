using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SoftwareHut.HubspotService.Models
{
    public class ContactsList
    {
        [JsonProperty("contacts")]
        public List<Contact> Contacts { get; }

        public ContactsList(List<Contact> contacts)
        {
            Contacts = contacts ?? throw new ArgumentNullException(nameof(contacts));
        }
    }

    public class Contact
    {
        [JsonProperty("externalId")]
        public int ExternalId { get; }

        [JsonProperty("email")]
        public string Email { get;  }

        public Contact(int externalId, string email)
        {
            ExternalId = externalId;
            Email = email ?? throw new ArgumentNullException(nameof(email));
        }
    }
}