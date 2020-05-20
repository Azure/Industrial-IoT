// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.Configuration {

    /// <summary>
    /// Determines where in the configuration providers chain current provider should
    /// be added.
    /// </summary>
    public enum ConfigurationProviderPriority {

        /// <summary>
        /// Configuration provider should be added at the end of providers list,
        /// thus having highest priority with all values overriding other providers.
        /// </summary>
        Highest,

        /// <summary>
        /// Configuratoin provider should be added at the beginning of providers list,
        /// thus having lowest priority with all values being potentially overridden.
        /// </summary>
        Lowest
    }
}
