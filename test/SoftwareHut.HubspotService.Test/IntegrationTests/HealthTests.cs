using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace SoftwareHut.HubspotService.Test.IntegrationTests
{
    [Collection(BootstrappedTestCollection.CollectionName)]
    public class HealthTests
    {
        public BootstrappedTestFixture BootstrappedTestFixture { get; }

        public HealthTests(BootstrappedTestFixture bootstrappedTestFixture)
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
