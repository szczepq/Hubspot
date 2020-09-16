using System;
using Newtonsoft.Json;

namespace SoftwareHut.HubspotService.Models
{
    public class CreateContact
    {
        [JsonProperty("firstName")]
        public string FirstName { get; private set; }

        [JsonProperty("email")] 
        public string Email { get; private set; }

        [JsonConstructor]
        private CreateContact() { }

        public CreateContact(string firstName, string email)
        {
            FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
            Email = email ?? throw new ArgumentNullException(nameof(email));
        }
    }
}