using System;

namespace SoftwareHut.HubspotService.Configurations
{
    public interface IRetryPolicyConfiguration
    {
        int MaxNumberOfRetries { get; }
        TimeSpan RetryBackoffPeriod { get; }
    }

    public class RetryPolicyConfiguration : IRetryPolicyConfiguration
    {
        public const string SectionName = "RetryPolicy";

        public int? NumberOfRetries { get; set; }
        public int MaxNumberOfRetries => NumberOfRetries ?? 3;

        public int? RetryBackoffPeriodMs { get; set; }
        public TimeSpan RetryBackoffPeriod => TimeSpan.FromMilliseconds(RetryBackoffPeriodMs ?? 1000);
    }
}