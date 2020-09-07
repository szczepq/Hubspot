using FluentAssertions;
using Newtonsoft.Json;
using SoftwareHut.HubspotService.Models;
using System.Collections.Generic;
using Xunit;

namespace SoftwareHut.HubspotService.Test.Deserialize
{
    public class HubspotContactsTests
    {
        [Fact]
        public void HubspotContacts_ShouldBeDeserialize()
        {
            var sampleJson = @"
            {
              'contacts': [
                {
                  'vid': 204727,
                  'identity-profiles': [
                    {
                      'identities': [
                        {
                          'type': 'EMAIL',
                          'value': 'mgnew-email@hubspot.com'
                        }]}]},
                {
                  'vid': 207303,
                  'identity-profiles': [
                    {
                      'identities': [
                        {
                          'type': 'EMAIL',
                          'value': 'email_0be34aebe5@abctest.com',
                        }]}]}
                ]}
            ";

            var expected = new HubspotContacts(
                new List<HubspotContact>
                {
                    new HubspotContact(
                        204727,
                        new List<HubspotProfile>
                        {
                            new HubspotProfile(
                                new List<HubspotIdentity>
                                {
                                    new HubspotIdentity("EMAIL", "mgnew-email@hubspot.com")
                                })
                        }),
                    new HubspotContact(
                        207303,
                        new List<HubspotProfile>
                        {
                            new HubspotProfile(
                                new List<HubspotIdentity>
                                {
                                    new HubspotIdentity("EMAIL", "email_0be34aebe5@abctest.com")
                                })
                        })
                }
            );


            var response = JsonConvert.DeserializeObject<HubspotContacts>(sampleJson);
            Assert.NotNull(response);
            response.Should().BeEquivalentTo(expected);
        }
    }
}