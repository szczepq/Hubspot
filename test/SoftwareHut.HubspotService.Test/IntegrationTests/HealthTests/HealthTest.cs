using System;
using System.Net;
using System.Threading.Tasks;
using SoftwareHut.HubspotService.Test.Attributes;
using Xunit;

namespace SoftwareHut.HubspotService.Test.IntegrationTests.HealthTests
{
    [Collection(BootstrappedTestCollection.CollectionName)]
    [IntegrationTests]
    public class HealthTest
    {
        public BootstrappedTestFixture BootstrappedTestFixture { get; }

        public HealthTest(BootstrappedTestFixture bootstrappedTestFixture)
        {
            BootstrappedTestFixture = bootstrappedTestFixture ??
                                      throw new ArgumentNullException(nameof(bootstrappedTestFixture));
        }

        [Fact]
        public async Task Get_Health_ShouldReturn200Ok()
        {
            var response = await BootstrappedTestFixture.TestClient.GetAsync("health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
