// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Cdm.Runtime {
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// CDM storage configuration
    /// </summary>
    public class CdmClientConfig : DiagnosticsConfig, ICdmClientConfig, IClientConfig {

        /// <summary>
        /// CDM's ADLSg2 configuration
        /// </summary>
        private const string kCdmAdDLS2HostName = "Cdm:ADLSg2HostName";
        private const string kCdmADLSg2ContainerName = "Cdm:ADLSg2ContainerName";
        private const string kCdmRootFolder = "Cdm:RootFolder";

        /// <inheritdoc/>
        public string ADLSg2HostName => GetStringOrDefault(kCdmAdDLS2HostName,
            () => GetStringOrDefault(PcsVariable.PCS_ADLSG2_ACCOUNT,
            () => GetStringOrDefault("PCS_ASA_DATA_AZUREBLOB_ACCOUNT",
            () => GetAccountNameFromConnectionString(PcsVariable.PCS_ADLSG2_CONNSTRING))) +
                ".dfs.core.windows.net");
        /// <inheritdoc/>
        public string ADLSg2ContainerName => GetStringOrDefault(kCdmADLSg2ContainerName,
            () => GetStringOrDefault(PcsVariable.PCS_ADLSG2_CONTAINER_CDM,
            () => GetStringOrDefault("PCS_CDM_ADLSG2_BLOBNAME", () => "powerbi")));
        /// <inheritdoc/>
        public string RootFolder => GetStringOrDefault(kCdmRootFolder,
            () => GetStringOrDefault(PcsVariable.PCS_ADLSG2_CONTAINER_CDM_ROOTFOLDER,
            () => GetStringOrDefault("PCS_CDM_ROOTFOLDER", () => "IIoTDataFlow")));

        /// <inheritdoc/>
        public string AppId => _client.AppId;
        /// <inheritdoc/>
        public string AppSecret => _client.AppSecret;
        /// <inheritdoc/>
        public string TenantId => _client.TenantId;
        /// <inheritdoc/>
        public string Domain => _client.Domain;
        /// <inheritdoc/>
        public string InstanceUrl => _client.InstanceUrl;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public CdmClientConfig(IConfiguration configuration) :
            base(configuration) {
            _client = new ClientConfig(configuration);
        }

        /// <summary>
        /// Helper to get account name from connection string
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        private string GetAccountNameFromConnectionString(string variable) {
            var cs = GetStringOrDefault(variable, () => null);
            return cs == null ? null : ConnectionString.Parse(cs).Endpoint;
        }


        private readonly ClientConfig _client;
    }
}
