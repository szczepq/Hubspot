using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WireMock.Server;
using Xunit;

namespace SoftwareHut.HubspotService.Test.IntegrationTests
{
    public class BootstrappedTestFixture : WebApplicationFactory<Startup>, IAsyncLifetime
    {
        public HttpClient TestClient { get; }
        public WireMockServer WireMockServer { get; }
        public HubspotDbContext HubspotDbContext { get; }

        private string ConnectionString { get; }
        public string Hapikey = "demo";
        
        public BootstrappedTestFixture()
        {
            WireMockServer = WireMockServer.Start();
            Environment.SetEnvironmentVariable("Hubspot__baseUrl", $"http://localhost:{WireMockServer.Ports.First()}");
            Environment.SetEnvironmentVariable("Hubspot__hapikey", Hapikey);

            ConnectionString = "Server=127.0.0.1,1401;" +
                               $"Database=hubspot_{Guid.NewGuid():N};" +
                               "User Id=SA;" +
                               "Password=YourSTRONG!Passw0rd;" +
                               "MultipleActiveResultSets=True";

            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);

            WithWebHostBuilder(b =>
            {
                b.UseConfiguration(InitConfiguration())
                    .UseStartup<Startup>();
            });

            HubspotDbContext = InitializeDbContext();
            TestClient = CreateClient();
        }

        public HubspotDbContext InitializeDbContext()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkSqlServer()
                .BuildServiceProvider();

            var builder = new DbContextOptionsBuilder<HubspotDbContext>();

            builder.UseSqlServer(ConnectionString)
                .UseInternalServiceProvider(serviceProvider);

            var context = new HubspotDbContext(builder.Options);
            context.Database.Migrate();
            return context;
        }

        public Task InitializeAsync() => Task.CompletedTask;
        public Task DisposeAsync() => HubspotDbContext.Database.EnsureDeletedAsync();

        public T GetService<T>() => Services.GetRequiredService<T>();

        private static IConfiguration InitConfiguration() =>
            new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
    }
}