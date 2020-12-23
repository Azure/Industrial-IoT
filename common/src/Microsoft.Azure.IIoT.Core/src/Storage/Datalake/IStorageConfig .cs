// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Datalake {

    /// <summary>
    /// Storage configuration
    /// </summary>
    public interface IStorageConfig  {

        /// <summary>
        /// Storage endpoint
        /// </summary>
        string EndpointSuffix { get; }

        /// <summary>
        /// Storage account
        /// </summary>
        string AccountName { get; }

        /// <summary>
        /// Storage account key
        /// </summary>
        string AccountKey { get; }
    }
}
