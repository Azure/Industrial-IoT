// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.IIoT.Auth.Clients;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.KeyVault;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Auth;
using Newtonsoft.Json;
using Opc.Ua;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault
{

    /// <summary>
    /// The Key Vault implementation of the Certificate Group.
    /// </summary>
    public sealed class KeyVaultCertificateGroup : ICertificateGroup
    {
        private readonly IServicesConfig _servicesConfig;
        private readonly IClientConfig _clientConfig;
        private readonly KeyVaultServiceClient _keyVaultServiceClient;
        private readonly ILogger _log;
        private readonly string _serviceHost = null;
        private readonly string _groupSecret = "groups";
        private const string _kAuthority = "https://login.microsoftonline.com/";

        /// <inheritdoc/>
        public KeyVaultCertificateGroup(
            IServicesConfig servicesConfig,
            IClientConfig clientConfig,
            ILogger logger)
        {
            _servicesConfig = servicesConfig;
            _clientConfig = clientConfig;
            _serviceHost = _servicesConfig.ServiceHost;
            _keyVaultServiceClient = new KeyVaultServiceClient(_groupSecret, servicesConfig.KeyVaultBaseUrl, true, logger);
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
            _log.Debug("Creating new instance of `KeyVault` service " + servicesConfig.KeyVaultBaseUrl);
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
            _serviceHost = _servicesConfig.ServiceHost;
            _keyVaultServiceClient = keyVaultServiceClient;
            _log = logger;
            _log.Debug("Creating new on behalf of instance of `KeyVault` service ");
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
                    certificateGroup = KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, certificateGroupConfiguration, _servicesConfig.ServiceHost);
                    await certificateGroup.Init().ConfigureAwait(false);
#if LOADPRIVATEKEY
                    // test if private key can be loaded
                    await certificateGroup.LoadSigningKeyAsync(null, null);
#endif
                    continue;
                }
                catch (Exception ex)
                {
                    _log.Error("Failed to initialize certificate group. ", ex);
                    if (certificateGroup == null)
                    {
                        throw ex;
                    }
                }

                _log.Information("Create new issuer CA certificate for group. ", certificateGroup);

                if (!await certificateGroup.CreateIssuerCACertificateAsync().ConfigureAwait(false))
                {
                    _log.Error("Failed to create issuer CA certificate. ", certificateGroup);
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
                var authority = String.IsNullOrEmpty(_clientConfig.InstanceUrl) ? _kAuthority : _clientConfig.InstanceUrl;
                if (!authority.EndsWith("/"))
                {
                    authority += "/";
                }
                authority += _clientConfig.TenantId;
                var serviceClientCredentials =
                    new KeyVaultCredentials(
                        token,
                        authority,
                        _servicesConfig.KeyVaultResourceId,
                        _clientConfig.AppId,
                        _clientConfig.AppSecret);
                var keyVaultServiceClient = new KeyVaultServiceClient(_groupSecret, _servicesConfig.KeyVaultBaseUrl, true, _log);
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
                _log.Error(ex, "Failed to create on behalf Key Vault client. ");
            }
            return Task.FromResult<ICertificateGroup>(this);
        }

        /// <inheritdoc/>
        public async Task<string[]> GetCertificateGroupIds()
        {
            return await KeyVaultCertificateGroupProvider.GetCertificateGroupIds(_keyVaultServiceClient).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<CertificateGroupConfigurationModel> GetCertificateGroupConfiguration(string id)
        {
            return await KeyVaultCertificateGroupProvider.GetCertificateGroupConfiguration(_keyVaultServiceClient, id).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<CertificateGroupConfigurationModel> UpdateCertificateGroupConfiguration(string id, CertificateGroupConfigurationModel config)
        {
            return await KeyVaultCertificateGroupProvider.UpdateCertificateGroupConfiguration(_keyVaultServiceClient, id, config).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<CertificateGroupConfigurationModel> CreateCertificateGroupConfiguration(string id, string subject, string certType)
        {
            return await KeyVaultCertificateGroupProvider.CreateCertificateGroupConfiguration(_keyVaultServiceClient, id, subject, certType).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IList<CertificateGroupConfigurationModel>> GetCertificateGroupConfigurationCollection()
        {
            string json = await _keyVaultServiceClient.GetCertificateConfigurationGroupsAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<IList<CertificateGroupConfigurationModel>>(json);
        }

        /// <inheritdoc/>
        public async Task<Opc.Ua.X509CRL> RevokeCertificateAsync(string id, X509Certificate2 certificate)
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id, _serviceHost).ConfigureAwait(false);
            await certificateGroup.RevokeCertificateAsync(certificate).ConfigureAwait(false);
            return certificateGroup.Crl;
        }

        /// <inheritdoc/>
        public async Task<X509Certificate2Collection> RevokeCertificatesAsync(string id, X509Certificate2Collection certificates)
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id, _serviceHost).ConfigureAwait(false);
            return await certificateGroup.RevokeCertificatesAsync(certificates).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<X509Certificate2> CreateIssuerCACertificateAsync(string id)
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id, _serviceHost).ConfigureAwait(false);
            if (await certificateGroup.CreateIssuerCACertificateAsync().ConfigureAwait(false))
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
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id, _serviceHost).ConfigureAwait(false);
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
            string requestId,
            string applicationUri,
            string subjectName,
            string[] domainNames,
            string privateKeyFormat,
            string privateKeyPassword
            )
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id, _serviceHost).ConfigureAwait(false);
            var app = new Opc.Ua.Gds.ApplicationRecordDataType
            {
                ApplicationNames = new Opc.Ua.LocalizedTextCollection(),
                ApplicationUri = applicationUri
            };
            var keyPair = await certificateGroup.NewKeyPairRequestAsync(app, subjectName, domainNames, privateKeyFormat, privateKeyPassword).ConfigureAwait(false);
            await certificateGroup.ImportCertKeySecret(id, requestId, keyPair.PrivateKey, keyPair.PrivateKeyFormat);
            return keyPair;
        }

        /// <inheritdoc/>
        public async Task<(X509Certificate2Collection, string)> GetIssuerCACertificateVersionsAsync(string id, bool? withCertificates, string nextPageLink, int? pageSize)
        {
            // TODO: implement withCertificates
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id, _serviceHost).ConfigureAwait(false);
            X509Certificate2Collection result = new X509Certificate2Collection();
            (result, nextPageLink) = await _keyVaultServiceClient.GetCertificateVersionsAsync(id, null, nextPageLink, pageSize);
            return (result, nextPageLink);
        }

        /// <inheritdoc/>
        public async Task<X509Certificate2Collection> GetIssuerCACertificateChainAsync(string id, string thumbPrint = null, string nextPageLink = null, int? pageSize = null)
        {
            // TODO: implement paging (low priority, only when long chains are expected)
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id, _serviceHost).ConfigureAwait(false);
            return new X509Certificate2Collection(await certificateGroup.GetIssuerCACertificateAsync(id, thumbPrint).ConfigureAwait(false));
        }

        /// <inheritdoc/>
        public async Task<IList<X509CRL>> GetIssuerCACrlChainAsync(string id, string thumbPrint = null, string nextPageLink = null, int? pageSize = null)
        {
            // TODO: implement paging (low priority, only when long chains are expected)
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id, _serviceHost).ConfigureAwait(false);
            var crlList = new List<Opc.Ua.X509CRL>
            {
                await certificateGroup.GetIssuerCACrlAsync(id, thumbPrint).ConfigureAwait(false)
            };
            return crlList;
        }

        /// <inheritdoc/>
        public async Task<byte[]> LoadPrivateKeyAsync(string id, string requestId, string privateKeyFormat)
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id, _serviceHost).ConfigureAwait(false);
            return await certificateGroup.LoadCertKeySecret(id, requestId, privateKeyFormat);
        }

        /// <inheritdoc/>
        public async Task AcceptPrivateKeyAsync(string id, string requestId)
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id, _serviceHost).ConfigureAwait(false);
            await certificateGroup.AcceptCertKeySecret(id, requestId);
        }

        /// <inheritdoc/>
        public async Task DeletePrivateKeyAsync(string id, string requestId)
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id, _serviceHost).ConfigureAwait(false);
            await certificateGroup.DeleteCertKeySecret(id, requestId);
        }

        /// <inheritdoc/>
        public async Task<KeyVaultTrustListModel> GetTrustListAsync(string id, string nextPageLink = null, int? pageSize = null)
        {
            return await _keyVaultServiceClient.GetTrustListAsync(id, pageSize, nextPageLink).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task PurgeAsync(string configId = null, string groupId = null)
        {
            await _keyVaultServiceClient.PurgeAsync(configId, groupId).ConfigureAwait(false);
        }

    }
}
