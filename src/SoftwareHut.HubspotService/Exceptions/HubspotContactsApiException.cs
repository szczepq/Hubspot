using System;
using System.Net;

namespace SoftwareHut.HubspotService.Exceptions
{
    public class HubspotContactsApiException : Exception
    {
        public HubspotContactsApiException(HttpStatusCode statusCode, string message)
            :base($"HubspotContacts request failed with statusCode: {statusCode}" +
                  $" with message: {message}")
        {
        }
    }
}