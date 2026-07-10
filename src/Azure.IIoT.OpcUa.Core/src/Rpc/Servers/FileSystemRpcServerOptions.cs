// ------------------------------------------------------------
//  Copyright (c) Microsoft.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Servers
{
    /// <summary>
    /// File system options
    /// </summary>
    public class FileSystemRpcServerOptions
    {
        /// <summary>
        /// Request file path. The file contains the
        /// request in .http file format.
        /// </summary>
        public string? RequestFilePath { get; set; }

        /// <summary>
        /// Response file path, the folder with the
        /// file in it must be writable.
        /// </summary>
        public string? ResponseFilePath { get; set; }
    }
}
