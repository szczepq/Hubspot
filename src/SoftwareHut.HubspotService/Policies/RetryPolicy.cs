using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Refit;
using SoftwareHut.HubspotService.Configurations;

namespace SoftwareHut.HubspotService.Policies
{
    public interface IRetryPolicy
    {
        Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action);
    }
    public class RetryPolicy : IRetryPolicy
    {
        public IRetryPolicyConfiguration RetryPolicyConfiguration { get; }
        public ILogger<RetryPolicy> Logger { get; }
        private readonly AsyncPolicy _policyInternal;

        public RetryPolicy(
            IRetryPolicyConfiguration retryPolicyConfiguration, 
            ILogger<RetryPolicy> logger)
        {
            RetryPolicyConfiguration = retryPolicyConfiguration ?? throw new ArgumentNullException(nameof(retryPolicyConfiguration));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _policyInternal = Policy.Handle<ApiException>(response =>
                    response.StatusCode == HttpStatusCode.InternalServerError ||
                    response.StatusCode == HttpStatusCode.TooManyRequests ||
                    response.StatusCode == HttpStatusCode.RequestTimeout)
                .WaitAndRetryAsync(retryPolicyConfiguration.MaxNumberOfRetries, duration =>
                        retryPolicyConfiguration.RetryBackoffPeriod,
                    (exception, duration, retryCount, context) =>
                    {
                        Logger.LogWarning(exception,
                            $"Request failed with statusCode {((ApiException)exception).StatusCode}, " +
                            $"waiting {duration.Milliseconds} ms before retry. Retry attempt {retryCount}");
                    });
        }

        public Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action)
        {
            if(action == null)  throw new ArgumentNullException(nameof(action));

            return _policyInternal.ExecuteAsync(action);
        }
    }
}