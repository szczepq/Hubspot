using System;
using SoftwareHut.HubspotService.Models;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareHut.HubspotService.Mappers
{
    public interface IHubspotMapper
    {
        ContactsList FromHubspotContacts(HubspotContacts hubspotContacts);
        Contact FromHubspotContact(HubspotContact hubspotContact);
        CreateHubspotContact ToCreateHubspotContact(CreateContact contact);
    }

    public class HubspotMapper : IHubspotMapper
    {
        public ContactsList FromHubspotContacts(HubspotContacts hubspotContacts)
        {
            if (hubspotContacts == null) throw new ArgumentNullException(nameof(hubspotContacts));

            return new ContactsList(
                hubspotContacts.HubspotContact
                    .Select(FromHubspotContact)
                    .ToList());
        }

        public Contact FromHubspotContact(HubspotContact hubspotContact)
        {
            if (hubspotContact == null) throw new ArgumentNullException(nameof(hubspotContact));

            return new Contact(
                hubspotContact.Id,
                hubspotContact.Profiles.First()
                    .Identity.FirstOrDefault(y => y.Type == "EMAIL")?.Value);
        }

        public CreateHubspotContact ToCreateHubspotContact(CreateContact contact)
        {
            if (contact == null) throw new ArgumentNullException(nameof(contact));

            return new CreateHubspotContact(new List<CreateContactProperty>
            {
                new CreateContactProperty("email", contact.Email),
                new CreateContactProperty("firstname", contact.FirstName)
            });
        }
    }
}
