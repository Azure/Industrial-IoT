// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Opc.Ua;
using Xunit;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test
{
    public class X509TestUtils
    {
        public static void VerifyApplicationCertIntegrity(
            X509Certificate2 newCert,
            byte[] privateKey,
            string privateKeyPassword,
            string privateKeyFormat,
            X509Certificate2Collection issuerCertificates)
        {
            Assert.NotNull(newCert);
            if (privateKey != null)
            {
                X509Certificate2 newPrivateKeyCert = null;
                if (privateKeyFormat == "PFX")
                {
                    newPrivateKeyCert = CertificateFactory.CreateCertificateFromPKCS12(privateKey, privateKeyPassword);
                }
                else if (privateKeyFormat == "PEM")
                {
                    newPrivateKeyCert = CertificateFactory.CreateCertificateWithPEMPrivateKey(newCert, privateKey, privateKeyPassword);
                }
                else
                {
                    Assert.True(false, "Invalid private key format");
                }
                Assert.NotNull(newPrivateKeyCert);
                // verify the public cert matches the private key
                Assert.True(CertificateFactory.VerifyRSAKeyPair(newCert, newPrivateKeyCert, true));
                Assert.True(CertificateFactory.VerifyRSAKeyPair(newPrivateKeyCert, newPrivateKeyCert, true));
            }

            CertificateIdentifierCollection issuerCertIdCollection = new CertificateIdentifierCollection();
            foreach (var issuerCert in issuerCertificates)
            {
                issuerCertIdCollection.Add(new CertificateIdentifier(issuerCert));
            }

            // verify cert with issuer chain
            CertificateValidator certValidator = new CertificateValidator();
            CertificateTrustList issuerStore = new CertificateTrustList();
            CertificateTrustList trustedStore = new CertificateTrustList();
            trustedStore.TrustedCertificates = issuerCertIdCollection;
            certValidator.Update(trustedStore, issuerStore, null);
            Assert.Throws<Opc.Ua.ServiceResultException>(() =>
            {
                certValidator.Validate(newCert);
            });
            issuerStore.TrustedCertificates = issuerCertIdCollection;
            certValidator.Update(issuerStore, trustedStore, null);
            certValidator.Validate(newCert);
        }

        public static void VerifySignedApplicationCert(ApplicationTestData testApp, X509Certificate2 signedCert, X509Certificate2Collection issuerCerts)
        {
            X509Certificate2 issuerCert = issuerCerts[0];

            Assert.NotNull(signedCert);
            Assert.False(signedCert.HasPrivateKey);
            Assert.True(Opc.Ua.Utils.CompareDistinguishedName(testApp.Subject, signedCert.Subject));
            Assert.False(Opc.Ua.Utils.CompareDistinguishedName(signedCert.Issuer, signedCert.Subject));
            Assert.True(Opc.Ua.Utils.CompareDistinguishedName(signedCert.Issuer, issuerCert.Subject));

            // test basic constraints
            var constraints = FindBasicConstraintsExtension(signedCert);
            Assert.NotNull(constraints);
            Assert.True(constraints.Critical);
            Assert.False(constraints.CertificateAuthority);
            Assert.False(constraints.HasPathLengthConstraint);

            // key usage
            var keyUsage = FindKeyUsageExtension(signedCert);
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
            var enhancedKeyUsage = FindEnhancedKeyUsageExtension(signedCert);
            Assert.NotNull(enhancedKeyUsage);
            Assert.True(enhancedKeyUsage.Critical);

            // test for authority key
            X509AuthorityKeyIdentifierExtension authority = FindAuthorityKeyIdentifier(signedCert);
            Assert.NotNull(authority);
            Assert.NotNull(authority.SerialNumber);
            Assert.NotNull(authority.KeyId);
            Assert.NotNull(authority.AuthorityNames);

            // verify authority key in signed cert
            X509SubjectKeyIdentifierExtension subjectKeyId = FindSubjectKeyIdentifierExtension(issuerCert);
            Assert.Equal(subjectKeyId.SubjectKeyIdentifier, authority.KeyId);
            Assert.Equal(issuerCert.SerialNumber, authority.SerialNumber);

            X509SubjectAltNameExtension subjectAlternateName = FindSubjectAltName(signedCert);
            Assert.NotNull(subjectAlternateName);
            Assert.False(subjectAlternateName.Critical);
            var domainNames = Opc.Ua.Utils.GetDomainsFromCertficate(signedCert);
            foreach (var domainName in testApp.DomainNames)
            {
                Assert.Contains(domainName, domainNames, StringComparer.OrdinalIgnoreCase);
            }
            Assert.True(subjectAlternateName.Uris.Count == 1);
            var applicationUri = Opc.Ua.Utils.GetApplicationUriFromCertificate(signedCert);
            Assert.True(testApp.ApplicationRecord.ApplicationUri == applicationUri);

            CertificateIdentifierCollection issuerCertIdCollection = new CertificateIdentifierCollection();
            foreach (var cert in issuerCerts)
            {
                issuerCertIdCollection.Add(new CertificateIdentifier(cert));
            }

            // verify cert with issuer chain
            CertificateValidator certValidator = new CertificateValidator();
            CertificateTrustList issuerStore = new CertificateTrustList();
            CertificateTrustList trustedStore = new CertificateTrustList();
            trustedStore.TrustedCertificates = issuerCertIdCollection;
            certValidator.Update(trustedStore, issuerStore, null);
            Assert.Throws<Opc.Ua.ServiceResultException>(() =>
            {
                certValidator.Validate(signedCert);
            });
            issuerStore.TrustedCertificates = issuerCertIdCollection;
            certValidator.Update(issuerStore, trustedStore, null);
            certValidator.Validate(signedCert);

        }

        internal static async Task<CertificateValidator> CreateValidatorAsync(KeyVaultTrustListModel trustList)
        {
            var storePath = "%LocalApplicationData%/OPCVaultTest/pki/";
            DeleteDirectory(storePath);

            // verify cert with issuer chain
            var certValidator = new CertificateValidator();
            var issuerTrustList = await CreateTrustListAsync(
                storePath + "issuer",
                trustList.IssuerCertificates,
                trustList.IssuerCrls
                );
            var trustedTrustList = await CreateTrustListAsync(
                storePath + "trusted",
                trustList.TrustedCertificates,
                trustList.TrustedCrls
                );

            certValidator.Update(issuerTrustList, trustedTrustList, null);
            return certValidator;
        }

        internal static async Task<CertificateTrustList> CreateTrustListAsync(
            string storePath, 
            X509Certificate2Collection certCollection,
            IList<X509CRL> crlCollection)
        {
            var certTrustList = new CertificateTrustList();
            certTrustList.StoreType = CertificateStoreType.Directory;
            certTrustList.StorePath = storePath;
            using (var store = certTrustList.OpenStore())
            {
                foreach (var cert in certCollection)
                {
                    await store.Add(cert);
                }
                if (store.SupportsCRLs)
                {
                    foreach (var crl in crlCollection)
                    {
                        store.AddCRL(crl);
                    }
                }
            }
            return certTrustList;
        }

        internal static X509BasicConstraintsExtension FindBasicConstraintsExtension(X509Certificate2 certificate)
        {
            for (int ii = 0; ii < certificate.Extensions.Count; ii++)
            {
                X509BasicConstraintsExtension extension = certificate.Extensions[ii] as X509BasicConstraintsExtension;
                if (extension != null)
                {
                    return extension;
                }
            }
            return null;
        }

        internal static X509KeyUsageExtension FindKeyUsageExtension(X509Certificate2 certificate)
        {
            for (int ii = 0; ii < certificate.Extensions.Count; ii++)
            {
                X509KeyUsageExtension extension = certificate.Extensions[ii] as X509KeyUsageExtension;
                if (extension != null)
                {
                    return extension;
                }
            }
            return null;
        }
        internal static X509EnhancedKeyUsageExtension FindEnhancedKeyUsageExtension(X509Certificate2 certificate)
        {
            for (int ii = 0; ii < certificate.Extensions.Count; ii++)
            {
                X509EnhancedKeyUsageExtension extension = certificate.Extensions[ii] as X509EnhancedKeyUsageExtension;
                if (extension != null)
                {
                    return extension;
                }
            }
            return null;
        }

        internal static X509AuthorityKeyIdentifierExtension FindAuthorityKeyIdentifier(X509Certificate2 certificate)
        {
            for (int ii = 0; ii < certificate.Extensions.Count; ii++)
            {
                X509Extension extension = certificate.Extensions[ii];

                switch (extension.Oid.Value)
                {
                    case X509AuthorityKeyIdentifierExtension.AuthorityKeyIdentifierOid:
                    case X509AuthorityKeyIdentifierExtension.AuthorityKeyIdentifier2Oid:
                        {
                            return new X509AuthorityKeyIdentifierExtension(extension, extension.Critical);
                        }
                }
            }

            return null;
        }

        internal static X509SubjectAltNameExtension FindSubjectAltName(X509Certificate2 certificate)
        {
            foreach (var extension in certificate.Extensions)
            {
                if (extension.Oid.Value == X509SubjectAltNameExtension.SubjectAltNameOid ||
                    extension.Oid.Value == X509SubjectAltNameExtension.SubjectAltName2Oid)
                {
                    return new X509SubjectAltNameExtension(extension, extension.Critical);
                }
            }
            return null;
        }

        internal static X509SubjectKeyIdentifierExtension FindSubjectKeyIdentifierExtension(X509Certificate2 certificate)
        {
            for (int ii = 0; ii < certificate.Extensions.Count; ii++)
            {
                X509SubjectKeyIdentifierExtension extension = certificate.Extensions[ii] as X509SubjectKeyIdentifierExtension;
                if (extension != null)
                {
                    return extension;
                }
            }
            return null;
        }

        public static void CleanupTrustList(ICertificateStore _store)
        {
            using (var store = _store)
            {
                var certs = store.Enumerate().Result;
                foreach (var cert in certs)
                {
                    store.Delete(cert.Thumbprint);
                }
                var crls = store.EnumerateCRLs();
                foreach (var crl in crls)
                {
                    store.DeleteCRL(crl);
                }
            }
        }

        public static void DeleteDirectory(string storePath)
        {
            try
            {
                string fullStorePath = Opc.Ua.Utils.ReplaceSpecialFolderNames(storePath);
                if (Directory.Exists(fullStorePath))
                {
                    Directory.Delete(fullStorePath, true);
                }
            }
            catch
            {
                // intentionally ignore errors
            }
        }
    }

    public static class X509CRLExtension
    {
        internal static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (list is List<T>)
            {
                ((List<T>)list).AddRange(items);
            }
            else
            {
                foreach (var item in items)
                {
                    list.Add(item);
                }
            }
        }

        internal static void AddRange(this KeyVaultTrustListModel list, KeyVaultTrustListModel items)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            list.TrustedCertificates.AddRange(items.TrustedCertificates);
            list.TrustedCrls.AddRange(items.TrustedCrls);
            list.IssuerCertificates.AddRange(items.IssuerCertificates);
            list.IssuerCrls.AddRange(items.IssuerCrls);
        }

    }

}
