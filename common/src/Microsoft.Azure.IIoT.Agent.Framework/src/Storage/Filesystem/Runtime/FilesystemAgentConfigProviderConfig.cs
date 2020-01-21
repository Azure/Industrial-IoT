// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Filesystem {

    /// <summary>
    /// File configuration provider
    /// </summary>
    public class FilesystemAgentConfigProviderConfig : IFilesystemAgentConfigProviderConfig {

        /// <inheritdoc/>
        public string ConfigFilename { get; set; }
    }
}