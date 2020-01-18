// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Runtime {
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Storage.Blob;
    using Microsoft.Azure.IIoT.Storage.Blob.Runtime;
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Azure.IIoT.Crypto.KeyVault;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Azure.IIoT.Crypto.KeyVault.Runtime;

    /// <summary>
    /// Configuration aggregation
    /// </summary>
    public class Config : ApiConfig, IClientConfig, IStorageConfig, IKeyVaultConfig {

        /// <inheritdoc/>
        public string BlobStorageConnString => _stg.BlobStorageConnString;

        /// <inheritdoc/>
        public string AppId => _auth.AppId;
        /// <inheritdoc/>
        public string AppSecret => _auth.AppSecret;
        /// <inheritdoc/>
        public string TenantId => _auth.TenantId;
        /// <inheritdoc/>
        public string InstanceUrl => _auth.InstanceUrl;
        /// <inheritdoc/>
        public string Domain => _auth.Domain;

        /// <inheritdoc/>
        public string KeyVaultBaseUrl => _kv.KeyVaultBaseUrl;
        /// <inheritdoc/>
        public string KeyVaultResourceId => _kv.KeyVaultResourceId;
        /// <inheritdoc/>
        public bool KeyVaultIsHsm => _kv.KeyVaultIsHsm;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration) {

            _auth = new ApiClientConfig(configuration);
            _stg = new StorageConfig(configuration);
            _kv = new KeyVaultConfig(configuration);
        }

        private readonly ApiClientConfig _auth;
        private readonly StorageConfig _stg;
        private readonly KeyVaultConfig _kv;
    }
}
