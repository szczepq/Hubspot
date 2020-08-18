using Xunit;

namespace SoftwareHut.HubspotService.Test.IntegrationTests.HealthTests
{
    [CollectionDefinition(CollectionName)]
    public class BootstrappedTestCollection : ICollectionFixture<BootstrappedTestFixture>
    {
        public const string CollectionName = "Bootstrapped tests";
    }
}