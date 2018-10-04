// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Opc.Ua;
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

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.KeyVault
{
    public class KeyVaultCertFactory
    {
        /// <summary>
        /// Creates a CA signed certificate.
        /// </summary>
        /// <returns>The signed certificate</returns>
        public static Task<X509Certificate2> CreateSignedCertificate(
            string applicationUri,
            string applicationName,
            string subjectName,
            IList<String> domainNames,
            ushort keySize,
            DateTime startTime,
            ushort lifetimeInMonths,
            ushort hashSizeInBits,
            X509Certificate2 issuerCAKeyCert,
            RSA publicKey,
            X509SignatureGenerator generator
            )
        {
            if (publicKey == null || issuerCAKeyCert == null)
            {
                throw new NotSupportedException("Need a public key and a CA certificate.");
            }

            if (publicKey.KeySize != keySize)
            {
                throw new NotSupportedException(String.Format("Public key size {0} does not match expected key size {1}", publicKey.KeySize, keySize));
            }

            // set default values.
            X500DistinguishedName subjectDN = SetSuitableDefaults(
                ref applicationUri,
                ref applicationName,
                ref subjectName,
                ref domainNames,
                ref keySize,
                ref lifetimeInMonths);

            var request = new CertificateRequest(subjectDN, publicKey, GetRSAHashAlgorithmName(hashSizeInBits), RSASignaturePadding.Pkcs1);

            // Basic constraints
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, true));

            // Subject Key Identifier
            request.CertificateExtensions.Add(
                            new X509SubjectKeyIdentifierExtension(
                                request.PublicKey,
                                X509SubjectKeyIdentifierHashAlgorithm.Sha1,
                                false));

            // Authority Key Identifier
            request.CertificateExtensions.Add(BuildAuthorityKeyIdentifier(issuerCAKeyCert));

            // Key Usage
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyCertSign |
                    X509KeyUsageFlags.NonRepudiation | X509KeyUsageFlags.KeyEncipherment, true));

            // Enhanced key usage
            request.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection {
                        new Oid("1.3.6.1.5.5.7.3.1"),
                        new Oid("1.3.6.1.5.5.7.3.2") }, true));

            // Subject Alternative Name
            var subjectAltName = BuildSubjectAlternativeName(applicationUri, domainNames);
            request.CertificateExtensions.Add(new X509Extension(subjectAltName, false));

            byte[] serialNumber = new byte[20];
            RandomNumberGenerator.Fill(serialNumber);
            serialNumber[0] &= 0x7F;

            DateTime notAfter = startTime.AddMonths(lifetimeInMonths);
            if (notAfter > issuerCAKeyCert.NotAfter)
            {
                notAfter = issuerCAKeyCert.NotAfter;
            }

            X509Certificate2 signedCert = request.Create(
                issuerCAKeyCert.SubjectName,
                generator,
                startTime,
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

            DateTime now = DateTime.UtcNow;
            DateTime nextUpdate = now.AddMonths(12);
            if (nextUpdate > bcCertCA.NotAfter)
            {
                nextUpdate = bcCertCA.NotAfter;
            }
            crlGen.SetThisUpdate(now);
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
                crlGen.AddCrlEntry(Org.BouncyCastle.Math.BigInteger.One, now, Org.BouncyCastle.Asn1.X509.CrlReason.Unspecified);
            }
            else
            {
                // add the revoked cert
                foreach (var revokedCertificate in revokedCertificates)
                {
                    crlGen.AddCrlEntry(GetSerialNumber(revokedCertificate), now, Org.BouncyCastle.Asn1.X509.CrlReason.PrivilegeWithdrawn);
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

        private static string GetRSAHashAlgorithm(uint hashSizeInBits)
        {
            if (hashSizeInBits <= 160)
                return "SHA1WITHRSA";
            if (hashSizeInBits <= 224)
                return "SHA224WITHRSA";
            else if (hashSizeInBits <= 256)
                return "SHA256WITHRSA";
            else if (hashSizeInBits <= 384)
                return "SHA384WITHRSA";
            else
                return "SHA512WITHRSA";
        }

        private static HashAlgorithmName GetRSAHashAlgorithmName(uint hashSizeInBits)
        {
            if (hashSizeInBits <= 160)
                return HashAlgorithmName.SHA1;
            else if (hashSizeInBits <= 256)
                return HashAlgorithmName.SHA256;
            else if (hashSizeInBits <= 384)
                return HashAlgorithmName.SHA384;
            else
                return HashAlgorithmName.SHA512;
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
            RSA rsa = null;
            try
            {
                rsa = certificate.GetRSAPublicKey();
                RSAParameters rsaParams = rsa.ExportParameters(false);
                return new Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters(
                    false,
                    new Org.BouncyCastle.Math.BigInteger(1, rsaParams.Modulus),
                    new Org.BouncyCastle.Math.BigInteger(1, rsaParams.Exponent));
            }
            finally
            {
                RsaUtils.RSADispose(rsa);
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
            ref ushort keySize,
            ref ushort lifetimeInMonths)
        {
            // enforce recommended keysize unless lower value is enforced.
            if (keySize < 1024)
            {
                keySize = CertificateFactory.defaultKeySize;
            }

            if (keySize % 1024 != 0)
            {
                throw new ArgumentNullException(nameof(keySize), "KeySize must be a multiple of 1024.");
            }

            // enforce minimum lifetime.
            if (lifetimeInMonths < 1)
            {
                lifetimeInMonths = 1;
            }

            // parse the subject name if specified.
            List<string> subjectNameEntries = null;

            if (!String.IsNullOrEmpty(subjectName))
            {
                subjectNameEntries = Opc.Ua.Utils.ParseDistinguishedName(subjectName);
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
                domainNames = new List<string>();
                domainNames.Add(Opc.Ua.Utils.GetHostName());
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

        private static X509Extension BuildSubjectAlternativeName(string applicationUri, IList<string> domainNames)
        {
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddUri(new Uri(applicationUri));
            foreach (string domainName in domainNames)
            {
                IPAddress ipAddr;
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

        private static X509Extension BuildAuthorityKeyIdentifier(X509Certificate2 issuer)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.PushSequence();

                // force exception if SKI is not present
                var ski = issuer.Extensions.OfType<X509SubjectKeyIdentifierExtension>().Single();
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
                byte[] issuerName = issuer.SubjectName.RawData;
                writer.WriteEncodedValue(issuerName);
                writer.PopSetOf(directoryNameTag);
                writer.PopSequence(issuerNameTag);

                Asn1Tag issuerSerialTag = new Asn1Tag(TagClass.ContextSpecific, 2);
                System.Numerics.BigInteger issuerSerial = new System.Numerics.BigInteger(issuer.GetSerialNumber());
                writer.WriteInteger(issuerSerialTag, issuerSerial);

                writer.PopSequence();
                return new X509Extension("2.5.29.35", writer.Encode(), false);
            }
        }

        public class KeyVaultSignatureGenerator : X509SignatureGenerator
        {
            X509Certificate2 _issuerCert;
            KeyVaultServiceClient _keyVaultServiceClient;
            string _signingKey;

            public KeyVaultSignatureGenerator(
                KeyVaultServiceClient keyVaultServiceClient,
                string signingKey,
                X509Certificate2 issuerCertificate)
            {
                _issuerCert = issuerCertificate;
                _keyVaultServiceClient = keyVaultServiceClient;
                _signingKey = signingKey;
            }

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
                    throw new ArgumentOutOfRangeException(nameof(hashAlgorithm));
                }
                var digest = hash.ComputeHash(data);
                var resultKeyVaultPkcs = _keyVaultServiceClient.SignDigestAsync(_signingKey, digest, hashAlgorithm, RSASignaturePadding.Pkcs1).GetAwaiter().GetResult();
#if TESTANDVERIFYTHEKEYVAULTSIGNER
                // for testing only
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
                if (rsa == null) {
                    throw new ArgumentNullException(nameof(rsa));
                }

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
                    throw new ArgumentOutOfRangeException(nameof(hashAlgorithm));
                }
                return oidSequence;
            }
        }
    }

    public class KeyVaultSignatureFactory : Org.BouncyCastle.Crypto.ISignatureFactory
    {
        private readonly Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier algID;
        private readonly HashAlgorithmName hashAlgorithm;
        private X509SignatureGenerator generator;

        /// <summary>
        /// Constructor which also specifies a source of randomness to be used if one is required.
        /// </summary>
        /// <param name="hashAlgorithm">The name of the signature algorithm to use.</param>
        /// <param name="generator"></param>
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
            this.hashAlgorithm = hashAlgorithm;
            this.generator = generator;
            this.algID = new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(sigOid);
        }

        /// <inheritdoc/>
        public Object AlgorithmDetails
        {
            get { return this.algID; }
        }

        public Org.BouncyCastle.Crypto.IStreamCalculator CreateCalculator()
        {
            return new KeyVaultStreamCalculator(generator, hashAlgorithm);
        }
    }

    public class KeyVaultStreamCalculator : Org.BouncyCastle.Crypto.IStreamCalculator
    {
        private X509SignatureGenerator generator;
        private HashAlgorithmName hashAlgorithm;

        public KeyVaultStreamCalculator(
            X509SignatureGenerator generator,
            HashAlgorithmName hashAlgorithm)
        {
            Stream = new MemoryStream();
            this.generator = generator;
            this.hashAlgorithm = hashAlgorithm;
        }

        public Stream Stream { get; }

        public object GetResult()
        {
            var memStream = Stream as MemoryStream;
            var digest = memStream.ToArray();
            var signature = generator.SignData(digest, hashAlgorithm);
            return new MemoryBlockResult(signature);
        }
    }

    /// <inheritdoc/>
    public class MemoryBlockResult : Org.BouncyCastle.Crypto.IBlockResult
    {
        private byte[] data;
        /// <inheritdoc/>
        public MemoryBlockResult(byte[] data)
        {
            this.data = data;
        }
        /// <inheritdoc/>
        public byte[] Collect()
        {
            return data;
        }
        /// <inheritdoc/>
        public int Collect(byte[] destination, int offset)
        {
            throw new NotImplementedException();
        }
    }

    // TODO: replace with asnreader/writer
    public class ASN1Decoder //: IDisposable
    {
        internal enum DerTag : byte
        {
            Boolean = 0x01,
            Integer = 0x02,
            BitString = 0x03,
            OctetString = 0x04,
            Null = 0x05,
            ObjectIdentifier = 0x06,
            UTF8String = 0x0C,
            Sequence = 0x10,
            Set = 0x11,
            PrintableString = 0x13,
            T61String = 0x14,
            IA5String = 0x16,
            UTCTime = 0x17,
            GeneralizedTime = 0x18,
            BMPString = 0x1E,
        }

        private BinaryReader _reader;

        public ASN1Decoder(byte[] asn1Blob)
        {
            var stream = new MemoryStream(asn1Blob);
            _reader = new BinaryReader(stream);
        }

        public ASN1Decoder(Stream asn1Stream)
        {
            _reader = new BinaryReader(asn1Stream);
        }

        public RSACryptoServiceProvider GetRSAPublicKey()
        {
            var oidRSAEncryption = Org.BouncyCastle.Asn1.Pkcs.PkcsObjectIdentifiers.RsaEncryption.GetDerEncoded().Skip(2);

            int headerSize = ReadASN1HeaderLength();
            int identifierSize = ReadASN1HeaderLength();

            if (ReadByte() == (byte)DerTag.ObjectIdentifier)
            {
                int oidLength = ReadASN1HeaderLength(false);
                byte[] oidBytes = new byte[oidLength];
                _reader.Read(oidBytes, 0, oidBytes.Length);
                if (!oidBytes.SequenceEqual(oidRSAEncryption))
                {
                    throw new CryptographicException("No RSA Encryption key.");
                }
                int remainingBytes = identifierSize - 2 - oidBytes.Length;
                _reader.ReadBytes(remainingBytes);
            }

            if (ReadByte() == (byte)DerTag.BitString)
            {
                ReadASN1HeaderLength(false);
                _reader.ReadByte();
                ReadASN1HeaderLength();
                if (_reader.ReadByte() == (byte)DerTag.Integer)
                {
                    int modulusSize = ReadASN1HeaderLength(false);
                    byte modulus0 = ReadByte();
                    if (modulus0 == 0)
                    {
                        modulusSize--;
                    }
                    byte[] modulus = new byte[modulusSize];
                    if (modulus0 != 0)
                    {
                        modulus[0] = modulus0;
                        _reader.Read(modulus, 1, modulus.Length - 1);
                    }
                    else
                    {
                        _reader.Read(modulus, 0, modulus.Length);
                    }

                    if (ReadByte() == (byte)DerTag.Integer)
                    {
                        int exponentSize = ReadASN1HeaderLength(false);
                        byte[] exponent = new byte[exponentSize];
                        _reader.Read(exponent, 0, exponent.Length);

                        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                        RSAParameters rsaKeyInfo = new RSAParameters();
                        rsaKeyInfo.Modulus = modulus;
                        rsaKeyInfo.Exponent = exponent;
                        rsa.ImportParameters(rsaKeyInfo);
                        return rsa;
                    }
                }
            }
            throw new CryptographicException("Invalid RSA key.");
        }

        private byte ReadByte()
        {
            return _reader.ReadByte();
        }

        private int ReadASN1HeaderLength(bool testHeader = true)
        {
            if (testHeader)
            {
                if (ReadByte() != 0x30)
                {
                    throw new CryptographicException("ASN.1 Header not found");
                }
            }
            int length = ReadByte();
            if ((length & 0x80) != 0)
            {
                const int maxBytes = 4;
                int count = length & 0x0f;
                byte[] lengthBytes = new byte[maxBytes];
                _reader.Read(lengthBytes, maxBytes - count, count);
                Array.Reverse(lengthBytes);
                length = BitConverter.ToInt32(lengthBytes, 0);
            }
            return length;
        }
    }
}
