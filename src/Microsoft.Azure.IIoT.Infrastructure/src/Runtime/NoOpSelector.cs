// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Runtime {
    using System.Collections.Generic;

    public class NoOpSelector : ISubscriptionInfoSelector {

        /// <inheritdoc/>
        public string SelectEnvironment(
            IEnumerable<string> environments) => null;

        /// <inheritdoc/>
        public string SelectSubscription(
            IEnumerable<string> subscriptions) => null;

        /// <inheritdoc/>
        public string SelectRegion(
            IEnumerable<string> regions) => null;
    }
}
