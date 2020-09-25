using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SoftwareHut.HubspotService.Test.IntegrationTests
{
    public class BootstrappedTestFixture : WebApplicationFactory<Startup>, IAsyncLifetime
    {
        public HttpClient TestClient { get; }

        public BootstrappedTestFixture()
        {
            TestClient = CreateClient();
        }

        public Task InitializeAsync() => Task.CompletedTask;
        public Task DisposeAsync() => Task.CompletedTask;

        public T GetService<T>() => Services.GetRequiredService<T>();
    }
}