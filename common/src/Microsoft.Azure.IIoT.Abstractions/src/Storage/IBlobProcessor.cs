// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Process blobs
    /// </summary>
    public interface IBlobProcessor {

        /// <summary>
        /// Handle blob stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="properties"></param>
        /// <param name="ct"></param>
        /// <returns>Whether the blob was procssed</returns>
        Task<BlobDisposition> ProcessAsync(Stream stream,
            IDictionary<string, string> properties, CancellationToken ct);
    }
}
