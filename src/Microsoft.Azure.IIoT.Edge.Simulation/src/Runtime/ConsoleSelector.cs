// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Management.Runtime {
    using System;
    using System.Collections.Generic;

    public class ConsoleSelector : IConfigSelector {

        /// <inheritdoc/>
        public string SelectRegion(
            IEnumerable<string> regions) {
            Console.WriteLine("Select region (or X for default):");
            return ConsoleEx.Select(regions, "D");
        }

        /// <inheritdoc/>
        public string SelectSubscription(
            IEnumerable<string> subscriptions) {
            Console.WriteLine("Select subscription (or D for default):");
            return ConsoleEx.Select(subscriptions, "D");
        }
    }
}