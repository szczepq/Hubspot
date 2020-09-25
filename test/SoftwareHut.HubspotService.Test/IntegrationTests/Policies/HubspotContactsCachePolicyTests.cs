using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Caching;
using SoftwareHut.HubspotService.Configurations;
using SoftwareHut.HubspotService.Models;
using SoftwareHut.HubspotService.Policies;
using SoftwareHut.HubspotService.Test.Attributes;
using SoftwareHut.HubspotService.Test.Deserialize;
using Xunit;

namespace SoftwareHut.HubspotService.Test.IntegrationTests.Policies
{
    [IntegrationTests]
    [Collection(BootstrappedTestCollection.CollectionName)]
    public class HubspotContactsCachePolicyTests : BaseAssertion<HubspotContactsCachePolicy>
    {
        public BootstrappedTestFixture BootstrappedTestFixture { get; }

        public HubspotContactsCachePolicyTests(BootstrappedTestFixture bootstrappedTestFixture)
        {
            BootstrappedTestFixture = bootstrappedTestFixture;
        }

        [Theory, AutoFakeData]
        public async Task ExecuteAsync_PositiveTtl(
            int count,
            CachePolicyConfiguration cachingConfig,
            HubspotContacts hubspotContactsFirst,
            HubspotContacts hubspotContactsSecond,
            Func<int, Task<HubspotContacts>> func)
        {
            const int ttl = 1;
            cachingConfig.CacheDurationSec = ttl;
            var asyncCacheProvider = BootstrappedTestFixture.GetService<IAsyncCacheProvider>();
            A.CallTo(() => func(count)).Returns(hubspotContactsFirst).Once().Then
                .Returns(hubspotContactsSecond);

            var sut = new HubspotContactsCachePolicy(asyncCacheProvider, cachingConfig);
            var context = new Context($"{count}");

            var firstResult = await sut.ExecuteAsync(_ => func(count), context);
            var secondResult = await sut.ExecuteAsync(_ => func(count), context);
            await Task.Delay(TimeSpan.FromSeconds(ttl + 1));
            var thirdResult = await sut.ExecuteAsync(_ => func(count), context);

            firstResult.Should().BeEquivalentTo(hubspotContactsFirst);
            secondResult.Should().BeEquivalentTo(hubspotContactsFirst);
            thirdResult.Should().BeEquivalentTo(hubspotContactsSecond);

            A.CallTo(() => func(count)).MustHaveHappenedTwiceExactly();
        }

        [Theory, AutoFakeData]
        public async Task ExecuteAsync_ZeroTtl(
            int count,
            CachePolicyConfiguration cachingConfig,
            HubspotContacts hubspotContactsFirst,
            HubspotContacts hubspotContactsSecond,
            Func<int, Task<HubspotContacts>> func)
        {
            const int ttl = 0;
            cachingConfig.CacheDurationSec = ttl;
            var asyncCacheProvider = BootstrappedTestFixture.GetService<IAsyncCacheProvider>();
            A.CallTo(() => func(count)).Returns(hubspotContactsFirst).Once()
                .Then.Returns(hubspotContactsSecond);

            var sut = new HubspotContactsCachePolicy(asyncCacheProvider, cachingConfig);
            var context = new Context($"{count}");

            var firstResult = await sut.ExecuteAsync(_ => func(count), context);
            var secondResult = await sut.ExecuteAsync(_ => func(count), context);

            firstResult.Should().BeEquivalentTo(hubspotContactsFirst);
            secondResult.Should().BeEquivalentTo(hubspotContactsSecond);
            firstResult.Should().NotBeEquivalentTo(secondResult);

            A.CallTo(() => func(count)).MustHaveHappenedTwiceExactly();
        }


        [Theory, AutoFakeData]
        public async Task ExecuteAsync_DifferentListSize(
            int countFirst,
            int countSecond,
            CachePolicyConfiguration cachingConfig,
            HubspotContacts hubspotContactsFirst,
            HubspotContacts hubspotContactsSecond,
            Func<int, Task<HubspotContacts>> func)
        {
            const int ttl = 1;
            cachingConfig.CacheDurationSec = ttl;
            var asyncCacheProvider = BootstrappedTestFixture.GetService<IAsyncCacheProvider>();
            A.CallTo(() => func(countFirst)).Returns(hubspotContactsFirst);
            A.CallTo(() => func(countSecond)).Returns(hubspotContactsSecond);

            var sut = new HubspotContactsCachePolicy(asyncCacheProvider, cachingConfig);

            var firstResult =
                await sut.ExecuteAsync(_ => func(countFirst),
                    new Context($"{countFirst}"));
            var secondResult =
                await sut.ExecuteAsync(_ => func(countSecond),
                    new Context($"{countSecond}"));

            firstResult.Should().BeEquivalentTo(hubspotContactsFirst);
            secondResult.Should().BeEquivalentTo(hubspotContactsSecond);
            firstResult.Should().NotBeEquivalentTo(secondResult);

            A.CallTo(() => func(countFirst)).MustHaveHappenedOnceExactly();
            A.CallTo(() => func(countSecond)).MustHaveHappenedOnceExactly();
        }
    }
}
