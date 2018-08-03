// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.Diagnostics;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.KeyVault;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault
{

    public sealed class KeyVaultCertificateGroup : ICertificateGroup
    {
        private readonly KeyVaultServiceClient _keyVaultServiceClient;
        private readonly ILogger _log;
        public KeyVaultCertificateGroup(
            IServicesConfig config,
            ILogger logger)
        {
            _keyVaultServiceClient = new KeyVaultServiceClient(config.KeyVaultApiUrl, logger);
            // TODO: support AD App ID for authentication
            _keyVaultServiceClient.SetAuthenticationTokenProvider();
            _log = logger;
            _log.Debug("Creating new instance of `KeyVault` service " + config.KeyVaultApiUrl, () => { });
        }

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

        public async Task<string[]> GetCertificateGroupIds()
        {
            return await KeyVaultCertificateGroupProvider.GetCertificateGroupIds(_keyVaultServiceClient).ConfigureAwait(false); ;
        }

        public async Task<Opc.Ua.Gds.Server.CertificateGroupConfiguration> GetCertificateGroupConfiguration(string id)
        {
            return await KeyVaultCertificateGroupProvider.GetCertificateGroupConfiguration(_keyVaultServiceClient, id).ConfigureAwait(false);
        }

        public async Task<Opc.Ua.Gds.Server.CertificateGroupConfigurationCollection> GetCertificateGroupConfigurationCollection()
        {
            string json = await _keyVaultServiceClient.GetCertificateConfigurationGroupsAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Opc.Ua.Gds.Server.CertificateGroupConfigurationCollection>(json);
        }

        public async Task<Opc.Ua.X509CRL> RevokeCertificateAsync(string id, X509Certificate2 certificate)
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id).ConfigureAwait(false);
            await certificateGroup.RevokeCertificateAsync(certificate).ConfigureAwait(false); ;
            return certificateGroup.Crl;
        }

        public async Task<X509Certificate2> CreateCACertificateAsync(string id)
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id).ConfigureAwait(false);
            if (await certificateGroup.CreateCACertificateAsync().ConfigureAwait(false))
            {
                return certificateGroup.Certificate;
            }
            return null;
        }

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

        public async Task<Opc.Ua.Gds.Server.X509Certificate2KeyPair> NewKeyPairRequestAsync(
            string id,
            string applicationUri,
            string subjectName,
            string[] domainNames,
            string privateKeyFormat,
            string privateKeyPassword
            )
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id).ConfigureAwait(false); ;
            var app = new Opc.Ua.Gds.ApplicationRecordDataType
            {
                ApplicationNames = new Opc.Ua.LocalizedTextCollection(),
                ApplicationUri = applicationUri
            };
            return await certificateGroup.NewKeyPairRequestAsync(app, subjectName, domainNames, privateKeyFormat, privateKeyPassword).ConfigureAwait(false); ;
        }

        public async Task<X509Certificate2Collection> GetCACertificateChainAsync(string id)
        {
            var certificateGroup = await KeyVaultCertificateGroupProvider.Create(_keyVaultServiceClient, id).ConfigureAwait(false);
            // TODO: return CA chain
            return new X509Certificate2Collection(await certificateGroup.GetCACertificateAsync(id).ConfigureAwait(false));
        }

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

        public async Task<KeyVaultTrustListModel> GetTrustListAsync(string id)
        {
            return await _keyVaultServiceClient.GetTrustListAsync(id).ConfigureAwait(false);
        }

        private async Task<X509Certificate2Collection> GetCertificateVersionsAsync(string id)
        {
            return await _keyVaultServiceClient.GetCertificateVersionsAsync(id).ConfigureAwait(false);
        }

    }
}
