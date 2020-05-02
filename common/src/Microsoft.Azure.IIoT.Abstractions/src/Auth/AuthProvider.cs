// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {

    /// <summary>
    /// Industrial IoT defined auth providers
    /// </summary>
    public static class AuthProvider {

        /// <summary>
        /// Unknown provider
        /// </summary>
        public const string Unknown = nameof(Unknown);

        /// <summary>
        /// User assigned identity based authentication
        /// </summary>
        public const string Msi = nameof(Msi);

        /// <summary>
        /// Azure AD based authentication
        /// </summary>
        public const string AzureAD = nameof(AzureAD);

        /// <summary>
        /// Auth service
        /// </summary>
        public const string AuthService = nameof(AuthService);
    }
}
