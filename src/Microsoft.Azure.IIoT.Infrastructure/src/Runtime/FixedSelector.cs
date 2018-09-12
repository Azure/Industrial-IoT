// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Runtime {
    using System.Collections.Generic;

    /// <summary>
    /// Select from configuration
    /// </summary>
    public class FixedSelector : ISubscriptionInfoSelector {

        /// <summary>
        /// Create selector
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="subscription"></param>
        /// <param name="region"></param>
        public FixedSelector(string environment = null,
            string subscription = null, string region = null) {
            _environment = environment;
            _subscription = subscription;
            _region = region;
        }

        /// <inheritdoc/>
        public string SelectEnvironment(
            IEnumerable<string> environments) => _environment;

        /// <inheritdoc/>
        public string SelectSubscription(
            IEnumerable<string> subscriptions) => _subscription;

        /// <inheritdoc/>
        public string SelectRegion(
            IEnumerable<string> regions) => _region;

        private readonly string _environment;
        private readonly string _subscription;
        private readonly string _region;
    }
}
