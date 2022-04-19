// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher {

    /// <summary>
    /// File system sink configuration
    /// </summary>
    public interface IFileSystemConfiguration {

        /// <summary>
        /// Folder to write to
        /// </summary>
        string Directory { get; }
    }
}