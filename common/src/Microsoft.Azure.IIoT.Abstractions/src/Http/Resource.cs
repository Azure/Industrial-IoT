// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {

    /// <summary>
    /// Resources that can be targeted with service calls
    /// </summary>
    public static class Resource {

        /// <summary>
        /// No authentication required
        /// </summary>
        public static readonly string None = null;

        /// <summary>
        /// The resource is local to the client
        /// </summary>
        public static readonly string Local = nameof(Local) + ":";

        /// <summary>
        /// Platform
        /// </summary>
        public static readonly string Platform = nameof(Platform);

        /// <summary>
        /// Keyvault
        /// </summary>
        public static readonly string KeyVault = nameof(KeyVault);

        /// <summary>
        /// Storage
        /// </summary>
        public static readonly string Storage = nameof(Storage);
    }
}
