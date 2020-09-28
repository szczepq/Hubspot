using AutoFixture.Idioms;
using AutoFixture.Xunit2;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using SoftwareHut.HubspotService.Controllers;
using SoftwareHut.HubspotService.Models;
using SoftwareHut.HubspotService.Test.Attributes;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace SoftwareHut.HubspotService.Test.Controllers
{
    [UnitTests]
    public class HubspotControllerTests
    {
        [Theory, GreedyControllerConstructor]
        public void SutHasGuardClauses(GuardClauseAssertion guardClauseAssertion)
        {
            guardClauseAssertion.Verify(typeof(HubspotController).GetConstructors());
            guardClauseAssertion.Verify(typeof(HubspotController)
                .GetMethods(BindingFlags.DeclaredOnly));
        }

        [Theory, AutoFakeData]
        public async Task GetContactsAsync_Ok(
            [Greedy] HubspotController sut,
            int count,
            ContactsList response)
        {
            A.CallTo(() => sut.HubspotService.GetHubspotContactsAsync(count))
                .Returns(response);

            var result = await sut.GetContactsAsync(count);

            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            var getContactsResponseResult = Assert.IsType<ContactsList>(okObjectResult.Value);
            getContactsResponseResult.Should().BeEquivalentTo(response);
            A.CallTo(() => sut.HubspotService.GetHubspotContactsAsync(count))
                .MustHaveHappened();
        }

        [Theory, AutoFakeData]
        public async Task CreateContactAsync_Ok(
            [Greedy] HubspotController sut,
            CreateContact request,
            int response)
        {
            A.CallTo(() => sut.HubspotService.CreateHubspotContactAsync(request))
                .Returns(response);

            var result = await sut.CreateContactAsync(request);

            Assert.IsType<OkResult>(result);
            A.CallTo(() => sut.HubspotService.CreateHubspotContactAsync(request))
                .MustHaveHappened();
        }
    }
}