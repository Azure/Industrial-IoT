// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Blob {

    /// <summary>
    /// Configuration for storage
    /// </summary>
    public interface IStorageConfig {

        /// <summary>
        /// Blob storage connection string 
        /// </summary>
        string BlobStorageConnString { get; }
    }
}
