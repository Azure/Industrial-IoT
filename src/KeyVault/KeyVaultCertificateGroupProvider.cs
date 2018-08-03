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
using static Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.KeyVault.KeyVaultCertFactory;

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.KeyVault
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
            try
            {
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

                    // upload ca cert with private key
                    await _keyVaultServiceClient.ImportCACertificate(Configuration.Id, new X509Certificate2Collection(caCert), true).ConfigureAwait(false);
                    await _keyVaultServiceClient.ImportCACrl(Configuration.Id, Certificate, Crl).ConfigureAwait(false);
                }
            }
            catch
            {
                return false;
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

        private KeyVaultServiceClient _keyVaultServiceClient;
        private string _caCertSecretIdentifier;
        private string _caCertKeyIdentifier;
    }
}
