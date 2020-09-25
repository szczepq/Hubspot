using Polly;
using Polly.Caching;
using System;
using System.Threading.Tasks;

namespace SoftwareHut.HubspotService.Policies
{
    public interface ICachePolicy<T>
    {
        Task<T> ExecuteAsync(Func<Context, Task<T>> action, Context context);
    }

    public abstract class CachePolicy<T> : ICachePolicy<T>
    {
        public IAsyncCacheProvider AsyncCacheProvider { get; }
        private readonly AsyncCachePolicy<T> _policyInternal;

        public CachePolicy(IAsyncCacheProvider asyncCacheProvider, TimeSpan validity)
        {
            AsyncCacheProvider = asyncCacheProvider ?? throw new ArgumentNullException(nameof(asyncCacheProvider));
            _policyInternal = Policy.CacheAsync<T>(
                asyncCacheProvider,
                validity);
        }

        public Task<T> ExecuteAsync(Func<Context, Task<T>> action, Context context)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (context == null) throw new ArgumentNullException(nameof(context));

            return _policyInternal.ExecuteAsync(action, context);
        }
    }
}