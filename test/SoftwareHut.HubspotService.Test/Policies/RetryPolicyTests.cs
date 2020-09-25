using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Refit;
using SoftwareHut.HubspotService.Configurations;
using SoftwareHut.HubspotService.Policies;
using SoftwareHut.HubspotService.Test.Attributes;
using SoftwareHut.HubspotService.Test.Deserialize;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace SoftwareHut.HubspotService.Test.Policies
{
    [UnitTests]
    public class RetryPolicyTests : BaseAssertion<RetryPolicy>
    {
        [Theory]
        [AutoFakeData(typeof(HttpResponseMessage), "statusCode", HttpStatusCode.TooManyRequests)]
        [AutoFakeData(typeof(HttpResponseMessage), "statusCode", HttpStatusCode.InternalServerError)]
        [AutoFakeData(typeof(HttpResponseMessage), "statusCode", HttpStatusCode.RequestTimeout)]
        public async Task Execute_Retries(
            IRetryPolicyConfiguration retryPolicyConfiguration,
            Func<Task<string>> action,
            ILogger<RetryPolicy> logger,
            HttpRequestMessage httpRequestMessage,
            HttpResponseMessage httpResponseMessage,
            RefitSettings refitSettings)
        {
            const int numberOfRetries = 3;
            A.CallTo(() => retryPolicyConfiguration.RetryBackoffPeriod).Returns(TimeSpan.FromMilliseconds(100));
            A.CallTo(() => retryPolicyConfiguration.MaxNumberOfRetries).Returns(numberOfRetries);

            httpResponseMessage.StatusCode = HttpStatusCode.TooManyRequests;

            var sut = new RetryPolicy(retryPolicyConfiguration, logger);

            A.CallTo(() => action())
                .Throws(await ApiException.Create(httpRequestMessage, HttpMethod.Get, httpResponseMessage,
                    refitSettings));

            await Assert.ThrowsAsync<ApiException>(async () => await sut.ExecuteAsync(action));

            A.CallTo(() => action())
                .MustHaveHappenedANumberOfTimesMatching(x => x == numberOfRetries + 1);
        }

        [Theory]
        [AutoFakeData(typeof(RetryPolicyConfiguration), "RetryBackoffPeriodMs", 500)]
        [AutoFakeData(typeof(RetryPolicyConfiguration), "RetryBackoffPeriodMs", 750)]
        [AutoFakeData(typeof(RetryPolicyConfiguration), "RetryBackoffPeriodMs", 1000)]
        public async Task Execute_RespectsBackoffPeriod(IRetryPolicyConfiguration retryPolicyConfiguration,
            ILogger<RetryPolicy> logger, Func<Task<string>> action, HttpRequestMessage httpRequestMessage,
            HttpResponseMessage httpResponseMessage, RefitSettings refitSettings)
        {
            const int faultToleranceMs = 25;
            const int numberOfRetries = 1;

            var firstCall = DateTime.Now;
            var secondCall = DateTime.Now;

            A.CallTo(() => retryPolicyConfiguration.MaxNumberOfRetries).Returns(numberOfRetries);

            httpResponseMessage.StatusCode = HttpStatusCode.RequestTimeout;

            var sut = new RetryPolicy(retryPolicyConfiguration, logger);

            var apiException =
                await ApiException.Create(httpRequestMessage, HttpMethod.Get, httpResponseMessage, refitSettings);

            A.CallTo(() => action())
                .Invokes(() => { firstCall = DateTime.Now; }).Throws(apiException).Once().Then
                .Invokes(() => { secondCall = DateTime.Now; }).Throws(apiException);

            await Assert.ThrowsAsync<ApiException>(async () => await sut.ExecuteAsync(action));
            firstCall.Should().BeCloseTo(
                secondCall.Subtract(retryPolicyConfiguration.RetryBackoffPeriod),
                faultToleranceMs);
        }
    }
}