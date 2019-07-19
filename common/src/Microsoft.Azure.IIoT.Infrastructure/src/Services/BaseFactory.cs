// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Services {
    using Serilog;
    using Microsoft.Azure.IIoT.Infrastructure.Auth;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Base factory
    /// </summary>
    public abstract class BaseFactory {

        /// <summary>
        /// Create virtual machine factory
        /// </summary>
        /// <param name="creds"></param>
        /// <param name="logger"></param>
        protected BaseFactory(ICredentialProvider creds, ILogger logger) {
            _creds = creds ??
                throw new ArgumentNullException(nameof(creds));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Helper to create new client
        /// </summary>
        /// <returns></returns>
        protected async Task<IAzure> CreateClientAsync(
            IResourceGroupResource resourceGroup) {
            if (resourceGroup == null) {
                throw new ArgumentNullException(nameof(resourceGroup));
            }
            var environment =
                await resourceGroup.Subscription.GetAzureEnvironmentAsync();
            var subscriptionId =
                await resourceGroup.Subscription.GetSubscriptionId();
            var credentials =
                await _creds.GetAzureCredentialsAsync(environment);
            return Azure
                .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithSubscription(subscriptionId);
        }

        /// <summary>Credentials to be used by derived classes</summary>
        protected readonly ICredentialProvider _creds;
        /// <summary>Logger to be used by derived classes</summary>
        protected readonly ILogger _logger;
    }
}
