// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {

    /// <summary>
    /// Industrial IoT defined auth schemes
    /// </summary>
    public static class AuthScheme {

        /// <summary>
        /// Unknown
        /// </summary>
        public const string Unknown = "Bearer";

        /// <summary>
        /// Aad auth
        /// </summary>
        public const string Aad = nameof(Aad);

        /// <summary>
        /// User assigned identity
        /// </summary>
        public const string Msi = nameof(Msi);

        /// <summary>
        /// Auth service
        /// </summary>
        public const string AuthService = nameof(AuthService);
    }
}
