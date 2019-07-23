// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Tests {
    using Microsoft.Azure.IIoT.Crypto;
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Tests;
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Xunit;


    public static class X509TestUtils {
        public static void VerifyApplicationCertIntegrity(
            X509Certificate2 newCert,
            Key privateKey,
            X509Certificate2Collection issuerCertificates) {

            Assert.NotNull(newCert);
            Assert.True(privateKey.HasPrivateKey());
            if (privateKey != null) {
                using (var newPrivateKeyCert = new X509Certificate2(newCert.RawData) {
                    PrivateKey = privateKey.ToRSA()
                }) {
                    Assert.True(newPrivateKeyCert.HasPrivateKey);
                    Assert.NotNull(newPrivateKeyCert);
                    Assert.True(newPrivateKeyCert.HasPrivateKey);
                    // verify the public cert matches the private key
                    Assert.True(CertificateFactory.VerifyRSAKeyPair(newCert, newPrivateKeyCert, true));
                    Assert.True(CertificateFactory.VerifyRSAKeyPair(newPrivateKeyCert, newPrivateKeyCert, true));
                }
            }
            var issuerCertIdCollection = new CertificateIdentifierCollection();
            foreach (var issuerCert in issuerCertificates) {
                issuerCertIdCollection.Add(new CertificateIdentifier(issuerCert));
            }

            // verify cert with issuer chain
            var certValidator = new CertificateValidator();
            var issuerStore = new CertificateTrustList();
            var trustedStore = new CertificateTrustList {
                TrustedCertificates = issuerCertIdCollection
            };
            certValidator.Update(trustedStore, issuerStore, null);
            Assert.Throws<ServiceResultException>(() => certValidator.Validate(newCert));
            issuerStore.TrustedCertificates = issuerCertIdCollection;
            certValidator.Update(issuerStore, trustedStore, null);
            certValidator.Validate(newCert);
        }

        public static void VerifySignedApplicationCert(ApplicationTestData testApp,
            X509Certificate2 signedCert, X509Certificate2Collection issuerCerts) {
            var issuerCert = issuerCerts[0];

            var signedCertO = signedCert.ToCertificate();
            Assert.NotNull(signedCertO);
            Assert.False(signedCert.HasPrivateKey);
            Assert.True(Utils.CompareDistinguishedName(testApp.Subject, signedCert.Subject));
            Assert.False(Utils.CompareDistinguishedName(signedCert.Issuer, signedCert.Subject));
            Assert.True(Utils.CompareDistinguishedName(signedCert.Issuer, issuerCert.Subject));

            // test basic constraints
            var constraints = signedCertO.GetBasicConstraintsExtension();
            Assert.NotNull(constraints);
            Assert.True(constraints.Critical);
            Assert.False(constraints.CertificateAuthority);
            Assert.False(constraints.HasPathLengthConstraint);

            // key usage
            var keyUsage = signedCertO.GetKeyUsageExtension();
            Assert.NotNull(keyUsage);
            Assert.True(keyUsage.Critical);
            Assert.True((keyUsage.KeyUsages & X509KeyUsageFlags.CrlSign) == 0);
            Assert.True((keyUsage.KeyUsages & X509KeyUsageFlags.DataEncipherment) == X509KeyUsageFlags.DataEncipherment);
            Assert.True((keyUsage.KeyUsages & X509KeyUsageFlags.DecipherOnly) == 0);
            Assert.True((keyUsage.KeyUsages & X509KeyUsageFlags.DigitalSignature) == X509KeyUsageFlags.DigitalSignature);
            Assert.True((keyUsage.KeyUsages & X509KeyUsageFlags.EncipherOnly) == 0);
            Assert.True((keyUsage.KeyUsages & X509KeyUsageFlags.KeyAgreement) == 0);
            Assert.True((keyUsage.KeyUsages & X509KeyUsageFlags.KeyCertSign) == 0);
            Assert.True((keyUsage.KeyUsages & X509KeyUsageFlags.KeyEncipherment) == X509KeyUsageFlags.KeyEncipherment);
            Assert.True((keyUsage.KeyUsages & X509KeyUsageFlags.NonRepudiation) == X509KeyUsageFlags.NonRepudiation);

            // enhanced key usage
            var enhancedKeyUsage = signedCertO.GetEnhancedKeyUsageExtension();
            Assert.NotNull(enhancedKeyUsage);
            Assert.True(enhancedKeyUsage.Critical);

            // test for authority key
            var authority = signedCertO.GetAuthorityKeyIdentifierExtension();
            Assert.NotNull(authority);
            Assert.NotNull(authority.SerialNumber);
            Assert.NotNull(authority.KeyId);
            Assert.NotNull(authority.AuthorityNames);

            // verify authority key in signed cert
            var subjectKeyId = signedCertO.GetSubjectKeyIdentifierExtension();
            Assert.Equal(subjectKeyId.SubjectKeyIdentifier, authority.KeyId);
            Assert.Equal(issuerCert.SerialNumber, authority.SerialNumber.ToString());

            var subjectAlternateName = signedCertO.GetSubjectAltNameExtension();
            Assert.NotNull(subjectAlternateName);
            Assert.False(subjectAlternateName.Critical);
            var domainNames = Utils.GetDomainsFromCertficate(signedCert);
            foreach (var domainName in testApp.DomainNames) {
                Assert.Contains(domainName, domainNames, StringComparer.OrdinalIgnoreCase);
            }
            Assert.True(subjectAlternateName.Uris.Count == 1);
            var applicationUri = Utils.GetApplicationUriFromCertificate(signedCert);
            Assert.True(testApp.ApplicationRecord.ApplicationUri == applicationUri);

            var issuerCertIdCollection = new CertificateIdentifierCollection();
            foreach (var cert in issuerCerts) {
                issuerCertIdCollection.Add(new CertificateIdentifier(cert));
            }

            // verify cert with issuer chain
            var certValidator = new CertificateValidator();
            var issuerStore = new CertificateTrustList();
            var trustedStore = new CertificateTrustList {
                TrustedCertificates = issuerCertIdCollection
            };
            certValidator.Update(trustedStore, issuerStore, null);
            Assert.Throws<ServiceResultException>(() => certValidator.Validate(signedCert));
            issuerStore.TrustedCertificates = issuerCertIdCollection;
            certValidator.Update(issuerStore, trustedStore, null);
            certValidator.Validate(signedCert);

        }

        internal static async Task<CertificateValidator> CreateValidatorAsync(TrustListModel trustList) {
            var storePath = "%LocalApplicationData%/OPCVaultTest/pki/";
            DeleteDirectory(storePath);

            // verify cert with issuer chain
            var certValidator = new CertificateValidator();
            var issuerTrustList = await CreateTrustListAsync(
                storePath + "issuer",
                trustList.IssuerCertificates.ToStackModel(),
                trustList.IssuerCrls.ToStackModel()
                );
            var trustedTrustList = await CreateTrustListAsync(
                storePath + "trusted",
                trustList.TrustedCertificates.ToStackModel(),
                trustList.TrustedCrls.ToStackModel()
                );

            certValidator.Update(issuerTrustList, trustedTrustList, null);
            return certValidator;
        }

        /// <summary>
        /// Create collection
        /// </summary>
        /// <param name="certificateCollection"></param>
        public static X509Certificate2Collection ToStackModel(
            this X509CertificateChainModel certificateCollection) {
            return new X509Certificate2Collection(certificateCollection.Chain
                .Select(c => c.ToStackModel().ToX509Certificate2()).ToArray());
        }

        internal static async Task<CertificateTrustList> CreateTrustListAsync(
            string storePath,
            X509Certificate2Collection certCollection,
            IEnumerable<Crl> crlCollection) {
            var certTrustList = new CertificateTrustList {
                StoreType = CertificateStoreType.Directory,
                StorePath = storePath
            };
            using (var store = certTrustList.OpenStore()) {
                foreach (var cert in certCollection) {
                    await store.Add(cert);
                }
                if (store.SupportsCRLs) {
                    foreach (var crl in crlCollection) {
                        using (var x509crl = new X509CRL(crl.RawData)) {
                            store.AddCRL(x509crl);
                        }
                    }
                }
            }
            return certTrustList;
        }

        public static void CleanupTrustList(Opc.Ua.ICertificateStore _store) {
            using (var store = _store) {
                var certs = store.Enumerate().Result;
                foreach (var cert in certs) {
                    store.Delete(cert.Thumbprint);
                }
                var crls = store.EnumerateCRLs();
                foreach (var crl in crls) {
                    store.DeleteCRL(crl);
                }
            }
        }

        public static void DeleteDirectory(string storePath) {
            try {
                var fullStorePath = Utils.ReplaceSpecialFolderNames(storePath);
                if (Directory.Exists(fullStorePath)) {
                    Directory.Delete(fullStorePath, true);
                }
            }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
            catch {
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                // intentionally ignore errors
            }
        }
    }

}
