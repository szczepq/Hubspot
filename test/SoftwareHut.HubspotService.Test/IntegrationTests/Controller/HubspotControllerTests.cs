using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SoftwareHut.HubspotService.Models;
using SoftwareHut.HubspotService.Test.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace SoftwareHut.HubspotService.Test.IntegrationTests.Controller
{
    [IntegrationTests]
    [Collection(BootstrappedTestCollection.CollectionName)]
    public class HubspotControllerTests
    {
        public BootstrappedTestFixture BootstrappedTestFixture { get; }

        public string GetContactsPath = "/contacts/v1/lists/all/contacts/all";
        public string CreateContactPath = "/contacts/v1/contact";
        public string ApiPath = "api/hubspot";

        public HubspotControllerTests(
            BootstrappedTestFixture bootstrappedTestFixture)
        {
            BootstrappedTestFixture = bootstrappedTestFixture ??
                                      throw new ArgumentNullException(nameof(bootstrappedTestFixture));
        }

        [Theory, AutoFakeData]
        public async Task GetHubspotContactsAsync_Ok(
            int count,
            HubspotContacts hubspotContacts)
        {
            BootstrappedTestFixture.WireMockServer
                .Given(Request.Create()
                    .WithPath(GetContactsPath)
                    .WithParam("hapikey", BootstrappedTestFixture.Hapikey)
                    .WithParam("count", count.ToString())
                    .UsingGet())
                .RespondWith(
                    Response.Create().WithStatusCode(200)
                        .WithBody(JsonConvert.SerializeObject(hubspotContacts)));

            var response = await BootstrappedTestFixture.TestClient
                .GetAsync($"{ApiPath}?count={count}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseJsonString = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(responseJsonString);

            var exceptionResponse = JsonConvert.DeserializeObject<ContactsList>(responseJsonString);
            Assert.NotNull(exceptionResponse);
        }

        [Theory, AutoFakeData]
        public async Task CreateContactAsync_Ok(
            int externalId,
            CreateContact contact)
        {
            var hubspotContact = new HubspotContact(
                externalId,
                new List<HubspotProfile>
                {
                    new HubspotProfile(
                        new List<HubspotIdentity>
                        {
                            new HubspotIdentity("EMAIL", contact.Email)
                        })
                });

            BootstrappedTestFixture.WireMockServer
                .Given(Request.Create()
                    .WithPath(CreateContactPath)
                    .WithParam("hapikey", BootstrappedTestFixture.Hapikey)
                    .UsingPost())
                .RespondWith(
                    Response.Create().WithStatusCode(200)
                        .WithBody(JsonConvert.SerializeObject(hubspotContact)));

            var content = new StringContent(JsonConvert.SerializeObject(contact), Encoding.UTF8, "application/json");
            var response = await BootstrappedTestFixture.TestClient.PostAsync(ApiPath, content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var user = await BootstrappedTestFixture.HubspotDbContext.Users.Where(x => x.Email == contact.Email)
                .FirstOrDefaultAsync();

            Assert.NotNull(user);
            Assert.Equal(contact.Email, user.Email);
            Assert.Equal(externalId, user.ExternalId);
        }
    }
}