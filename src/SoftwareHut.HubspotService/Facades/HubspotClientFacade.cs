using Polly;
using SoftwareHut.HubspotService.Clients;
using SoftwareHut.HubspotService.Configurations;
using SoftwareHut.HubspotService.Models;
using SoftwareHut.HubspotService.Policies;
using System;
using System.Threading.Tasks;
using Refit;
using SoftwareHut.HubspotService.Exceptions;

namespace SoftwareHut.HubspotService.Facades
{
    public interface IHubspotClientFacade
    {
        Task<HubspotContacts> GetContactsAsync(int count);
        Task<HubspotContact> CreateContactsAsync(CreateHubspotContact contacts);
    }

    public class HubspotClientFacade : IHubspotClientFacade
    {
        public IHubspotClient HubspotClient { get; }
        public IHubspotConfiguration HubspotConfiguration { get; }
        public IHubspotContactsCachePolicy HubspotContactsCachePolicy { get; }
        public IRetryPolicy RetryPolicy { get; }

        public HubspotClientFacade(
            IHubspotClient hubspotClient,
            IHubspotConfiguration hubspotConfiguration,
            IHubspotContactsCachePolicy hubspotContactsCachePolicy,
            IRetryPolicy retryPolicy)
        {
            HubspotClient = 
                hubspotClient ?? throw new ArgumentNullException(nameof(hubspotClient));
            HubspotConfiguration =
                hubspotConfiguration ?? throw new ArgumentNullException(nameof(hubspotConfiguration));
            HubspotContactsCachePolicy = 
                hubspotContactsCachePolicy ?? throw new ArgumentNullException(nameof(hubspotContactsCachePolicy));
            RetryPolicy = 
                retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        }

        public Task<HubspotContacts> GetContactsAsync(int count)
        {
            return HubspotContactsCachePolicy.ExecuteAsync(
                context => GetContactsInternalAsync(count),
                new Context($"HubspotContacts_{count}"));
        }

        public Task<HubspotContacts> GetContactsInternalAsync(int count)
        {
            return ExecuteApiCallAsync(() => HubspotClient.GetContactsAsync(HubspotConfiguration.HapiKey, count));
        }

        public Task<HubspotContact> CreateContactsAsync(
            CreateHubspotContact contacts)
        {
            if(contacts == null) throw new ArgumentNullException(nameof(contacts));
            return ExecuteApiCallAsync(() => HubspotClient.CreateContactsAsync(HubspotConfiguration.HapiKey, contacts));
        }

        public Task<T> ExecuteApiCallAsync<T>(Func<Task<T>> action)
        {
            if(action == null) throw new ArgumentNullException(nameof(action));
            return ExecuteApiCallInternalAsync(action);
        }

        private async Task<T> ExecuteApiCallInternalAsync<T>(Func<Task<T>> action)
        {
            try
            {
                return await RetryPolicy.ExecuteAsync(action);
            }
            catch (ApiException ae)
            {
                throw new HubspotContactsApiException(ae.StatusCode, ae.Message);
            }
        }
    }
}