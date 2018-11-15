// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.IIoT.Auth.Clients;
using Microsoft.Azure.IIoT.Diagnostics;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.KeyVault;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Auth;
using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault
{

    /// <inheritdoc/>
    public sealed class KeyVaultCertificateGroup : ICertificateGroup
    {
        private readonly IServicesConfig _servicesConfig;
        private readonly IClientConfig _clientConfig;
        private readonly KeyVaultServiceClient _keyVaultServiceClient;
        private readonly ILogger _log;
        private const string kAuthority = "https://login.microsoftonline.com/";

        /// <inheritdoc/>
        public KeyVaultCertificateGroup(
            IServicesConfig servicesConfig,
            IClientConfig clientConfig,
            ILogger logger)
        {
            _servicesConfig = servicesConfig;
            _clientConfig = clientConfig;
            _keyVaultServiceClient = new KeyVaultServiceClient(servicesConfig.KeyVaultBaseUrl, true, logger);
            if (clientConfig != null &&
                clientConfig.AppId != null && clientConfig.AppSecret != null)
            {
                _keyVaultServiceClient.SetAuthenticationClientCredential(clientConfig.AppId, clientConfig.AppSecret);
            }
            else
            {
                // uses MSI or dev account
                _keyVaultServiceClient.SetAuthenticationTokenProvider();
            }
            _log = logger;
            _log.Debug("Creating new instance of `KeyVault` service " + servicesConfig.KeyVaultBaseUrl, () => { });
        }

        /// <inheritdoc/>
        public KeyVaultCertificateGroup(
            KeyVaultServiceClient keyVaultServiceClient,
            IServicesConfig servicesConfig,
            IClientConfig clientConfig,
            ILogger logger
            )
        {
            _servicesConfig = servicesConfig;
            _clientConfig = clientConfig;
            _keyVaultServiceClient = keyVaultServiceClient;
            _log = logger;
            _log.Debug("Creating new on behalf of instance of `KeyVault` service ", () => { });
        }

        /// <inheritdoc/>
        public async Task Init()
        {
            var certificateGroupCollection = await GetCertificateGroupConfigurationCollection().ConfigureAwait(false);
            foreach (var certificateGroupConfiguration in certificateGroupCollection)
            {
                KeyVaultCertificateGroupProvider certificateGroup = null;
                try
                {
                    certificateGroup = KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, certificateGroupConfiguration);
                    await certificateGroup.Init().ConfigureAwait(false);
#if LOADPRIVATEKEY
                    // test if private key can be loaded
                    await certificateGroup.LoadSigningKeyAsync(null, null);
#endif
                    continue;
                }
                catch (Exception ex)
                {
                    _log.Error("Failed to initialize certificate group. ", () => new { ex });
                    if (certificateGroup == null)
                    {
                        throw ex;
                    }
                }

                _log.Error("Create new root CA certificate for group. ", () => new { certificateGroup });

                if (!await certificateGroup.CreateCACertificateAsync().ConfigureAwait(false))
                {
                    _log.Error("Failed to create CA certificate. ", () => new { certificateGroup });
                }
            }
        }

        /// <inheritdoc/>
        public Task<ICertificateGroup> OnBehalfOfRequest(HttpRequest request)
        {
            try
            {
                var accessToken = request.Headers["Authorization"];
                var token = accessToken.First().Remove(0, "Bearer ".Length);
                var serviceClientCredentials =
                    new KeyVaultCredentials(
                        token,
                        (String.IsNullOrEmpty(_clientConfig.InstanceUrl) ? kAuthority : _clientConfig.InstanceUrl) + _clientConfig.TenantId,
                        _servicesConfig.KeyVaultResourceId,
                        _clientConfig.AppId,
                        _clientConfig.AppSecret);
                var keyVaultServiceClient = new KeyVaultServiceClient(_servicesConfig.KeyVaultBaseUrl, true, _log);
                keyVaultServiceClient.SetServiceClientCredentials(serviceClientCredentials);
                return Task.FromResult<ICertificateGroup>(new KeyVaultCertificateGroup(
                    keyVaultServiceClient,
                    _servicesConfig,
                    _clientConfig,
                    _log
                    ));
            }
            catch (Exception ex)
            {
                // try default
                _log.Error("Failed to create on behalf Key Vault client. ", () => new { ex });
            }
            return Task.FromResult<ICertificateGroup>(this);
        }

        /// <inheritdoc/>
        public async Task<string[]> GetCertificateGroupIds()
        {
            return await KeyVaultCertificateGroupProvider.GetCertificateGroupIds(_keyVaultServiceClient).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Opc.Ua.Gds.Server.CertificateGroupConfiguration> GetCertificateGroupConfiguration(string id)
        {
            return await KeyVaultCertificateGroupProvider.GetCertificateGroupConfiguration(_keyVaultServiceClient, id).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Opc.Ua.Gds.Server.CertificateGroupConfiguration> UpdateCertificateGroupConfiguration(string id, Opc.Ua.Gds.Server.CertificateGroupConfiguration config)
        {
            return await KeyVaultCertificateGroupProvider.UpdateCertificateGroupConfiguration(_keyVaultServiceClient, id, config).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Opc.Ua.Gds.Server.CertificateGroupConfiguration> CreateCertificateGroupConfiguration(string id, string subject, string certType)
        {
            return await KeyVaultCertificateGroupProvider.CreateCertificateGroupConfiguration(_keyVaultServiceClient, id, subject, certType).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Opc.Ua.Gds.Server.CertificateGroupConfigurationCollection> GetCertificateGroupConfigurationCollection()
        {
            string json = await _keyVaultServiceClient.GetCertificateConfigurationGroupsAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Opc.Ua.Gds.Server.CertificateGroupConfigurationCollection>(json);
        }

        /// <inheritdoc/>
        public async Task<Opc.Ua.X509CRL> RevokeCertificateAsync(string id, X509Certificate2 certificate)
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id).ConfigureAwait(false);
            await certificateGroup.RevokeCertificateAsync(certificate).ConfigureAwait(false);
            return certificateGroup.Crl;
        }

        /// <inheritdoc/>
        public async Task<X509Certificate2Collection> RevokeCertificatesAsync(string id, X509Certificate2Collection certificates)
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id).ConfigureAwait(false);
            return await certificateGroup.RevokeCertificatesAsync(certificates).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<X509Certificate2> CreateCACertificateAsync(string id)
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id).ConfigureAwait(false);
            if (await certificateGroup.CreateCACertificateAsync().ConfigureAwait(false))
            {
                return certificateGroup.Certificate;
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<X509Certificate2> SigningRequestAsync(
            string id,
            string applicationUri,
            byte[] certificateRequest
            )
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id).ConfigureAwait(false);
            var app = new Opc.Ua.Gds.ApplicationRecordDataType
            {
                ApplicationNames = new Opc.Ua.LocalizedTextCollection(),
                ApplicationUri = applicationUri
            };
            return await certificateGroup.SigningRequestAsync(app, null, certificateRequest).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Opc.Ua.Gds.Server.X509Certificate2KeyPair> NewKeyPairRequestAsync(
            string id,
            string applicationUri,
            string subjectName,
            string[] domainNames,
            string privateKeyFormat,
            string privateKeyPassword
            )
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id).ConfigureAwait(false);
            var app = new Opc.Ua.Gds.ApplicationRecordDataType
            {
                ApplicationNames = new Opc.Ua.LocalizedTextCollection(),
                ApplicationUri = applicationUri
            };
            return await certificateGroup.NewKeyPairRequestAsync(app, subjectName, domainNames, privateKeyFormat, privateKeyPassword).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<X509Certificate2Collection> GetCACertificateChainAsync(string id)
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id).ConfigureAwait(false);
            // TODO: return CA chain
            return new X509Certificate2Collection(await certificateGroup.GetCACertificateAsync(id).ConfigureAwait(false));
        }

        /// <inheritdoc/>
        public async Task<IList<Opc.Ua.X509CRL>> GetCACrlChainAsync(string id)
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id).ConfigureAwait(false);
            var crlList = new List<Opc.Ua.X509CRL>
            {
                // TODO: return CA CRL chain
                await certificateGroup.GetCACrlAsync(id).ConfigureAwait(false)
            };
            return crlList;
        }

        /// <inheritdoc/>
        public async Task<KeyVaultTrustListModel> GetTrustListAsync(string id, int? maxResults = null, string nextPageLink = null)
        {
            return await _keyVaultServiceClient.GetTrustListAsync(id, maxResults, nextPageLink).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task PurgeAsync()
        {
            await _keyVaultServiceClient.PurgeAsync().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        private async Task<X509Certificate2Collection> GetCertificateVersionsAsync(string id)
        {
            return await _keyVaultServiceClient.GetCertificateVersionsAsync(id).ConfigureAwait(false);
        }

    }
}
