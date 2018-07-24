// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.Management.ResourceManager.Fluent {
    using System;

    public static class AzureEnvironmentEx {

        /// <summary>
        /// Convert from string to environment
        /// </summary>
        public static AzureEnvironment FromName(string name) {
            if (string.IsNullOrEmpty(name)) {
                return AzureEnvironment.AzureGlobalCloud;
            }
            var env = AzureEnvironment.FromName(name);
            if (env == null) {
                throw new ArgumentException(nameof(name));
            }
            return env;
        }
    }
}
