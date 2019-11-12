// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Cdm.Runtime {
    using Microsoft.Azure.IIoT.Cdm; 
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Extensions.Configuration;
    
    /// <summary>
    /// CDM storage configuration
    /// </summary>
    public class CdmClientConfig : ClientConfig, ICdmClientConfig {
        
        /// <summary>
        /// CDM's ADLSg2 configuration
        /// </summary>
        private const string kCdmAdDLS2HostName = "Cdm:ADLSg2HostName";
        private const string kCdmADLSg2BlobName = "Cdm:ADLSg2BlobName";
        private const string kCdmRootFolder = "Cdm:RootFolder";

        /// <summary>ADLSg2 host's name </summary>
        public string ADLSg2HostName => GetStringOrDefault(kCdmAdDLS2HostName,
            GetStringOrDefault("PCS_CDM_ADLSG2_HOSTNAME", null));
        /// <summary>Blob name to store data in the ADLSg2</summary>
        public string ADLSg2BlobName => GetStringOrDefault(kCdmADLSg2BlobName,
            GetStringOrDefault("PCS_CDM_ADLSG2_BLOBNAME", "powerbi"));
        /// <summary>Root Folder within the blob</summary>
        public string RootFolder => GetStringOrDefault(kCdmRootFolder,
            GetStringOrDefault("PCS_CDM_ROOTFOLDER", "/IIoTDataFlow"));
        
        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public CdmClientConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
