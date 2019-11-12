// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Filesystem {

    /// <summary>
    /// Configuration for file system job repo
    /// </summary>
    public interface IFilesystemJobRepositoryConfig {

        /// <summary>
        /// Root
        /// </summary>
        string RootDirectory { get; }

        /// <summary>
        /// Update buffer size
        /// </summary>
        int UpdateBuffer { get; }
    }
}