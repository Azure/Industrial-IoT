// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Runtime {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Composite subcription info
    /// </summary>
    public class CompositeInfo : ISubscriptionInfo {

        /// <summary>
        /// Create composite info
        /// </summary>
        /// <param name="main"></param>
        /// <param name="fallback"></param>
        internal CompositeInfo(ISubscriptionInfo main,
            ISubscriptionInfo fallback) {
            _main = main ?? throw new ArgumentNullException(nameof(main));
            _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        }

        /// <inheritdoc/>
        public Task<string> GetEnvironment() => _main.GetEnvironment()
            .FallbackWhen(string.IsNullOrEmpty, () => _fallback.GetEnvironment());

        /// <inheritdoc/>
        public Task<string> GetSubscriptionId() => _main.GetSubscriptionId()
            .FallbackWhen(string.IsNullOrEmpty, () => _fallback.GetSubscriptionId());

        /// <inheritdoc/>
        public Task<string> GetRegionAsync() => _main.GetRegionAsync()
            .FallbackWhen(string.IsNullOrEmpty, () => _fallback.GetRegionAsync());

        /// <summary>
        /// Safe create
        /// </summary>
        /// <param name="main"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static ISubscriptionInfo Create(ISubscriptionInfo main,
            ISubscriptionInfo fallback) {
            if (main == null) {
                return fallback ?? new SubscriptionInfo();
            }
            if (fallback == null) {
                return main ?? new SubscriptionInfo();
            }
            return new CompositeInfo(main, fallback);
        }

        private readonly ISubscriptionInfo _main;
        private readonly ISubscriptionInfo _fallback;
    }

}
