// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {

    /// <summary>
    /// Configuration for oauth clients
    /// </summary>
    public interface IOAuthClientConfig {

        /// <summary>
        /// Name of the authentication scheme
        /// </summary>
        string Scheme { get; }

        /// <summary>
        /// Name of the audience
        /// </summary>
        string Resource { get; }

        /// <summary>
        /// App id of the service
        /// </summary>
        string Audience { get; }

        /// <summary>
        /// The Id of the client.
        /// </summary>
        string AppId { get; }

        /// <summary>
        /// Client secret
        /// </summary>
        string AppSecret { get; }

        /// <summary>
        /// Tenant id if any
        /// </summary>
        string TenantId { get; }

        /// <summary>
        /// Instance url
        /// </summary>
        string InstanceUrl { get; }
    }
}
