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
        /// ADLSg2 connection Host name
        /// #identifier#.dfs.core.windows.net
        /// </summary>
        string ADLSg2HostName { get; }

        /// <summary>
        /// the blob name used by thge CDM storage - E.g. pwerbi
        /// </summary>
        string ADLSg2BlobName { get; }

        /// <summary>
        /// the cdm storage's root folder
        /// </summary>
        string RootFolder { get; }
    }
}
