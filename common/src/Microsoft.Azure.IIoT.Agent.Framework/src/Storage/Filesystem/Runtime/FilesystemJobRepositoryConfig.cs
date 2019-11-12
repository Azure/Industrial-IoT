// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Filesystem {

    /// <summary>
    /// Configuration for file system job repo
    /// </summary>
    public class FilesystemJobRepositoryConfig : IFilesystemJobRepositoryConfig {

        /// <inheritdoc/>
        public string RootDirectory { get; set; }

        /// <inheritdoc/>
        public int UpdateBuffer { get; set; } = 5;
    }
}