using FakeItEasy;
using Polly;
using SoftwareHut.HubspotService.Facades;
using SoftwareHut.HubspotService.Models;
using SoftwareHut.HubspotService.Test.Attributes;
using SoftwareHut.HubspotService.Test.Deserialize;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Refit;
using SoftwareHut.HubspotService.Exceptions;
using Xunit;

namespace SoftwareHut.HubspotService.Test.Facades
{
    [UnitTests]
    public class HubspotClientFacadeTests : BaseAssertion<HubspotClientFacade>
    {
        [Theory, AutoFakeData]
        public async Task GetContactsAsync_Ok(
            HubspotClientFacade sut,
            HubspotContacts hubspotContacts,
            int count)
        {
            A.CallTo(() =>
                    sut.HubspotContactsCachePolicy.ExecuteAsync(A<Func<Context, Task<HubspotContacts>>>._,
                        A<Context>._))
                .Returns(hubspotContacts);

            await sut.GetContactsAsync(count);

            A.CallTo(() =>
                    sut.HubspotContactsCachePolicy.ExecuteAsync(A<Func<Context, Task<HubspotContacts>>>._,
                        A<Context>._))
                .MustHaveHappenedOnceExactly();
        }

        [Theory, AutoFakeData]
        public async Task GetContactsInternalAsync_Ok(
            HubspotClientFacade sut,
            HubspotContacts hubspotContacts,
            int count)
        {
            A.CallTo(() => sut.RetryPolicy.ExecuteAsync(A<Func<Task<HubspotContacts>>>._))
                .Returns(hubspotContacts);

            await sut.GetContactsInternalAsync(count);

            A.CallTo(() => sut.RetryPolicy.ExecuteAsync(A<Func<Task<HubspotContacts>>>._))
                .MustHaveHappenedOnceExactly();
        }

        [Theory, AutoFakeData]
        public async Task CreateContactsAsync_Ok(
            HubspotClientFacade sut,
            CreateHubspotContact contacts,
            HubspotContact response)
        {
            A.CallTo(() => sut.RetryPolicy.ExecuteAsync(A<Func<Task<HubspotContact>>>._))
                .Returns(response);

            await sut.CreateContactsAsync(contacts);

            A.CallTo(() => sut.RetryPolicy.ExecuteAsync(A<Func<Task<HubspotContact>>>._))
                .MustHaveHappenedOnceExactly();
        }

        [Theory, AutoFakeData]
        public async Task ExecuteApiCallAsync_ThrowsHubspotContactsApiException(
            HubspotClientFacade sut,
            HttpRequestMessage httpRequestMessage,
            HttpResponseMessage httpResponseMessage,
            RefitSettings refitSettings,
            Func<Task<int>> action
        )
        {
            A.CallTo(() => sut.RetryPolicy.ExecuteAsync(A<Func<Task<int>>>._))
                .Throws(await ApiException.Create(httpRequestMessage, HttpMethod.Get, httpResponseMessage,
                    refitSettings));

            await Assert.ThrowsAsync<HubspotContactsApiException>(async () => await sut.ExecuteApiCallAsync(action));
        }
    }
}