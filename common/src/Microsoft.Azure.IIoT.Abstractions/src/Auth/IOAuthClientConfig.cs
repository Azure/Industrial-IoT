// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {

    /// <summary>
    /// Configuration for oauth flow participating clients
    /// </summary>
    public interface IOAuthClientConfig : IOAuthConfig {

        /// <summary>
        /// The <see cref="Http.Resource"/> that
        /// can be accessed with this configuration.
        /// </summary>
        string Resource { get; }

        /// <summary>
        /// Client id
        /// </summary>
        string ClientId { get; }

        /// <summary>
        /// Optional client secret
        /// </summary>
        string ClientSecret { get; }

        /// <summary>
        /// Optional resource's audience.
        /// if not provided will be part of scopes.
        /// </summary>
        string Audience { get; }
    }
}
