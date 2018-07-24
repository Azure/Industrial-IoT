// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Services {
    using Microsoft.Azure.IIoT.Infrastructure.Auth;
    using Microsoft.Azure.IIoT.Infrastructure.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Read subscription information from user other selector.
    /// </summary>
    public class AzureSubscription : ISubscriptionInfoProvider  {

        /// <summary>
        /// Create subscription service
        /// </summary>
        /// <param name="creds"></param>
        /// <param name="logger"></param>
        public AzureSubscription(ICredentialProvider creds,
            ILogger logger) : this (creds, new NoOpSelector(), logger) {
        }

        /// <summary>
        /// Create subscription service
        /// </summary>
        /// <param name="creds"></param>
        /// <param name="selector"></param>
        /// <param name="logger"></param>
        public AzureSubscription(ICredentialProvider creds,
            ISubscriptionInfoSelector selector, ILogger logger) {
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));
            _creds = creds ?? throw new ArgumentNullException(nameof(creds));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public ISubscriptionInfo GetSubscriptionInfo() {
            var environment = new Lazy<string>(SelectEnvironment, true);
            var subscription = new Lazy<Task<ISubscription>>(
                () => SelectSubscriptionAsync(environment.Value), true);
            var region = new Lazy<Task<string>>(
                () => SelectRegion(subscription.Value), true);
            var info = new SubscriptionInformation(environment,
                subscription, region);
            return info;
        }

        /// <summary>
        /// Select region
        /// </summary>
        /// <param name="subscriptionTask"></param>
        /// <returns></returns>
        private async Task<string> SelectRegion(Task<ISubscription> subscriptionTask) {
            var subscription = await subscriptionTask;
            var regions = subscription.ListLocations()
                .Select(l => l.Region).Distinct();
            var region = _selector.SelectRegion(regions.Select(r => r.Name)) ??
                regions.FirstOrDefault()?.Name;
            return region;
        }

        /// <summary>
        /// Get subscription
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        private async Task<ISubscription> SelectSubscriptionAsync(string environment) {
            var credentials = await _creds.GetAzureCredentialsAsync(
                AzureEnvironmentEx.FromName(environment));
            var authenticated = Azure
                .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials);
            var subscriptions = await authenticated.Subscriptions.ListAsync();
            var name = _selector.SelectSubscription(
                subscriptions.Select(s => s.DisplayName));
            ISubscription subscription = null;
            if (name != null) {
                subscription = subscriptions.First(s =>
                    s.DisplayName == name);
            }
            if (subscription == null) {
                subscription = subscriptions.First(s =>
                    s.State.AnyOfIgnoreCase("Enabled", "Warned"));
            }
            return subscription;
        }

        /// <summary>
        /// Select environment
        /// </summary>
        /// <returns></returns>
        private string SelectEnvironment() {
            var environment = _selector.SelectEnvironment(
                AzureEnvironment.KnownEnvironments.Select(e => e.Name));
            if (string.IsNullOrEmpty(environment)) {
                return AzureEnvironment.AzureGlobalCloud.Name;
            }
            return environment;
        }

        /// <summary>
        /// Subscription info implementation
        /// </summary>
        private class SubscriptionInformation : ISubscriptionInfo {

            /// <summary>
            /// Create info
            /// </summary>
            /// <param name="environment"></param>
            /// <param name="subscription"></param>
            /// <param name="region"></param>
            public SubscriptionInformation(Lazy<string> environment,
                Lazy<Task<ISubscription>> subscription,
                Lazy<Task<string>> region) {
                _environment = environment;
                _subscription = subscription;
                _region = region;
            }

            /// <inheritdoc/>
            public Task<string> GetEnvironment() =>
                Task.FromResult(_environment.Value);

            /// <inheritdoc/>
            public async Task<string> GetSubscriptionId() {
                var sub = await _subscription.Value;
                return sub.SubscriptionId;
            }

            /// <inheritdoc/>
            public Task<string> GetRegion() => _region.Value;

            private Lazy<string> _environment;
            private Lazy<Task<ISubscription>> _subscription;
            private Lazy<Task<string>> _region;
        }

        private readonly ISubscriptionInfoSelector _selector;
        private readonly ICredentialProvider _creds;
        private readonly ILogger _logger;
    }
}
