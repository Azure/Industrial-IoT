// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Datalake.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Blob storage configuration
    /// </summary>
    public class StorageConfig : ConfigBase, IStorageConfig {
        /// <summary>
        /// Configuration keys
        /// </summary>
        private const string kStorageAccountNameKey = "Storage:AccountName";
        private const string kStorageEndpointSuffixKey = "Storage:EndpointSuffix";
        private const string kStorageAccountKeyKey = "Storage:AccountKey";

        /// <summary> Name </summary>
        public string AccountName =>
            GetConnectonStringTokenOrDefault(PcsVariable.PCS_STORAGE_CONNSTRING,
                cs => cs.Endpoint,
            () => GetStringOrDefault(kStorageAccountNameKey,
            () => GetStringOrDefault("PCS_ASA_DATA_AZUREBLOB_ACCOUNT",
                () => GetStringOrDefault("PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT",
                    () => null))));
        /// <summary> Suffix </summary>
        public string EndpointSuffix =>
            GetConnectonStringTokenOrDefault(PcsVariable.PCS_STORAGE_CONNSTRING,
                cs => cs.EndpointSuffix,
            () => GetStringOrDefault(kStorageEndpointSuffixKey,
            () => GetStringOrDefault("PCS_ASA_DATA_AZUREBLOB_ENDPOINT_SUFFIX",
                () => GetStringOrDefault("PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX",
                     () => "core.windows.net"))));
        /// <summary> Key </summary>
        public string AccountKey =>
            GetConnectonStringTokenOrDefault(PcsVariable.PCS_STORAGE_CONNSTRING,
                cs => cs.SharedAccessKey,
            () => GetStringOrDefault(kStorageAccountKeyKey,
            () => GetStringOrDefault("PCS_ASA_DATA_AZUREBLOB_KEY",
                () => GetStringOrDefault("PCS_IOTHUBREACT_AZUREBLOB_KEY",
                    () => null))));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public StorageConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
