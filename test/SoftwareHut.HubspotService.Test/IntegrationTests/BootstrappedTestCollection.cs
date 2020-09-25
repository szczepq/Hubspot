using Xunit;

namespace SoftwareHut.HubspotService.Test.IntegrationTests
{
    [CollectionDefinition(CollectionName)]
    public class BootstrappedTestCollection : ICollectionFixture<BootstrappedTestFixture>
    {
        public const string CollectionName = "Bootstrapped tests";
    }
}