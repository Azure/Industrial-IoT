// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Cdm {
    using Microsoft.Azure.IIoT.Auth.Clients;

    /// <summary>
    /// configuration for the CDM storage handler
    /// </summary>
    public interface ICdmClientConfig : IClientConfig {

        /// <summary>
        /// ADLSg2 connection host name
        /// #identifier#.dfs.core.windows.net
        /// </summary>
        string ADLSg2HostName { get; }

        /// <summary>
        /// Blob container name used by the CDM storage
        /// </summary>
        string ADLSg2ContainerName { get; }

        /// <summary>
        /// CDM root folder within CDM blob container
        /// </summary>
        string RootFolder { get; }
    }
}
