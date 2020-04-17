// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// File
    /// </summary>
    public interface IFile {

        /// <summary>
        /// Name of the file
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Get file size
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<long> GetSizeAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Get last modified time
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<DateTimeOffset> GetLastModifiedAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Append buffer at end of file and flush.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="count"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task AppendAsync(byte[] stream, int count,
            CancellationToken ct = default);

        /// <summary>
        /// Download entire content into stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DownloadAsync(Stream stream,
            CancellationToken ct = default);

        /// <summary>
        /// Upload from stream and override existing content
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UploadAsync(Stream stream,
            CancellationToken ct = default);
    }
}
