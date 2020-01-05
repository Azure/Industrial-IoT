// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Blob.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Blob storage configuration
    /// </summary>
    public class StorageConfig : ConfigBase, IStorageConfig {

        /// <summary>
        /// Configuration keys
        /// </summary>
        private const string kBlobStorageConnStringKey = "BlobStorageConnectionString";

        /// <summary> Storage </summary>
        public string BlobStorageConnString {
            get {
                var cs = GetStringOrDefault(kBlobStorageConnStringKey,
                    GetStringOrDefault("PCS_STORAGE_CONNSTRING", null))?.Trim();
                if (!string.IsNullOrEmpty(cs)) {
                    return cs;
                }
                var account = GetStringOrDefault("PCS_ASA_DATA_AZUREBLOB_ACCOUNT",
                    GetStringOrDefault("PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT", null));
                var key = GetStringOrDefault("PCS_ASA_DATA_AZUREBLOB_KEY",
                    GetStringOrDefault("PCS_IOTHUBREACT_AZUREBLOB_KEY", null));
                var suffix = GetStringOrDefault("PCS_ASA_DATA_AZUREBLOB_ENDPOINT_SUFFIX",
                    GetStringOrDefault("PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX",
                        "core.windows.net"));
                if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(key)) {
                    return null;
                }
                return ConnectionString.CreateStorageConnectionString(
                    account, suffix, key, "https").ToString();
            }
        }

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public StorageConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
