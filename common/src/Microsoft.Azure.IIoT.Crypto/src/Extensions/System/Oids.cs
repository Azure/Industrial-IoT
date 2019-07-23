// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Security.Cryptography.X509Certificates {

    /// <summary>
    /// Standard oids
    /// </summary>
    public static class Oids {

        // Cert Extensions
        internal const string AuthorityInformationAccess = "1.3.6.1.5.5.7.1.1";
        internal const string AuthorityKeyIdentifier = "2.5.29.1";
        internal const string AuthorityKeyIdentifier2 = "2.5.29.35";
        internal const string SubjectAltName = "2.5.29.7";
        internal const string SubjectAltName2 = "2.5.29.17";
        internal const string BasicConstraints = "2.5.29.10";
        internal const string SubjectKeyIdentifier = "2.5.29.14";
        internal const string KeyUsage = "2.5.29.15";
        internal const string IssuerAltName = "2.5.29.18";
        internal const string BasicConstraints2 = "2.5.29.19";
        internal const string CrlDistributionPoints = "2.5.29.31";
        internal const string CertPolicies = "2.5.29.32";
        internal const string AnyCertPolicy = "2.5.29.32.0";
        internal const string CertPolicyMappings = "2.5.29.33";
        internal const string CertPolicyConstraints = "2.5.29.36";
        internal const string EnhancedKeyUsage = "2.5.29.37";
        internal const string InhibitAnyPolicyExtension = "2.5.29.54";

        internal const string Sha256 = "2.16.840.1.101.3.4.2.1";
        internal const string Sha384 = "2.16.840.1.101.3.4.2.2";
        internal const string Sha512 = "2.16.840.1.101.3.4.2.3";

        // Elliptic Curve curve identifiers
        internal const string Secp256r1 = "1.2.840.10045.3.1.7";
        internal const string Secp384r1 = "1.3.132.0.34";
        internal const string Secp521r1 = "1.3.132.0.35";
        internal const string Secp521k1 = "1.3.132.0.10";

        // Symmetric encryption algorithms
        internal const string Rc2Cbc = "1.2.840.113549.3.2";
        internal const string Rc4 = "1.2.840.113549.3.4";
        internal const string TripleDesCbc = "1.2.840.113549.3.7";
        internal const string DesCbc = "1.3.14.3.2.7";
        internal const string Aes128Cbc = "2.16.840.1.101.3.4.1.2";
        internal const string Aes192Cbc = "2.16.840.1.101.3.4.1.22";
        internal const string Aes256Cbc = "2.16.840.1.101.3.4.1.42";

        // Asymmetric encryption algorithms
        internal const string Dsa = "1.2.840.10040.4.1";
        internal const string Rsa = "1.2.840.113549.1.1.1";
        internal const string RsaOaep = "1.2.840.113549.1.1.7";
        internal const string RsaPss = "1.2.840.113549.1.1.10";
        internal const string RsaPkcs1Sha1 = "1.2.840.113549.1.1.5";
        internal const string RsaPkcs1Sha256 = "1.2.840.113549.1.1.11";
        internal const string RsaPkcs1Sha384 = "1.2.840.113549.1.1.12";
        internal const string RsaPkcs1Sha512 = "1.2.840.113549.1.1.13";
        internal const string Esdh = "1.2.840.113549.1.9.16.3.5";
        internal const string EcDiffieHellman = "1.3.132.1.12";
        internal const string DiffieHellman = "1.2.840.10046.2.1";
        internal const string DiffieHellmanPkcs3 = "1.2.840.113549.1.3.1";

        // DSA CMS uses the combined signature+digest OID
        internal const string DsaWithSha1 = "1.2.840.10040.4.3";
        internal const string DsaWithSha256 = "2.16.840.1.101.3.4.3.2";
        internal const string DsaWithSha384 = "2.16.840.1.101.3.4.3.3";
        internal const string DsaWithSha512 = "2.16.840.1.101.3.4.3.4";

        // ECDSA CMS uses the combined signature+digest OID
        // https://tools.ietf.org/html/rfc5753#section-2.1.1
        internal const string EcPublicKey = "1.2.840.10045.2.1";
        internal const string ECDsaWithSha1 = "1.2.840.10045.4.1";
        internal const string ECDsaWithSha256 = "1.2.840.10045.4.3.2";
        internal const string ECDsaWithSha384 = "1.2.840.10045.4.3.3";
        internal const string ECDsaWithSha512 = "1.2.840.10045.4.3.4";

        internal const string Mgf1 = "1.2.840.113549.1.1.8";
        internal const string PSpecified = "1.2.840.113549.1.1.9";
    }
}
