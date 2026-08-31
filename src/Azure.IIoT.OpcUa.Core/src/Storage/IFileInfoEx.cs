// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Storage
{
    using Microsoft.Extensions.FileProviders;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Writable file info extension.
    /// </summary>
    public interface IFileInfoEx : IFileInfo
    {
        /// <summary>
        /// Whether the file is writable.
        /// </summary>
        bool IsWritable { get; }

        /// <summary>
        /// Create a writable stream.
        /// </summary>
        /// <returns></returns>
        Stream CreateWriteStream();

        /// <summary>
        /// Set last modified.
        /// </summary>
        /// <param name="timestamp"></param>
        void SetLastModified(DateTimeOffset timestamp);

        /// <summary>
        /// Delete file or folder.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DeleteAsync(CancellationToken ct);
    }
}
