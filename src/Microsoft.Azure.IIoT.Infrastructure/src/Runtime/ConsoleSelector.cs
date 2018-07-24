// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Runtime {
    using System;
    using System.Collections.Generic;

    public class ConsoleSelector : ISubscriptionInfoSelector {

        /// <inheritdoc/>
        public string SelectEnvironment(
            IEnumerable<string> environments) {
            Console.WriteLine("Select cloud (or enter for default):");
            return ConsoleEx.Select(environments);
        }

        /// <inheritdoc/>
        public string SelectRegion(
            IEnumerable<string> regions) {
            Console.WriteLine("Select region (or enter for default):");
            return ConsoleEx.Select(regions);
        }

        /// <inheritdoc/>
        public string SelectSubscription(
            IEnumerable<string> subscriptions) {
            Console.WriteLine("Select subscription (or enter for default):");
            return ConsoleEx.Select(subscriptions);
        }
    }
}
