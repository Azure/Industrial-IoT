// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Storage {

    /// <summary>
    /// A managed storage resource
    /// </summary>
    public interface IStorageResource : IResource {

        /// <summary>
        /// The storage connection string
        /// </summary>
        string StorageConnectionString { get; }
    }
}
