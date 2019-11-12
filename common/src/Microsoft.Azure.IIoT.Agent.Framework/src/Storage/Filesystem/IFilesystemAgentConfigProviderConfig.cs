// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Filesystem {

    /// <summary>
    /// Config provider config
    /// </summary>
    public interface IFilesystemAgentConfigProviderConfig {
        /// <summary>
        /// Config file
        /// </summary>
        string ConfigFilename { get; set; }
    }
}