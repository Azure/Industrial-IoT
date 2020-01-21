// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients {

    /// <summary>
    /// Configuration for AAD application registration
    /// </summary>
    public interface IClientConfig {

        /// <summary>
        /// The AAD application id for the client.
        /// </summary>
        string AppId { get; }

        /// <summary>
        /// AAD Client / Application secret
        /// </summary>
        string AppSecret { get; }

        /// <summary>
        /// Tenant id if any (optional - defaults
        /// to "common" for universal endpoint.)
        /// </summary>
        string TenantId { get; }

        /// <summary>
        /// The AAD tenant domain name
        /// </summary>
        string Domain { get; }

        /// <summary>
        /// Instance url (This is optional as it
        /// defaults to Azure global cloud, i.e.
        /// https://login.microsoftonline.com/, but can
        /// be set to another azure cloud as well.)
        /// </summary>
        string InstanceUrl { get; }
    }
}
