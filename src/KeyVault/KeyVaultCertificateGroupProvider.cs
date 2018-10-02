// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.Exceptions;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Gds;
using Opc.Ua.Gds.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using static Microsoft.Azure.IIoT.OpcUa.Services.Vault.KeyVault.KeyVaultCertFactory;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.KeyVault
{
    public sealed class KeyVaultCertificateGroupProvider : Opc.Ua.Gds.Server.CertificateGroup
    {
        public X509CRL Crl;
        public X509SignatureGenerator x509SignatureGenerator;

        private KeyVaultCertificateGroupProvider(
            KeyVaultServiceClient keyVaultServiceClient,
            CertificateGroupConfiguration certificateGroupConfiguration
            )
            :
            base(null, certificateGroupConfiguration)
        {
            _keyVaultServiceClient = keyVaultServiceClient;
            Certificate = null;
            Crl = null;
        }

        public static KeyVaultCertificateGroupProvider Create(
                KeyVaultServiceClient keyVaultServiceClient,
                CertificateGroupConfiguration certificateGroupConfiguration)
        {
            return new KeyVaultCertificateGroupProvider(keyVaultServiceClient, certificateGroupConfiguration);
        }

        public static async Task<KeyVaultCertificateGroupProvider> Create(
            KeyVaultServiceClient keyVaultServiceClient,
            string id)
        {
            var certificateGroupConfiguration = await GetCertificateGroupConfiguration(keyVaultServiceClient, id);
            return new KeyVaultCertificateGroupProvider(keyVaultServiceClient, certificateGroupConfiguration);
        }

        public static async Task<string[]> GetCertificateGroupIds(
            KeyVaultServiceClient keyVaultServiceClient)
        {
            string json = await keyVaultServiceClient.GetCertificateConfigurationGroupsAsync().ConfigureAwait(false);
            List<Opc.Ua.Gds.Server.CertificateGroupConfiguration> certificateGroupCollection = JsonConvert.DeserializeObject<List<Opc.Ua.Gds.Server.CertificateGroupConfiguration>>(json);
            List<string> groups = certificateGroupCollection.Select(cg => cg.Id).ToList();
            return groups.ToArray();
        }

        public static async Task<CertificateGroupConfiguration> GetCertificateGroupConfiguration(
            KeyVaultServiceClient keyVaultServiceClient,
            string id)
        {
            string json = await keyVaultServiceClient.GetCertificateConfigurationGroupsAsync().ConfigureAwait(false);
            List<Opc.Ua.Gds.Server.CertificateGroupConfiguration> certificateGroupCollection = JsonConvert.DeserializeObject<List<Opc.Ua.Gds.Server.CertificateGroupConfiguration>>(json);
            return certificateGroupCollection.SingleOrDefault(cg => String.Equals(cg.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public static async Task<CertificateGroupConfiguration> UpdateCertificateGroupConfiguration(
            KeyVaultServiceClient keyVaultServiceClient,
            string id,
            CertificateGroupConfiguration config)
        {
            if (id.ToLower() != config.Id.ToLower())
            {
                throw new ArgumentException("groupid doesn't match config id");
            }
            string json = await keyVaultServiceClient.GetCertificateConfigurationGroupsAsync().ConfigureAwait(false);
            List<Opc.Ua.Gds.Server.CertificateGroupConfiguration> certificateGroupCollection = JsonConvert.DeserializeObject<List<Opc.Ua.Gds.Server.CertificateGroupConfiguration>>(json);

            var original = certificateGroupCollection.SingleOrDefault(cg => String.Equals(cg.Id, id, StringComparison.OrdinalIgnoreCase));
            if (original == null)
            {
                throw new ArgumentException("invalid groupid");
            }

            ValidateConfiguration(config);

            var index = certificateGroupCollection.IndexOf(original);
            certificateGroupCollection[index] = config;

            json = JsonConvert.SerializeObject(certificateGroupCollection);

            // update config
            json = await keyVaultServiceClient.PutCertificateConfigurationGroupsAsync(json).ConfigureAwait(false);

            // read it back to verify
            certificateGroupCollection = JsonConvert.DeserializeObject<List<Opc.Ua.Gds.Server.CertificateGroupConfiguration>>(json);
            return certificateGroupCollection.SingleOrDefault(cg => String.Equals(cg.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public static async Task<CertificateGroupConfiguration> CreateCertificateGroupConfiguration(
            KeyVaultServiceClient keyVaultServiceClient,
            string id,
            string subject,
            string certType)
        {
            var config = DefaultConfiguration(id, subject, certType);
            if (id.ToLower() != config.Id.ToLower())
            {
                throw new ArgumentException("groupid doesn't match config id");
            }
            string json = await keyVaultServiceClient.GetCertificateConfigurationGroupsAsync().ConfigureAwait(false);
            List<Opc.Ua.Gds.Server.CertificateGroupConfiguration> certificateGroupCollection = JsonConvert.DeserializeObject<List<Opc.Ua.Gds.Server.CertificateGroupConfiguration>>(json);

            var original = certificateGroupCollection.SingleOrDefault(cg => String.Equals(cg.Id, id, StringComparison.OrdinalIgnoreCase));
            if (original != null)
            {
                throw new ArgumentException("groupid already exists");
            }

            ValidateConfiguration(config);

            certificateGroupCollection.Add(config);

            json = JsonConvert.SerializeObject(certificateGroupCollection);

            // update config
            json = await keyVaultServiceClient.PutCertificateConfigurationGroupsAsync(json).ConfigureAwait(false);

            // read it back to verify
            certificateGroupCollection = JsonConvert.DeserializeObject<List<Opc.Ua.Gds.Server.CertificateGroupConfiguration>>(json);
            return certificateGroupCollection.SingleOrDefault(cg => String.Equals(cg.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        #region ICertificateGroupProvider
        public override async Task Init()
        {
            try
            {
                Opc.Ua.Utils.Trace(Opc.Ua.Utils.TraceMasks.Information, "InitializeCertificateGroup: {0}", m_subjectName);
                var result = await _keyVaultServiceClient.GetCertificateAsync(Configuration.Id).ConfigureAwait(false);
                Certificate = new X509Certificate2(result.Cer);
                if (Opc.Ua.Utils.CompareDistinguishedName(Certificate.Subject, Configuration.SubjectName))
                {
                    _caCertSecretIdentifier = result.SecretIdentifier.Identifier;
                    _caCertKeyIdentifier = result.KeyIdentifier.Identifier;
                    Crl = await _keyVaultServiceClient.LoadCACrl(Configuration.Id, Certificate);
                }
                else
                {
                    throw new InvalidConfigurationException("Key Vault certificate subject(" + Certificate.Subject + ") does not match cert group subject " + Configuration.SubjectName);
                }
            }
            catch (Exception e)
            {
                _caCertSecretIdentifier = null;
                _caCertKeyIdentifier = null;
                Certificate = null;
                Crl = null;
                throw e;
            }
        }

        public async Task<bool> CreateCACertificateAsync()
        {
            DateTime now = DateTime.UtcNow;
            using (var caCert = CertificateFactory.CreateCertificate(
                null,
                null,
                null,
                null,
                null,
                Configuration.SubjectName,
                null,
                Configuration.CACertificateKeySize,
                now,
                Configuration.CACertificateLifetime,
                Configuration.CACertificateHashSize,
                true,
                null,
                null))
            {

                // save only public key
                Certificate = new X509Certificate2(caCert.RawData);

                // initialize revocation list
                Crl = CertificateFactory.RevokeCertificate(caCert, null, null);
                if (Crl == null)
                {
                    return false;
                }

                // upload ca cert with private key
                await _keyVaultServiceClient.ImportCACertificate(Configuration.Id, new X509Certificate2Collection(caCert), true).ConfigureAwait(false);
                await _keyVaultServiceClient.ImportCACrl(Configuration.Id, Certificate, Crl).ConfigureAwait(false);
            }
            return true;
        }

        public override async Task<X509CRL> RevokeCertificateAsync(
            X509Certificate2 certificate)
        {
            await LoadPublicAssets().ConfigureAwait(false);
#if LOADPRIVATEKEY
            var issuerCert = await LoadSigningKeyAsync(null, null).ConfigureAwait(false);
#else
            var issuerCert = Certificate;
#endif
            var certificates = new X509Certificate2Collection() { certificate };
            var crls = new List<X509CRL>() { Crl };
            Crl = RevokeCertificate(issuerCert, crls, certificates,
                new KeyVaultSignatureGenerator(_keyVaultServiceClient, _caCertKeyIdentifier, Certificate),
                this.Configuration.CACertificateHashSize);
            await _keyVaultServiceClient.ImportCACrl(Configuration.Id, Certificate, Crl).ConfigureAwait(false);
            return Crl;
        }

        public override async Task<X509Certificate2KeyPair> NewKeyPairRequestAsync(
            ApplicationRecordDataType application,
            string subjectName,
            string[] domainNames,
            string privateKeyFormat,
            string privateKeyPassword)
        {
            DateTime yesterday = DateTime.UtcNow.AddDays(-1);
            // create self signed
            using (var selfSignedCertificate = CertificateFactory.CreateCertificate(
                 null,
                 null,
                 null,
                 application.ApplicationUri ?? "urn:ApplicationURI",
                 application.ApplicationNames.Count > 0 ? application.ApplicationNames[0].Text : "ApplicationName",
                 subjectName,
                 domainNames,
                 Configuration.DefaultCertificateKeySize,
                 yesterday,
                 Configuration.DefaultCertificateLifetime,
                 Configuration.DefaultCertificateHashSize,
                 false,
                 null,
                 null))
            {
                await LoadPublicAssets().ConfigureAwait(false);
                var signedCert = await KeyVaultCertFactory.CreateSignedCertificate(
                    application.ApplicationUri,
                    application.ApplicationNames.Count > 0 ? application.ApplicationNames[0].Text : "ApplicationName",
                    subjectName,
                    domainNames,
                    Configuration.DefaultCertificateKeySize,
                    yesterday,
                    Configuration.DefaultCertificateLifetime,
                    Configuration.DefaultCertificateHashSize,
                    Certificate,
                    selfSignedCertificate.GetRSAPublicKey(),
                    new KeyVaultSignatureGenerator(_keyVaultServiceClient, _caCertKeyIdentifier, Certificate));

                using (var signedCertWithPrivateKey = CertificateFactory.CreateCertificateWithPrivateKey(signedCert, selfSignedCertificate))
                {
                    byte[] privateKey;
                    if (privateKeyFormat == "PFX")
                    {
                        privateKey = signedCertWithPrivateKey.Export(X509ContentType.Pfx, privateKeyPassword);
                    }
                    else if (privateKeyFormat == "PEM")
                    {
                        privateKey = CertificateFactory.ExportPrivateKeyAsPEM(signedCertWithPrivateKey);
                    }
                    else
                    {
                        throw new ServiceResultException(StatusCodes.BadInvalidArgument, "Invalid private key format");
                    }
                    return new X509Certificate2KeyPair(new X509Certificate2(signedCertWithPrivateKey.RawData), privateKeyFormat, privateKey);
                }
            }
        }


        public override async Task<X509Certificate2> SigningRequestAsync(
            ApplicationRecordDataType application,
            string[] domainNames,
            byte[] certificateRequest
            )
        {
            try
            {
                var pkcs10CertificationRequest = new Org.BouncyCastle.Pkcs.Pkcs10CertificationRequest(certificateRequest);
                if (!pkcs10CertificationRequest.Verify())
                {
                    throw new ServiceResultException(StatusCodes.BadInvalidArgument, "CSR signature invalid.");
                }

                var info = pkcs10CertificationRequest.GetCertificationRequestInfo();
                var altNameExtension = GetAltNameExtensionFromCSRInfo(info);
                if (altNameExtension != null)
                {
                    if (altNameExtension.Uris.Count > 0)
                    {
                        if (!altNameExtension.Uris.Contains(application.ApplicationUri))
                        {
                            throw new ServiceResultException(StatusCodes.BadCertificateUriInvalid,
                                "CSR AltNameExtension does not match " + application.ApplicationUri);
                        }
                    }

                    if (altNameExtension.IPAddresses.Count > 0 || altNameExtension.DomainNames.Count > 0)
                    {
                        var domainNameList = new List<string>();
                        domainNameList.AddRange(altNameExtension.DomainNames);
                        domainNameList.AddRange(altNameExtension.IPAddresses);
                        domainNames = domainNameList.ToArray();
                    }
                }

                DateTime yesterday = DateTime.UtcNow.AddDays(-1);
                await LoadPublicAssets().ConfigureAwait(false);
                var signingKey = Certificate;
                {
                    var asn1Decoder = new ASN1Decoder(info.SubjectPublicKeyInfo.GetDerEncoded());
                    var provider = asn1Decoder.GetRSAPublicKey();
                    return await KeyVaultCertFactory.CreateSignedCertificate(
                        application.ApplicationUri,
                        application.ApplicationNames.Count > 0 ? application.ApplicationNames[0].Text : "ApplicationName",
                        info.Subject.ToString(),
                        domainNames,
                        Configuration.DefaultCertificateKeySize,
                        yesterday,
                        Configuration.DefaultCertificateLifetime,
                        Configuration.DefaultCertificateHashSize,
                        signingKey,
                        provider,
                        new KeyVaultSignatureGenerator(_keyVaultServiceClient, _caCertKeyIdentifier, signingKey));
                }
            }
            catch (Exception ex)
            {
                if (ex is ServiceResultException)
                {
                    throw ex as ServiceResultException;
                }
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, ex.Message);
            }
        }

        public async Task<X509Certificate2> GetCACertificateAsync(string id)
        {
            await LoadPublicAssets().ConfigureAwait(false);
            return Certificate;
        }

        public async Task<X509CRL> GetCACrlAsync(string id)
        {
            await LoadPublicAssets().ConfigureAwait(false);
            return Crl;
        }
        #endregion

        public override Task<X509Certificate2> LoadSigningKeyAsync(
            X509Certificate2 signingCertificate,
            string signingKeyPassword)
        {
#if LOADPRIVATEKEY
            await LoadPublicAssets();
            return await _keyVaultServiceClient.LoadSigningCertificateAsync(
                _caCertSecretIdentifier,
                Certificate);
#else
            throw new NotSupportedException("Loading a private key from key Vault is not supported.");
#endif
        }
        private async Task LoadPublicAssets()
        {
            if (Certificate == null ||
                _caCertSecretIdentifier == null ||
                _caCertKeyIdentifier == null)
            {
                await Init();
            }
        }

        private static Opc.Ua.Gds.Server.CertificateGroupConfiguration DefaultConfiguration(string id, string subject, string certType)
        {
            var config = new Opc.Ua.Gds.Server.CertificateGroupConfiguration()
            {
                Id = id,
                SubjectName = subject,
                CertificateType = CertTypeMap()[Opc.Ua.ObjectTypeIds.RsaSha256ApplicationCertificateType],
                DefaultCertificateLifetime = 12,
                DefaultCertificateHashSize = 256,
                DefaultCertificateKeySize = 2048,
                CACertificateLifetime = 60,
                CACertificateHashSize = 256,
                CACertificateKeySize = 2048
            };
            if (certType != null)
            {
                var checkedCertType = CertTypeMap().Where(c => c.Value.ToLower() == certType.ToLower()).Single();
                config.CertificateType = checkedCertType.Value;
            }
            ValidateConfiguration(config);
            return config;
        }

        private static void ValidateConfiguration(Opc.Ua.Gds.Server.CertificateGroupConfiguration update)
        {
            if (!update.Id.All(char.IsLetterOrDigit))
            {
                throw new ArgumentException("Invalid Id");
            }

            // verify subject 
            var subjectList = Opc.Ua.Utils.ParseDistinguishedName(update.SubjectName);
            if (subjectList == null ||
                subjectList.Count == 0)
            {
                throw new ArgumentException("Invalid Subject");
            }

            try
            {
                // only allow specific cert types for now
                var certType = CertTypeMap().Where(c => c.Value.ToLower() == update.CertificateType.ToLower()).Single();
                update.CertificateType = certType.Value;
            }
            catch (Exception)
            {
                throw new ArgumentException("Invalid CertificateType");
            }

            // specify ranges for lifetime (months)
            if (update.DefaultCertificateLifetime < 1 ||
                    update.CACertificateLifetime < 1 ||
                    update.DefaultCertificateLifetime * 2 > update.CACertificateLifetime ||
                    update.DefaultCertificateLifetime > 60 ||
                    update.CACertificateLifetime > 1200)
            {
                throw new ArgumentException("Invalid lifetime");
            }

            if (update.DefaultCertificateKeySize < 2048 ||
                update.DefaultCertificateKeySize % 1024 != 0 ||
                update.DefaultCertificateKeySize > 2048)
            {
                throw new ArgumentException("Invalid key size");
            }

            if (update.CACertificateKeySize < 2048 ||
                update.CACertificateKeySize % 1024 != 0 ||
                update.CACertificateKeySize > 4096)
            {
                throw new ArgumentException("Invalid key size");
            }

            if (update.DefaultCertificateHashSize < 256 ||
                update.DefaultCertificateHashSize % 128 != 0 ||
                update.DefaultCertificateHashSize > 512)
            {
                throw new ArgumentException("Invalid hash size");
            }

            if (update.CACertificateHashSize < 256 ||
                update.CACertificateHashSize % 128 != 0 ||
                update.CACertificateHashSize > 512)
            {
                throw new ArgumentException("Invalid hash size");
            }

            update.BaseStorePath = "/" + update.Id.ToLower();
        }

        private static Dictionary<NodeId, string> CertTypeMap()
        {
            var certTypeMap = new Dictionary<NodeId, string>();
            // FUTURE: support more cert types
            //CertTypeMap.Add(Opc.Ua.ObjectTypeIds.HttpsCertificateType, "HttpsCertificateType");
            //CertTypeMap.Add(Opc.Ua.ObjectTypeIds.UserCredentialCertificateType, "UserCredentialCertificateType");
            certTypeMap.Add(Opc.Ua.ObjectTypeIds.ApplicationCertificateType, "ApplicationCertificateType");
            //CertTypeMap.Add(Opc.Ua.ObjectTypeIds.RsaMinApplicationCertificateType, "RsaMinApplicationCertificateType");
            certTypeMap.Add(Opc.Ua.ObjectTypeIds.RsaSha256ApplicationCertificateType, "RsaSha256ApplicationCertificateType");
            return certTypeMap;
        }

        private KeyVaultServiceClient _keyVaultServiceClient;
        private string _caCertSecretIdentifier;
        private string _caCertKeyIdentifier;
    }
}
