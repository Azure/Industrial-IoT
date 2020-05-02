// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth {

    /// <summary>
    /// Enabled providers
    /// </summary>
    public class Provider {

        /// <summary>
        /// Create provider
        /// </summary>
        /// <param name="name"></param>
        /// <param name="scheme"></param>
        public Provider(string name, string scheme) {
            Name = name;
            Scheme = scheme;
        }

        /// <summary>
        /// Name of provider
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Scheme provider is enabled for
        /// </summary>
        public string Scheme { get; }
    }
}