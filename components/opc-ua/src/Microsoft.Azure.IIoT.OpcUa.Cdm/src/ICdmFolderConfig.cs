// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Cdm {

    /// <summary>
    /// configuration for the CDM folder
    /// </summary>
    public interface ICdmFolderConfig {

        /// <summary>
        /// Blob container name used by the CDM storage
        /// </summary>
        string StorageDrive { get; }

        /// <summary>
        /// CDM root folder within CDM blob container
        /// </summary>
        string StorageFolder { get; }
    }
}
