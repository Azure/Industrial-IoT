// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Cdm.Runtime {
    using Microsoft.Azure.IIoT.Cdm;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// CDM storage configuration
    /// </summary>
    public class CdmClientConfig : DiagnosticsConfig, ICdmClientConfig {

        /// <summary>
        /// CDM's ADLSg2 configuration
        /// </summary>
        private const string kCdmAdDLS2HostName = "Cdm:ADLSg2HostName";
        private const string kCdmADLSg2BlobName = "Cdm:ADLSg2BlobName";
        private const string kCdmRootFolder = "Cdm:RootFolder";
        private const string kAuth_AppIdKey = "Auth:ServiceId";
        private const string kAuth_AppSecretKey = "Auth:ServiceSecret";
        private const string kAuth_TenantIdKey = "Auth:TenantId";
        private const string kAuth_DomainKey = "Auth:TenantId";
        private const string kAuth_InstanceUrlKey = "Auth:InstanceUrl";
        private const string kAuth_AudienceKey = "Auth:Audience";

        /// <summary>Application id</summary>
        public string AppId => GetStringOrDefault(kAuth_AppIdKey,
            GetStringOrDefault("PCS_AUTH_AAD_SERVICEID"))?.Trim();
        /// <summary>App secret</summary>
        public string AppSecret => GetStringOrDefault(kAuth_AppSecretKey,
            GetStringOrDefault("PCS_AUTH_AAD_SERVICESECRET"))?.Trim();
        /// <summary>Optional tenant</summary>
        public string TenantId => GetStringOrDefault(kAuth_TenantIdKey,
            GetStringOrDefault("PCS_WEBUI_AUTH_AAD_TENANT", "common")).Trim();
        /// <summary>Aad domain</summary>
        public string Domain => GetStringOrDefault(kAuth_DomainKey,
            GetStringOrDefault("PCS_AUTH_DOMAIN", Try.Op(() =>
            new Uri(GetStringOrDefault("PCS_AUTH_AUDIENCE")).DnsSafeHost)))?.Trim();
        /// <summary>Aad instance url</summary>
        public string InstanceUrl => GetStringOrDefault(kAuth_InstanceUrlKey,
            GetStringOrDefault("PCS_WEBUI_AUTH_AAD_INSTANCE",
                "https://login.microsoftonline.com")).Trim();
        /// <summary>Audience</summary>
        public string Audience => GetStringOrDefault(kAuth_AudienceKey,
            GetStringOrDefault("PCS_AUTH_AUDIENCE", null));
        /// <summary>ADLSg2 host's name </summary>
        public string ADLSg2HostName => GetStringOrDefault(kCdmAdDLS2HostName,
                (GetStringOrDefault("PCS_ADLSG2_ACCOUNT") + ".dfs.core.windows.net"));
        /// <summary>Blob name to store data in the ADLSg2</summary>
        public string ADLSg2BlobName => GetStringOrDefault(kCdmADLSg2BlobName,
            GetStringOrDefault("PCS_CDM_ADLSG2_BLOBNAME", "powerbi"));
        /// <summary>Root Folder within the blob</summary>
        public string RootFolder => GetStringOrDefault(kCdmRootFolder,
            GetStringOrDefault("PCS_CDM_ROOTFOLDER", "IIoTDataFlow"));
        
        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public CdmClientConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
