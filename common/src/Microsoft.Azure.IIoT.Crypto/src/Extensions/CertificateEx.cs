// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using Microsoft.Azure.IIoT.Crypto.BouncyCastle;
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Certificate extensions
    /// </summary>
    public static class CertificateEx {

        /// <summary>
        /// Parse buffer into cert
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="key"></param>
        /// <param name="policies"></param>
        /// <param name="revoked"></param>
        /// <returns></returns>
        public static Certificate Create(byte[] buffer,
            KeyHandle key = null, IssuerPolicies policies = null,
            RevocationInfo revoked = null) {
            using (var cert = new X509Certificate2(buffer)) {
                return ToCertificate(cert, policies, key, revoked);
            }
        }

        /// <summary>
        /// Parse buffer into cert
        /// </summary>
        /// <param name="cert"></param>
        /// <param name="policies"></param>
        /// <param name="key"></param>
        /// <param name="revoked"></param>
        /// <returns></returns>
        public static Certificate ToCertificate(this X509Certificate2 cert,
            IssuerPolicies policies = null, KeyHandle key = null,
            RevocationInfo revoked = null) {

            if (cert == null) {
                return null;
            }

            // We store big-endian but GetSerialNumber returns little-endian
            var serialNumber = cert.GetSerialNumber(); // .net creates clone
            Array.Reverse(serialNumber);

            var certificate = new Certificate {
                RawData = cert.RawData,
                KeyHandle = key,
                IssuerPolicies = cert.IsCa() ? policies : null,
                Revoked = revoked,
                NotAfterUtc = cert.NotAfter.ToUniversalTime(),
                NotBeforeUtc = cert.NotBefore.ToUniversalTime(),
                Subject = cert.SubjectName,
                Thumbprint = cert.Thumbprint,
                Issuer = cert.IssuerName,
                SerialNumber = serialNumber,
                Extensions = new List<X509Extension>(cert.Extensions.OfType<X509Extension>())
            };

            // Set issuer serial number
            certificate.IssuerSerialNumber =
                certificate.GetAuthorityKeyIdentifierExtension()?.SerialNumber.Value;
            if (certificate.IssuerSerialNumber == null && certificate.IsSelfSigned()) {
                certificate.IssuerSerialNumber = certificate.SerialNumber.ToArray();
            }
            return certificate;
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static Certificate Clone(this Certificate certificate) {
            if (certificate == null) {
                return null;
            }
            return new Certificate {
                Extensions = certificate.Extensions?.ToList(),
                Issuer = certificate.Issuer,
                IssuerPolicies = certificate.IssuerPolicies.Clone(),
                IssuerSerialNumber = certificate.IssuerSerialNumber?.ToArray(),
                KeyHandle = certificate.KeyHandle,
                NotAfterUtc = certificate.NotAfterUtc,
                NotBeforeUtc = certificate.NotBeforeUtc,
                RawData = certificate.RawData?.ToArray(),
                Revoked = certificate.Revoked.Clone(),
                SerialNumber = certificate.SerialNumber?.ToArray(),
                Subject = certificate.Subject,
                Thumbprint = certificate.Thumbprint
            };
        }

        /// <summary>
        /// Compare
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool SameAs(this Certificate certificate, Certificate other) {
            if (certificate == null) {
                return other == null;
            }
            if (other == null) {
                return false;
            }
            if (!certificate.RawData.SequenceEqualsSafe(other.RawData)) {
                return false;
            }
            if (!certificate.IssuerPolicies.SameAs(other.IssuerPolicies)) {
                return false;
            }
            if (!certificate.Revoked.SameAs(other.Revoked)) {
                return false;
            }
            if (certificate.KeyHandle != null != (other.KeyHandle != null)) {
                return false;
            }
            if (certificate.Thumbprint == other.Thumbprint &&
                certificate.GetSerialNumber().EqualsSafe(other.GetSerialNumber()) &&
                certificate.GetIssuerSerialNumber().EqualsSafe(other.GetIssuerSerialNumber()) &&
                certificate.NotAfterUtc == other.NotAfterUtc &&
                certificate.NotBeforeUtc == other.NotBeforeUtc) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check wether the certificate can issue new certificates
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static X509Certificate2 ToX509Certificate2(this Certificate certificate) {
            return new X509Certificate2(certificate.RawData);
        }

        /// <summary>
        /// Convert to pfx buffer
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="privateKey"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static byte[] ToPfx(this Certificate certificate,
            Key privateKey, string password = null) {
            using (var cert = certificate.ToX509Certificate2()) {
                return cert.ToPfx(privateKey, password);
            }
        }

        /// <summary>
        /// Get public key
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static Key GetPublicKey(this Certificate certificate) {
            using (var cert = certificate.ToX509Certificate2()) {
                var rsa = cert.GetRSAPublicKey();
                if (rsa != null) {
                    using (rsa) {
                        return rsa.ToPublicKey();
                    }
                }
                var ecdsa = cert.GetECDsaPublicKey();
                if (ecdsa != null) {
                    using (ecdsa) {
                        return ecdsa.ToPublicKey();
                    }
                }
                // var dsa = cert.GetDSAPublicKey();
                throw new ArgumentException("Key type not supported");
            }
        }

        /// <summary>
        /// Create collection
        /// </summary>
        /// <param name="certificateCollection"></param>
        public static X509Certificate2Collection ToX509Certificate2Collection(
            this IEnumerable<Certificate> certificateCollection) {
            return new X509Certificate2Collection(certificateCollection
                .Select(c => c.ToX509Certificate2())
                .ToArray());
        }

        /// <summary>
        /// Create collection
        /// </summary>
        /// <param name="certificateCollection"></param>
        public static void Dispose(this X509Certificate2Collection certificateCollection) {
            foreach (var cert in certificateCollection.OfType<X509Certificate>().ToList()) {
                certificateCollection.Remove(cert);
                cert?.Dispose();
            }
        }

        /// <summary>
        /// Validate certificate chain
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <returns></returns>
        public static bool IsValidChain(this Certificate certificate,
            IEnumerable<Certificate> chain) {
            return IsValidChain(certificate, chain, out _);
        }

        /// <summary>
        /// Validate certificate chain
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <returns></returns>
        public static bool IsValidChain(this Certificate certificate,
            params Certificate[] chain) {
            return IsValidChain(certificate, chain, out _);
        }

        /// <summary>
        /// Validate certificate chain
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool IsValidChain(this Certificate certificate,
            IEnumerable<Certificate> chain, out X509ChainStatus[] status) {
            using (var cert = certificate.ToX509Certificate2())
            using (var validator = new X509Chain(false)) {
                var extra = chain.ToX509Certificate2Collection();
                try {
                    validator.ChainPolicy.RevocationFlag =
                        X509RevocationFlag.EntireChain;
                    validator.ChainPolicy.RevocationMode =
                        X509RevocationMode.NoCheck;
#if TRUE // TODO
                    validator.ChainPolicy.VerificationFlags =
                        X509VerificationFlags.AllowUnknownCertificateAuthority;
#endif
                    validator.ChainPolicy.ExtraStore.AddRange(extra);
                    if (!validator.Build(cert)) {
                        status = validator.ChainStatus;
                        return false;
                    }
                    status = null;
                    return true;
                }
                finally {
                    extra.Dispose();
                }
            }
        }

        /// <summary>
        /// Verify certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="issuer"></param>
        /// <returns></returns>
        public static bool HasValidSignature(this Certificate certificate,
            Certificate issuer) {
            try {
                certificate.Verify(issuer);
                return true;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Verify certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="issuer"></param>
        /// <returns></returns>
        public static void Verify(this Certificate certificate,
            Certificate issuer) {
            var cert = issuer.ToX509Certificate();
            certificate.ToX509Certificate().Verify(cert.GetPublicKey());
        }

        /// <summary>
        /// Get serial number
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static string GetSerialNumberAsString(this Certificate certificate) {
            return GetSerialNumber(certificate)?.ToString();
        }

        /// <summary>
        /// Get serial number
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static SerialNumber GetSerialNumber(this Certificate certificate) {
            if (certificate?.SerialNumber == null) {
                return null;
            }
            return new SerialNumber(certificate.SerialNumber);
        }

        /// <summary>
        /// Get serial number in little endian
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static byte[] GetSerialNumberAsBytesLE(this Certificate certificate) {
            return certificate?.SerialNumber?.Reverse().ToArray();
        }

        /// <summary>
        /// Get Subject name
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static string GetSubjectName(this Certificate certificate) {
            return certificate?.Subject?.Name;
        }

        /// <summary>
        /// Wether the certificate can issue certificates
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static bool IsIssuer(this Certificate certificate) {
            return certificate?.IssuerPolicies != null;
        }

        /// <summary>
        /// Get issuer serial number
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static string GetIssuerSerialNumberAsString(this Certificate certificate) {
            return GetIssuerSerialNumber(certificate)?.ToString();
        }

        /// <summary>
        /// Get issuer serial number
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static SerialNumber GetIssuerSerialNumber(this Certificate certificate) {
            if (certificate?.IssuerSerialNumber == null) {
                return null;
            }
            return new SerialNumber(certificate.IssuerSerialNumber);
        }

        /// <summary>
        /// Get issuer name
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static string GetIssuerSubjectName(this Certificate certificate) {
            return certificate?.Issuer?.Name;
        }

        /// <summary>
        /// Find extension by oid
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="oid"></param>
        /// <returns></returns>
        internal static X509Extension GetExtensionByOid(this Certificate certificate, string oid) {
            return certificate?.Extensions?.FirstOrDefault(e => e.Oid.Value == oid);
        }

        /// <summary>
        /// Find subject key in certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static T GetExtensionByType<T>(this Certificate certificate) {
            if (certificate?.Extensions == null) {
                return default;
            }
            return certificate.Extensions.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Find auth key extension
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static X509AuthorityKeyIdentifierExtension GetAuthorityKeyIdentifierExtension(
            this Certificate certificate) {
            var extension = certificate.GetExtensionByOid(Oids.AuthorityKeyIdentifier);
            if (extension == null) {
                extension = certificate.GetExtensionByOid(Oids.AuthorityKeyIdentifier2);
            }
            if (extension != null) {
                return new X509AuthorityKeyIdentifierExtension(extension,
                    extension.Critical);
            }
            return null;
        }

        /// <summary>
        /// Find subject alt name extension
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static X509SubjectAltNameExtension GetSubjectAltNameExtension(
            this Certificate certificate) {
            var extension = certificate.GetExtensionByOid(Oids.SubjectAltName);
            if (extension == null) {
                extension = certificate.GetExtensionByOid(Oids.SubjectAltName2);
            }
            if (extension != null) {
                return new X509SubjectAltNameExtension(extension, extension.Critical);
            }
            return null;
        }

        /// <summary>
        /// Find distribution points extension
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static X509CrlDistributionPointsExtension GetCrlDistributionPointsExtension(
            this Certificate certificate) {
            var extension = certificate.GetExtensionByOid(Oids.CrlDistributionPoints);
            if (extension != null) {
                return new X509CrlDistributionPointsExtension(extension, extension.Critical);
            }
            return null;
        }

        /// <summary>
        /// Find auth information access extension
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static X509AuthorityInformationAccessExtension GetAuthorityInformationAccessExtension(
            this Certificate certificate) {
            var extension = certificate.GetExtensionByOid(Oids.AuthorityInformationAccess);
            if (extension != null) {
                return new X509AuthorityInformationAccessExtension(extension, extension.Critical);
            }
            return null;
        }

        /// <summary>
        /// Find subject key in certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static X509SubjectKeyIdentifierExtension GetSubjectKeyIdentifierExtension(
            this Certificate certificate) {
            return certificate.GetExtensionByType<X509SubjectKeyIdentifierExtension>();
        }

        /// <summary>
        /// Find subject key in certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static X509KeyUsageExtension GetKeyUsageExtension(
            this Certificate certificate) {
            return certificate.GetExtensionByType<X509KeyUsageExtension>();
        }

        /// <summary>
        /// Get enhanced key usage extensions
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static X509EnhancedKeyUsageExtension GetEnhancedKeyUsageExtension(
            this Certificate certificate) {
            return certificate.GetExtensionByType<X509EnhancedKeyUsageExtension>();
        }

        /// <summary>
        /// Get basic constraints extension
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static X509BasicConstraintsExtension GetBasicConstraintsExtension(
            this Certificate certificate) {
            return certificate.GetExtensionByType<X509BasicConstraintsExtension>();
        }

        /// <summary>
        /// Check wether the certificate can issue new certificates
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static bool IsSelfSigned(this Certificate certificate) {
            if (certificate.IsSelfIssued()) {
                // Check signature is its own
                return HasValidSignature(certificate, certificate);
            }
            return false;
        }

        /// <summary>
        /// If the certificate was issued by itself.
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static bool IsSelfIssued(this Certificate certificate) {
            var issuerSn = certificate.IssuerSerialNumber;
            if (issuerSn != null) {
                return certificate.IssuerSerialNumber.SequenceEqualsSafe(
                    certificate.SerialNumber);
            }
            return certificate.Issuer.SameAs(certificate.Subject);
        }

        /// <summary>
        /// Check wether the certificate is a ca certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        private static bool IsCa(this X509Certificate2 certificate) {
            var bce = certificate.Extensions
                .OfType<X509BasicConstraintsExtension>()
                .FirstOrDefault();
            if (bce != null && bce.CertificateAuthority) {
                // Must also be able to sign
                return CanSign(certificate);
            }
            return false;
        }

        /// <summary>
        /// Check wether the certificate is a ca certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        private static bool CanSign(this X509Certificate2 certificate) {
            var keyUsage = certificate.Extensions
                .OfType<X509KeyUsageExtension>()
                .FirstOrDefault();
            if (keyUsage != null) {
                const X509KeyUsageFlags kSignflags =
                    X509KeyUsageFlags.KeyCertSign |
                    X509KeyUsageFlags.CrlSign;
                return kSignflags == (keyUsage.KeyUsages & kSignflags);
            }
            return false;
        }
    }
}
