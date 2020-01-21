// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Filesystem {
    using System.IO;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// File system configuration provider
    /// </summary>
    public class FilesystemAgentConfigProvider : IAgentConfigProvider {

        /// <summary>
        /// Create provider
        /// </summary>
        /// <param name="filesystemAgentConfigProviderConfig"></param>
        public FilesystemAgentConfigProvider(FilesystemAgentConfigProviderConfig filesystemAgentConfigProviderConfig) {
            _configFilename = filesystemAgentConfigProviderConfig.ConfigFilename;
            var json = File.ReadAllText(_configFilename);
            Config = JsonConvert.DeserializeObject<AgentConfigModel>(json);
        }

        /// <inheritdoc/>
        public AgentConfigModel Config { get; }

        /// <inheritdoc/>
#pragma warning disable 0067
        public event ConfigUpdatedEventHandler OnConfigUpdated;
#pragma warning restore 0067

        private readonly string _configFilename;
    }
}