// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.KeyVault
{
    public class KeyVaultCertFactory
    {
        public const int SerialNumberLength = 20;
        public const int DefaultKeySize = 2048;

        /// <summary>
        /// Creates a KeyVault signed certificate.
        /// </summary>
        /// <returns>The signed certificate</returns>
        public static Task<X509Certificate2> CreateSignedCertificate(
            string applicationUri,
            string applicationName,
            string subjectName,
            IList<String> domainNames,
            ushort keySize,
            DateTime notBefore,
            DateTime notAfter,
            ushort hashSizeInBits,
            X509Certificate2 issuerCAKeyCert,
            RSA publicKey,
            X509SignatureGenerator generator,
            bool caCert = false,
            string extensionUrl = null
            )
        {
            if (publicKey == null)
            {
                throw new NotSupportedException("Need a public key and a CA certificate.");
            }

            if (publicKey.KeySize != keySize)
            {
                throw new NotSupportedException(String.Format("Public key size {0} does not match expected key size {1}", publicKey.KeySize, keySize));
            }

            // new serial number
            byte[] serialNumber = new byte[SerialNumberLength];
            RandomNumberGenerator.Fill(serialNumber);
            serialNumber[0] &= 0x7F;

            // set default values.
            X500DistinguishedName subjectDN = SetSuitableDefaults(
                ref applicationUri,
                ref applicationName,
                ref subjectName,
                ref domainNames,
                ref keySize);

            var request = new CertificateRequest(subjectDN, publicKey, GetRSAHashAlgorithmName(hashSizeInBits), RSASignaturePadding.Pkcs1);

            // Basic constraints
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(caCert, caCert, 0, true));

            // Subject Key Identifier
            var ski = new X509SubjectKeyIdentifierExtension(
                request.PublicKey,
                X509SubjectKeyIdentifierHashAlgorithm.Sha1,
                false);
            request.CertificateExtensions.Add(ski);

            // Authority Key Identifier
            if (issuerCAKeyCert != null)
            {
                request.CertificateExtensions.Add(BuildAuthorityKeyIdentifier(issuerCAKeyCert));
            }
            else
            {
                request.CertificateExtensions.Add(BuildAuthorityKeyIdentifier(subjectDN, serialNumber.Reverse().ToArray(), ski));
            }

            if (caCert)
            {
                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                        true));

                if (extensionUrl != null)
                {
                    // add CRL endpoint, if available
                    request.CertificateExtensions.Add(
                        BuildX509CRLDistributionPoints(PatchExtensionUrl(extensionUrl, serialNumber))
                        );
                }
            }
            else
            {
                // Key Usage
                X509KeyUsageFlags defaultFlags =
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.DataEncipherment |
                        X509KeyUsageFlags.NonRepudiation | X509KeyUsageFlags.KeyEncipherment;
                if (issuerCAKeyCert == null)
                {
                    // self signed case
                    defaultFlags |= X509KeyUsageFlags.KeyCertSign;
                }
                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(defaultFlags, true));

                // Enhanced key usage
                request.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                        new OidCollection {
                        new Oid("1.3.6.1.5.5.7.3.1"),
                        new Oid("1.3.6.1.5.5.7.3.2") }, true));

                // Subject Alternative Name
                var subjectAltName = BuildSubjectAlternativeName(applicationUri, domainNames);
                request.CertificateExtensions.Add(new X509Extension(subjectAltName, false));

                if (issuerCAKeyCert != null &&
                    extensionUrl != null)
                {   // add Authority Information Access, if available
                    request.CertificateExtensions.Add(
                        BuildX509AuthorityInformationAccess(new string[] { PatchExtensionUrl(extensionUrl, issuerCAKeyCert.SerialNumber) })
                        );
                }
            }

            if (issuerCAKeyCert != null)
            {
                if (notAfter > issuerCAKeyCert.NotAfter)
                {
                    notAfter = issuerCAKeyCert.NotAfter;
                }
                if (notBefore < issuerCAKeyCert.NotBefore)
                {
                    notBefore = issuerCAKeyCert.NotBefore;
                }
            }

            var issuerSubjectName = issuerCAKeyCert != null ? issuerCAKeyCert.SubjectName : subjectDN;
            X509Certificate2 signedCert = request.Create(
                issuerSubjectName,
                generator,
                notBefore,
                notAfter,
                serialNumber
                );

            return Task.FromResult<X509Certificate2>(signedCert);
        }

        /// <summary>
        /// Revoke the certificate.
        /// The CRL number is increased by one and the new CRL is returned.
        /// </summary>
        public static X509CRL RevokeCertificate(
            X509Certificate2 issuerCertificate,
            List<X509CRL> issuerCrls,
            X509Certificate2Collection revokedCertificates,
            DateTime thisUpdate,
            DateTime nextUpdate,
            X509SignatureGenerator generator,
            uint hashSize
            )
        {
            var crlSerialNumber = Org.BouncyCastle.Math.BigInteger.Zero;
            Org.BouncyCastle.X509.X509Certificate bcCertCA =
                new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(issuerCertificate.RawData);
            Org.BouncyCastle.Crypto.ISignatureFactory signatureFactory =
                    new KeyVaultSignatureFactory(GetRSAHashAlgorithmName(hashSize), generator);

            var crlGen = new Org.BouncyCastle.X509.X509V2CrlGenerator();
            crlGen.SetIssuerDN(bcCertCA.IssuerDN);

            if (thisUpdate == DateTime.MinValue)
            {
                thisUpdate = DateTime.UtcNow;
            }
            crlGen.SetThisUpdate(thisUpdate);

            if (nextUpdate <= thisUpdate)
            {
                nextUpdate = bcCertCA.NotAfter;
            }
            crlGen.SetNextUpdate(nextUpdate);
            // merge all existing revocation list
            if (issuerCrls != null)
            {
                var parser = new Org.BouncyCastle.X509.X509CrlParser();
                foreach (X509CRL issuerCrl in issuerCrls)
                {
                    Org.BouncyCastle.X509.X509Crl crl = parser.ReadCrl(issuerCrl.RawData);
                    crlGen.AddCrl(crl);
                    var crlVersion = GetCrlNumber(crl);
                    if (crlVersion.IntValue > crlSerialNumber.IntValue)
                    {
                        crlSerialNumber = crlVersion;
                    }
                }
            }

            if (revokedCertificates == null || revokedCertificates.Count == 0)
            {
                // add a dummy revoked cert
                crlGen.AddCrlEntry(Org.BouncyCastle.Math.BigInteger.One, thisUpdate, Org.BouncyCastle.Asn1.X509.CrlReason.Unspecified);
            }
            else
            {
                // add the revoked cert
                foreach (var revokedCertificate in revokedCertificates)
                {
                    crlGen.AddCrlEntry(GetSerialNumber(revokedCertificate), thisUpdate, Org.BouncyCastle.Asn1.X509.CrlReason.PrivilegeWithdrawn);
                }
            }

            crlGen.AddExtension(Org.BouncyCastle.Asn1.X509.X509Extensions.AuthorityKeyIdentifier,
                                false,
                                new Org.BouncyCastle.X509.Extension.AuthorityKeyIdentifierStructure(bcCertCA));

            // set new serial number
            crlSerialNumber = crlSerialNumber.Add(Org.BouncyCastle.Math.BigInteger.One);
            crlGen.AddExtension(Org.BouncyCastle.Asn1.X509.X509Extensions.CrlNumber,
                                false,
                                new Org.BouncyCastle.Asn1.X509.CrlNumber(crlSerialNumber));

            // generate updated CRL
            Org.BouncyCastle.X509.X509Crl updatedCrl = crlGen.Generate(signatureFactory);

            return new X509CRL(updatedCrl.GetEncoded());
        }

        /// <summary>
        /// Get RSA public key from a CSR.
        /// </summary>
        public static RSA GetRSAPublicKey(Org.BouncyCastle.Asn1.X509.SubjectPublicKeyInfo subjectPublicKeyInfo)
        {
            Org.BouncyCastle.Crypto.AsymmetricKeyParameter asymmetricKeyParameter = Org.BouncyCastle.Security.PublicKeyFactory.CreateKey(subjectPublicKeyInfo);
            Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters rsaKeyParameters = (Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters)asymmetricKeyParameter;
            RSAParameters rsaKeyInfo = new RSAParameters
            {
                Modulus = rsaKeyParameters.Modulus.ToByteArrayUnsigned(),
                Exponent = rsaKeyParameters.Exponent.ToByteArrayUnsigned()
            };
            RSA rsa = RSA.Create(rsaKeyInfo);
            return rsa;
        }

        private static string GetRSAHashAlgorithm(uint hashSizeInBits)
        {
            if (hashSizeInBits <= 160)
            {
                return "SHA1WITHRSA";
            }

            if (hashSizeInBits <= 224)
            {
                return "SHA224WITHRSA";
            }
            else if (hashSizeInBits <= 256)
            {
                return "SHA256WITHRSA";
            }
            else if (hashSizeInBits <= 384)
            {
                return "SHA384WITHRSA";
            }
            else
            {
                return "SHA512WITHRSA";
            }
        }

        private static HashAlgorithmName GetRSAHashAlgorithmName(uint hashSizeInBits)
        {
            if (hashSizeInBits <= 160)
            {
                return HashAlgorithmName.SHA1;
            }
            else if (hashSizeInBits <= 256)
            {
                return HashAlgorithmName.SHA256;
            }
            else if (hashSizeInBits <= 384)
            {
                return HashAlgorithmName.SHA384;
            }
            else
            {
                return HashAlgorithmName.SHA512;
            }
        }


        /// <summary>
        /// Read the Crl number from a X509Crl.
        /// </summary>
        private static Org.BouncyCastle.Math.BigInteger GetCrlNumber(Org.BouncyCastle.X509.X509Crl crl)
        {
            Org.BouncyCastle.Math.BigInteger crlNumber = Org.BouncyCastle.Math.BigInteger.One;
            try
            {
                Org.BouncyCastle.Asn1.Asn1Object asn1Object = GetExtensionValue(crl, Org.BouncyCastle.Asn1.X509.X509Extensions.CrlNumber);
                if (asn1Object != null)
                {
                    crlNumber = Org.BouncyCastle.Asn1.DerInteger.GetInstance(asn1Object).PositiveValue;
                }
            }
            finally
            {
            }
            return crlNumber;
        }

        /// <summary>
        /// Get the value of an extension oid.
        /// </summary>
        private static Org.BouncyCastle.Asn1.Asn1Object GetExtensionValue(
            Org.BouncyCastle.X509.IX509Extension extension,
            Org.BouncyCastle.Asn1.DerObjectIdentifier oid)
        {
            Org.BouncyCastle.Asn1.Asn1OctetString asn1Octet = extension.GetExtensionValue(oid);
            if (asn1Octet != null)
            {
                return Org.BouncyCastle.X509.Extension.X509ExtensionUtilities.FromExtensionValue(asn1Octet);
            }
            return null;
        }

        /// <summary>
        /// Get public key parameters from a X509Certificate2
        /// </summary>
        private static Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters GetPublicKeyParameter(X509Certificate2 certificate)
        {
            using (RSA rsa = certificate.GetRSAPublicKey())
            {
                RSAParameters rsaParams = rsa.ExportParameters(false);
                return new Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters(
                    false,
                    new Org.BouncyCastle.Math.BigInteger(1, rsaParams.Modulus),
                    new Org.BouncyCastle.Math.BigInteger(1, rsaParams.Exponent));
            }
        }

        /// <summary>
        /// Get the serial number from a certificate as BigInteger.
        /// </summary>
        private static Org.BouncyCastle.Math.BigInteger GetSerialNumber(X509Certificate2 certificate)
        {
            byte[] serialNumber = certificate.GetSerialNumber();
            Array.Reverse(serialNumber);
            return new Org.BouncyCastle.Math.BigInteger(1, serialNumber);
        }

        /// <summary>
        /// Sets the parameters to suitable defaults.
        /// </summary>
        private static X500DistinguishedName SetSuitableDefaults(
            ref string applicationUri,
            ref string applicationName,
            ref string subjectName,
            ref IList<String> domainNames,
            ref ushort keySize)
        {
            // enforce recommended keysize unless lower value is enforced.
            if (keySize < 2048)
            {
                keySize = DefaultKeySize;
            }

            if (keySize % 1024 != 0)
            {
                throw new ArgumentNullException(nameof(keySize), "KeySize must be a multiple of 1024.");
            }

            // parse the subject name if specified.
            List<string> subjectNameEntries = null;

            if (!String.IsNullOrEmpty(subjectName))
            {
                subjectNameEntries = Opc.Ua.Utils.ParseDistinguishedName(subjectName);
                // enforce proper formatting for the subject name string
                subjectName = string.Join(", ", subjectNameEntries);
            }

            // check the application name.
            if (String.IsNullOrEmpty(applicationName))
            {
                if (subjectNameEntries == null)
                {
                    throw new ArgumentNullException(nameof(applicationName), "Must specify a applicationName or a subjectName.");
                }

                // use the common name as the application name.
                for (int ii = 0; ii < subjectNameEntries.Count; ii++)
                {
                    if (subjectNameEntries[ii].StartsWith("CN=", StringComparison.InvariantCulture))
                    {
                        applicationName = subjectNameEntries[ii].Substring(3).Trim();
                        break;
                    }
                }
            }

            if (String.IsNullOrEmpty(applicationName))
            {
                throw new ArgumentNullException(nameof(applicationName), "Must specify a applicationName or a subjectName.");
            }

            // remove special characters from name.
            StringBuilder buffer = new StringBuilder();

            for (int ii = 0; ii < applicationName.Length; ii++)
            {
                char ch = applicationName[ii];

                if (Char.IsControl(ch) || ch == '/' || ch == ',' || ch == ';')
                {
                    ch = '+';
                }

                buffer.Append(ch);
            }

            applicationName = buffer.ToString();

            // ensure at least one host name.
            if (domainNames == null || domainNames.Count == 0)
            {
                domainNames = new List<string>
                {
                    Opc.Ua.Utils.GetHostName()
                };
            }

            // create the application uri.
            if (String.IsNullOrEmpty(applicationUri))
            {
                StringBuilder builder = new StringBuilder();

                builder.Append("urn:");
                builder.Append(domainNames[0]);
                builder.Append(":");
                builder.Append(applicationName);

                applicationUri = builder.ToString();
            }

            Uri uri = Opc.Ua.Utils.ParseUri(applicationUri);

            if (uri == null)
            {
                throw new ArgumentNullException(nameof(applicationUri), "Must specify a valid URL.");
            }

            // create the subject name,
            if (String.IsNullOrEmpty(subjectName))
            {
                subjectName = Opc.Ua.Utils.Format("CN={0}", applicationName);
            }

            if (!subjectName.Contains("CN="))
            {
                subjectName = Opc.Ua.Utils.Format("CN={0}", subjectName);
            }

            if (domainNames != null && domainNames.Count > 0)
            {
                if (!subjectName.Contains("DC=") && !subjectName.Contains("="))
                {
                    subjectName += Opc.Ua.Utils.Format(", DC={0}", domainNames[0]);
                }
                else
                {
                    subjectName = Opc.Ua.Utils.ReplaceDCLocalhost(subjectName, domainNames[0]);
                }
            }

            return new X500DistinguishedName(subjectName);
        }

        /// <summary>
        /// Build the Subject Alternative name extension (for OPC UA application certs)
        /// </summary>
        /// <param name="applicationUri">The application Uri</param>
        /// <param name="domainNames">The domain names. DNS Hostnames, IPv4 or IPv6 addresses</param>
        private static X509Extension BuildSubjectAlternativeName(string applicationUri, IList<string> domainNames)
        {
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddUri(new Uri(applicationUri));
            foreach (string domainName in domainNames)
            {
                IPAddress ipAddr;
                if (String.IsNullOrWhiteSpace(domainName))
                {
                    continue;
                }
                if (IPAddress.TryParse(domainName, out ipAddr))
                {
                    sanBuilder.AddIpAddress(ipAddr);
                }
                else
                {
                    sanBuilder.AddDnsName(domainName);
                }
            }

            return sanBuilder.Build();
        }

        /// <summary>
        /// Convert a hex string to a byte array.
        /// </summary>
        /// <param name="hexString">The hex string</param>
        internal static byte[] HexToByteArray(string hexString)
        {
            byte[] bytes = new byte[hexString.Length / 2];

            for (int i = 0; i < hexString.Length; i += 2)
            {
                string s = hexString.Substring(i, 2);
                bytes[i / 2] = byte.Parse(s, System.Globalization.NumberStyles.HexNumber, null);
            }

            return bytes;
        }

        /// <summary>
        /// Build the Authority Key Identifier from an Issuer CA certificate.
        /// </summary>
        /// <param name="issuerCaCertificate">The issuer CA certificate</param>
        private static X509Extension BuildAuthorityKeyIdentifier(X509Certificate2 issuerCaCertificate)
        {
            // force exception if SKI is not present
            var ski = issuerCaCertificate.Extensions.OfType<X509SubjectKeyIdentifierExtension>().Single();
            return BuildAuthorityKeyIdentifier(issuerCaCertificate.SubjectName, issuerCaCertificate.GetSerialNumber(), ski);
        }

        /// <summary>
        /// Build the CRL Distribution Point extension.
        /// </summary>
        /// <param name="distributionPoint">The CRL distribution point</param>
        private static X509Extension BuildX509CRLDistributionPoints(
            string distributionPoint
            )
        {
            var context0 = new Asn1Tag(TagClass.ContextSpecific, 0, true);
            Asn1Tag distributionPointChoice = context0;
            Asn1Tag fullNameChoice = context0;
            Asn1Tag generalNameUriChoice = new Asn1Tag(TagClass.ContextSpecific, 6);

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.PushSequence();
                writer.PushSequence();
                writer.PushSequence(distributionPointChoice);
                writer.PushSequence(fullNameChoice);
                writer.WriteCharacterString(
                    generalNameUriChoice,
                    UniversalTagNumber.IA5String,
                    distributionPoint);
                writer.PopSequence(fullNameChoice);
                writer.PopSequence(distributionPointChoice);
                writer.PopSequence();
                writer.PopSequence();
                return new X509Extension("2.5.29.31", writer.Encode(), false);
            }
        }

        /// <summary>
        /// Build the Authority information Access extension.
        /// </summary>
        /// <param name="caIssuerUrls">Array of CA Issuer Urls</param>
        /// <param name="ocspResponder">optional, the OCSP responder </param>
        private static X509Extension BuildX509AuthorityInformationAccess(
            string[] caIssuerUrls,
            string ocspResponder = null
            )
        {
            if (String.IsNullOrEmpty(ocspResponder) &&
               (caIssuerUrls == null ||
               (caIssuerUrls != null && caIssuerUrls.Length == 0)))
            {
                throw new ArgumentNullException(nameof(caIssuerUrls), "One CA Issuer Url or OCSP responder is required for the extension.");
            }

            var context0 = new Asn1Tag(TagClass.ContextSpecific, 0, true);
            Asn1Tag generalNameUriChoice = new Asn1Tag(TagClass.ContextSpecific, 6);
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.PushSequence();
                if (caIssuerUrls != null)
                {
                    foreach (var caIssuerUrl in caIssuerUrls)
                    {
                        writer.PushSequence();
                        writer.WriteObjectIdentifier("1.3.6.1.5.5.7.48.2");
                        writer.WriteCharacterString(
                            generalNameUriChoice,
                            UniversalTagNumber.IA5String,
                            caIssuerUrl);
                        writer.PopSequence();
                    }
                }
                if (!String.IsNullOrEmpty(ocspResponder))
                {
                    writer.PushSequence();
                    writer.WriteObjectIdentifier("1.3.6.1.5.5.7.48.1");
                    writer.WriteCharacterString(
                        generalNameUriChoice,
                        UniversalTagNumber.IA5String,
                        ocspResponder);
                    writer.PopSequence();
                }
                writer.PopSequence();
                return new X509Extension("1.3.6.1.5.5.7.1.1", writer.Encode(), false);
            }
        }

        /// <summary>
        /// Build the X509 Authority Key extension.
        /// </summary>
        /// <param name="issuerName">The distinguished name of the issuer</param>
        /// <param name="issuerSerialNumber">The serial number of the issuer</param>
        /// <param name="ski">The subject key identifier extension to use</param>
        private static X509Extension BuildAuthorityKeyIdentifier(
            X500DistinguishedName issuerName,
            byte[] issuerSerialNumber,
            X509SubjectKeyIdentifierExtension ski
            )
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.PushSequence();

                if (ski != null)
                {
                    Asn1Tag keyIdTag = new Asn1Tag(TagClass.ContextSpecific, 0);
                    writer.WriteOctetString(keyIdTag, HexToByteArray(ski.SubjectKeyIdentifier));
                }

                Asn1Tag issuerNameTag = new Asn1Tag(TagClass.ContextSpecific, 1);
                writer.PushSequence(issuerNameTag);

                // Add the tag to constructed context-specific 4 (GeneralName.directoryName)
                Asn1Tag directoryNameTag = new Asn1Tag(TagClass.ContextSpecific, 4, true);
                writer.PushSetOf(directoryNameTag);
                byte[] issuerNameRaw = issuerName.RawData;
                writer.WriteEncodedValue(issuerNameRaw);
                writer.PopSetOf(directoryNameTag);
                writer.PopSequence(issuerNameTag);

                Asn1Tag issuerSerialTag = new Asn1Tag(TagClass.ContextSpecific, 2);
                System.Numerics.BigInteger issuerSerial = new System.Numerics.BigInteger(issuerSerialNumber);
                writer.WriteInteger(issuerSerialTag, issuerSerial);

                writer.PopSequence();
                return new X509Extension("2.5.29.35", writer.Encode(), false);
            }
        }

        /// <summary>
        /// Patch serial number in a Url. byte version.
        /// </summary>
        private static string PatchExtensionUrl(string extensionUrl, byte[] serialNumber)
        {
            string serial = BitConverter.ToString(serialNumber).Replace("-", "");
            return PatchExtensionUrl(extensionUrl, serial);
        }

        /// <summary>
        /// Patch serial number in a Url. string version.
        /// </summary>
        private static string PatchExtensionUrl(string extensionUrl, string serial)
        {
            return extensionUrl.Replace("%serial%", serial.ToLower());
        }

    }
    /// <summary>
    /// The X509 signature generator to sign a digest with a KeyVault key.
    /// </summary>
    public class KeyVaultSignatureGenerator : X509SignatureGenerator
    {
        private X509Certificate2 _issuerCert;
        private KeyVaultServiceClient _keyVaultServiceClient;
        private readonly string _signingKey;

        /// <summary>
        /// Create the KeyVault signature generator.
        /// </summary>
        /// <param name="keyVaultServiceClient">The KeyVault service client to use</param>
        /// <param name="signingKey">The KeyVault signing key</param>
        /// <param name="issuerCertificate">The issuer certificate used for signing</param>
        public KeyVaultSignatureGenerator(
            KeyVaultServiceClient keyVaultServiceClient,
            string signingKey,
            X509Certificate2 issuerCertificate)
        {
            _issuerCert = issuerCertificate;
            _keyVaultServiceClient = keyVaultServiceClient;
            _signingKey = signingKey;
        }

        /// <summary>
        /// Callback to sign a digest with KeyVault key.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="hashAlgorithm"></param>
        /// <returns></returns>
        public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            HashAlgorithm hash;
            if (hashAlgorithm == HashAlgorithmName.SHA256)
            {
                hash = SHA256.Create();
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA384)
            {
                hash = SHA384.Create();
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA512)
            {
                hash = SHA512.Create();
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(hashAlgorithm), "The hash algorithm " + hashAlgorithm.Name + " is not supported.");
            }
            var digest = hash.ComputeHash(data);
            var resultKeyVaultPkcs = _keyVaultServiceClient.SignDigestAsync(_signingKey, digest, hashAlgorithm, RSASignaturePadding.Pkcs1).GetAwaiter().GetResult();
#if TESTANDVERIFYTHEKEYVAULTSIGNER
                // for test and dev only, verify the KeyVault signer acts identical to the internal signer
                if (_issuerCert.HasPrivateKey)
                {
                    var resultKeyVaultPss = _keyVaultServiceClient.SignDigestAsync(_signingKey, digest, hashAlgorithm, RSASignaturePadding.Pss).GetAwaiter().GetResult();
                    var resultLocalPkcs = _issuerCert.GetRSAPrivateKey().SignData(data, hashAlgorithm, RSASignaturePadding.Pkcs1);
                    var resultLocalPss = _issuerCert.GetRSAPrivateKey().SignData(data, hashAlgorithm, RSASignaturePadding.Pss);
                    for (int i = 0; i < resultKeyVaultPkcs.Length; i++)
                    {
                        if (resultKeyVaultPkcs[i] != resultLocalPkcs[i])
                        {
                            Debug.WriteLine("{0} != {1}", resultKeyVaultPkcs[i], resultLocalPkcs[i]);
                        }
                    }
                    for (int i = 0; i < resultKeyVaultPss.Length; i++)
                    {
                        if (resultKeyVaultPss[i] != resultLocalPss[i])
                        {
                            Debug.WriteLine("{0} != {1}", resultKeyVaultPss[i], resultLocalPss[i]);
                        }
                    }
                }
#endif
            return resultKeyVaultPkcs;
        }

        protected override PublicKey BuildPublicKey()
        {
            return _issuerCert.PublicKey;
        }

        internal static PublicKey BuildPublicKey(RSA rsa)
        {
            if (rsa == null)
            {
                throw new ArgumentNullException(nameof(rsa));
            }
            // function is never called
            return null;
        }

        public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
        {
            byte[] oidSequence;

            if (hashAlgorithm == HashAlgorithmName.SHA256)
            {
                //const string RsaPkcs1Sha256 = "1.2.840.113549.1.1.11";
                oidSequence = new byte[] { 48, 13, 6, 9, 42, 134, 72, 134, 247, 13, 1, 1, 11, 5, 0 };
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA384)
            {
                //const string RsaPkcs1Sha384 = "1.2.840.113549.1.1.12";
                oidSequence = new byte[] { 48, 13, 6, 9, 42, 134, 72, 134, 247, 13, 1, 1, 12, 5, 0 };
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA512)
            {
                //const string RsaPkcs1Sha512 = "1.2.840.113549.1.1.13";
                oidSequence = new byte[] { 48, 13, 6, 9, 42, 134, 72, 134, 247, 13, 1, 1, 13, 5, 0 };
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(hashAlgorithm), "The hash algorithm " + hashAlgorithm.Name + " is not supported.");
            }
            return oidSequence;
        }
    }

    /// <summary>
    /// The signature factory for Bouncy Castle to sign a digest with a KeyVault key.
    /// </summary>
    public class KeyVaultSignatureFactory : Org.BouncyCastle.Crypto.ISignatureFactory
    {
        private readonly Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier _algID;
        private readonly HashAlgorithmName _hashAlgorithm;
        private readonly X509SignatureGenerator _generator;

        /// <summary>
        /// Constructor which also specifies a source of randomness to be used if one is required.
        /// </summary>
        /// <param name="hashAlgorithm">The name of the signature algorithm to use.</param>
        /// <param name="generator">The signature generator.</param>
        public KeyVaultSignatureFactory(HashAlgorithmName hashAlgorithm, X509SignatureGenerator generator)
        {
            Org.BouncyCastle.Asn1.DerObjectIdentifier sigOid;
            if (hashAlgorithm == HashAlgorithmName.SHA256)
            {
                sigOid = Org.BouncyCastle.Asn1.Pkcs.PkcsObjectIdentifiers.Sha256WithRsaEncryption;
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA384)
            {
                sigOid = Org.BouncyCastle.Asn1.Pkcs.PkcsObjectIdentifiers.Sha384WithRsaEncryption;
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA512)
            {
                sigOid = Org.BouncyCastle.Asn1.Pkcs.PkcsObjectIdentifiers.Sha512WithRsaEncryption;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(hashAlgorithm));
            }
            _hashAlgorithm = hashAlgorithm;
            _generator = generator;
            _algID = new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(sigOid);
        }

        /// <inheritdoc/>
        public Object AlgorithmDetails => _algID;

        /// <inheritdoc/>
        public Org.BouncyCastle.Crypto.IStreamCalculator CreateCalculator()
        {
            return new KeyVaultStreamCalculator(_generator, _hashAlgorithm);
        }
    }

    /// <summary>
    /// Signs a Bouncy Castle digest stream with the .Net X509SignatureGenerator.
    /// </summary>
    public class KeyVaultStreamCalculator : Org.BouncyCastle.Crypto.IStreamCalculator
    {
        private X509SignatureGenerator _generator;
        private readonly HashAlgorithmName _hashAlgorithm;

        /// <summary>
        /// Ctor for the stream calculator. 
        /// </summary>
        /// <param name="generator">The X509SignatureGenerator to sign the digest.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use for the signature.</param>
        public KeyVaultStreamCalculator(
            X509SignatureGenerator generator,
            HashAlgorithmName hashAlgorithm)
        {
            Stream = new MemoryStream();
            _generator = generator;
            _hashAlgorithm = hashAlgorithm;
        }

        /// <summary>
        /// The digest stream (MemoryStream).
        /// </summary>
        public Stream Stream { get; }

        /// <summary>
        /// Callback signs the digest with X509SignatureGenerator.
        /// </summary>
        public object GetResult()
        {
            var memStream = Stream as MemoryStream;
            var digest = memStream.ToArray();
            var signature = _generator.SignData(digest, _hashAlgorithm);
            return new MemoryBlockResult(signature);
        }
    }

    /// <summary>
    /// Helper for Bouncy Castle signing operation to store the result in a memory block.
    /// </summary>
    public class MemoryBlockResult : Org.BouncyCastle.Crypto.IBlockResult
    {
        private readonly byte[] _data;
        /// <inheritdoc/>
        public MemoryBlockResult(byte[] data)
        {
            _data = data;
        }
        /// <inheritdoc/>
        public byte[] Collect()
        {
            return _data;
        }
        /// <inheritdoc/>
        public int Collect(byte[] destination, int offset)
        {
            throw new NotImplementedException();
        }
    }
}
