// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Management.Runtime {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Management.Auth;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public class AzureManagement : IConfigProvider  {

        /// <summary>
        /// Create simulator
        /// </summary>
        /// <param name="creds"></param>
        /// <param name="callbacks"></param>
        /// <param name="logger"></param>
        public AzureManagement(ICredentialProvider creds,
            IConfigSelector callbacks, ILogger logger) {
            _creds = creds ?? throw new ArgumentNullException(nameof(creds));
            _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Helper that allows selecting subscription and location
        /// </summary>
        /// <returns></returns>
        public async Task<IManagementConfig> GetContextAsync() {
            var subscriptions = await Authenticated.Subscriptions.ListAsync();
            var name = _callbacks.SelectSubscription(
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
            var regions = subscription.ListLocations()
                .Select(l => l.Region).Distinct();
            var region = _callbacks.SelectRegion(regions.Select(r => r.Name)) ??
                regions.FirstOrDefault()?.Name;
            return new SubscriptionInfo(subscription.SubscriptionId, region);
        }

        private class SubscriptionInfo : IManagementConfig {

            /// <summary>
            /// Create context
            /// </summary>
            /// <param name="subscriptionId"></param>
            /// <param name="region"></param>
            public SubscriptionInfo(string subscriptionId, string region) {
                SubscriptionId = subscriptionId;
                Region = region;
            }

            /// <inheritdoc/>
            public string SubscriptionId { get; }

            /// <inheritdoc/>
            public string Region { get; }
        }

        /// <summary>
        /// Get a authenticated boot
        /// </summary>
        private Azure.IAuthenticated Authenticated => Azure
            .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
            .Authenticate(_creds.Credentials);

        private readonly IConfigSelector _callbacks;
        private readonly ICredentialProvider _creds;
        private readonly ILogger _logger;
    }
}