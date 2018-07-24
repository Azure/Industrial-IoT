// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Runtime {
    using System.Threading.Tasks;

    public class SubscriptionInfo : ISubscriptionInfo {

        /// <summary>
        /// Create subscription info
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="region"></param>
        internal SubscriptionInfo(Task<string> environment,
            Task<string> subscriptionId, Task<string> region) {
            _environment = environment;
            _subscriptionId = subscriptionId;
            _region = region;
        }

        /// <summary>
        /// Create subscription info
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="region"></param>
        public SubscriptionInfo(string environment, string subscriptionId,
            string region) :
            this (Task.FromResult(environment), Task.FromResult(subscriptionId),
                Task.FromResult(region)) {
        }

        /// <summary>
        /// Create empty info
        /// </summary>
        internal SubscriptionInfo() :
            this ((string)null, null, null) {
        }

        /// <inheritdoc/>
        public Task<string> GetEnvironment() => _environment;

        /// <inheritdoc/>
        public Task<string> GetSubscriptionId() => _subscriptionId;

        /// <inheritdoc/>
        public Task<string> GetRegion() => _region;

        private readonly Task<string> _region;
        private readonly Task<string> _environment;
        private readonly Task<string> _subscriptionId;
    }

}
