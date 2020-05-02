// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System.Threading;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// File Storage
    /// </summary>
    public interface IFileStorage {

        /// <summary>
        /// Get the endpoint at which to access storage.
        /// </summary>
        Uri Endpoint { get; }

        /// <summary>
        /// Open drive with files and folders
        /// </summary>
        /// <param name="driveName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IDrive> CreateOrOpenDriveAsync(string driveName,
            CancellationToken ct = default);
    }
}
