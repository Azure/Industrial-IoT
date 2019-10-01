using Opc.Ua;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace OpcPublisher
{
    public static class SecureIoTHubToken
    {
        /// <summary>
        /// Returns the token from the cert in the given cert store.
        /// </summary>
        public static async Task<string> ReadAsync(string name, string storeType, string storePath)
        {
            string token = null;

            // handle each store type differently
            switch (storeType)
            {
                case CertificateStoreType.Directory:
                    {
                        // search a non expired cert with the given subject in the directory cert store and return the token
                        using (DirectoryCertificateStore store = new DirectoryCertificateStore())
                        {
                            store.Open(storePath);
                            X509CertificateCollection certificates = await store.Enumerate().ConfigureAwait(false);

                            foreach (X509Certificate2 cert in certificates)
                            {
                                if ((token = CheckForToken(cert, name)) != null)
                                {
                                    return token;
                                }
                            }
                        }
                        break;
                    }

                case CertificateStoreType.X509Store:
                    {
                        // search a non expired cert with the given subject in the X509 cert store and return the token
                        using (X509Store store = new X509Store(storePath, StoreLocation.CurrentUser))
                        {
                            store.Open(OpenFlags.ReadOnly);
                            foreach (X509Certificate2 cert in store.Certificates)
                            {
                                if ((token = CheckForToken(cert, name)) != null)
                                {
                                    return token;
                                }
                            }
                        }
                        break;
                    }

                default:
                    {
                        throw new Exception($"The requested store type '{storeType}' is not supported. Please change.");
                    }
            }
            return null;
        }

        /// <summary>
        /// Creates a cert with the connectionstring (token) and stores it in the given cert store.
        /// </summary>
        public static async Task WriteAsync(string name, string connectionString, string storeType, string storePath)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Token not found in X509Store and no new token provided!");
            }

            SecureRandom random = new SecureRandom();
            KeyGenerationParameters keyGenerationParameters = new KeyGenerationParameters(random, 2048);
            RsaKeyPairGenerator keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            AsymmetricCipherKeyPair keys = keyPairGenerator.GenerateKeyPair();

            ArrayList nameOids = new ArrayList();
            nameOids.Add(X509Name.CN);
            ArrayList nameValues = new ArrayList();
            nameValues.Add(name);
            X509Name subjectDN = new X509Name(nameOids, nameValues);
            X509Name issuerDN = subjectDN;

            X509V3CertificateGenerator cg = new X509V3CertificateGenerator();
            cg.SetSerialNumber(BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random));
            cg.SetIssuerDN(issuerDN);
            cg.SetSubjectDN(subjectDN);
            cg.SetNotBefore(DateTime.Now);
            cg.SetNotAfter(DateTime.Now.AddMonths(12));
            cg.SetPublicKey(keys.Public);
            cg.AddExtension(X509Extensions.KeyUsage, true, new KeyUsage(KeyUsage.DataEncipherment));

            // encrypt the token with the public key so only the owner of the assoc. private key can decrypt it and
            // "hide" it in the instruction code cert extension
            RSA rsa = RSA.Create();
            RSAParameters rsaParams = new RSAParameters();
            RsaKeyParameters keyParams = (RsaKeyParameters)keys.Public;

            rsaParams.Modulus = new byte[keyParams.Modulus.ToByteArrayUnsigned().Length];
            keyParams.Modulus.ToByteArrayUnsigned().CopyTo(rsaParams.Modulus, 0);

            rsaParams.Exponent = new byte[keyParams.Exponent.ToByteArrayUnsigned().Length];
            keyParams.Exponent.ToByteArrayUnsigned().CopyTo(rsaParams.Exponent, 0);

            rsa.ImportParameters(rsaParams);
            if (rsa != null)
            {
                byte[] bytes = rsa.Encrypt(Encoding.ASCII.GetBytes(connectionString), RSAEncryptionPadding.OaepSHA1);
                if (bytes != null)
                {
                    cg.AddExtension(X509Extensions.InstructionCode, false, bytes);
                }
                else
                {
                    throw new CryptographicException("Can not encrypt IoTHub security token using generated public key!");
                }
            }

            // sign the cert with the private key
            ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA256WITHRSA", keys.Private, random);
            Org.BouncyCastle.X509.X509Certificate x509 = cg.Generate(signatureFactory);

            // create a PKCS12 store for the cert and its private key
            X509Certificate2 certificate = null;
            using (MemoryStream pfxData = new MemoryStream())
            {
                Pkcs12StoreBuilder builder = new Pkcs12StoreBuilder();
                builder.SetUseDerEncoding(true);
                Pkcs12Store pkcsStore = builder.Build();
                X509CertificateEntry[] chain = new X509CertificateEntry[1];
                string passcode = Guid.NewGuid().ToString();
                chain[0] = new X509CertificateEntry(x509);
                pkcsStore.SetKeyEntry(name, new AsymmetricKeyEntry(keys.Private), chain);
                pkcsStore.Save(pfxData, passcode.ToCharArray(), random);

                // create X509Certificate2 object from PKCS12 file
                certificate = CertificateFactory.CreateCertificateFromPKCS12(pfxData.ToArray(), passcode);

                // handle each store type differently
                switch (storeType)
                {
                    case CertificateStoreType.Directory:
                        {
                            // Add to DirectoryStore
                            using (DirectoryCertificateStore store = new DirectoryCertificateStore())
                            {
                                store.Open(storePath);
                                X509CertificateCollection certificates = await store.Enumerate().ConfigureAwait(false);

                                // remove any existing cert with our name from the store
                                foreach (X509Certificate2 cert in certificates)
                                {
                                    if (cert.SubjectName.Decode(X500DistinguishedNameFlags.None | X500DistinguishedNameFlags.DoNotUseQuotes).Equals("CN=" + name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        await store.Delete(cert.Thumbprint).ConfigureAwait(false);
                                    }
                                }

                                // add new one
                                await store.Add(certificate).ConfigureAwait(false);
                            }
                            break;
                        }
                    case CertificateStoreType.X509Store:
                        {
                            // Add to X509Store
                            using (X509Store store = new X509Store(storePath, StoreLocation.CurrentUser))
                            {
                                store.Open(OpenFlags.ReadWrite);

                                // remove any existing cert with our name from the store
                                foreach (X509Certificate2 cert in store.Certificates)
                                {
                                    if (cert.SubjectName.Decode(X500DistinguishedNameFlags.None | X500DistinguishedNameFlags.DoNotUseQuotes).Equals("CN=" + name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        store.Remove(cert);
                                    }
                                }

                                // add new cert to store
                                try
                                {
                                    store.Add(certificate);
                                }
                                catch (Exception e)
                                {
                                    throw new Exception($"Not able to add cert to the requested store type '{storeType}' (exception message: '{e.Message}'.");
                                }
                            }
                            break;
                        }
                    default:
                        {
                            throw new Exception($"The requested store type '{storeType}' is not supported. Please change.");
                        }
                }
                return;
            }
        }

        /// <summary>
        /// Validates the cert and extracts the token from the cert.
        /// </summary>
        private static string CheckForToken(X509Certificate2 cert, string name)
        {
            if (cert.SubjectName.Decode(X500DistinguishedNameFlags.None | X500DistinguishedNameFlags.DoNotUseQuotes).Equals("CN=" + name, StringComparison.OrdinalIgnoreCase) &&
                (DateTime.Now < cert.NotAfter))
            {
                RSA rsa = cert.GetRSAPrivateKey();
                if (rsa != null)
                {
                    foreach (System.Security.Cryptography.X509Certificates.X509Extension extension in cert.Extensions)
                    {
                        // check for instruction code extension
                        if ((extension.Oid.Value == "2.5.29.23") && (extension.RawData.Length >= 4))
                        {
                            byte[] bytes = new byte[extension.RawData.Length - 4];
                            Array.Copy(extension.RawData, 4, bytes, 0, bytes.Length);
                            byte[] token = rsa.Decrypt(bytes, RSAEncryptionPadding.OaepSHA1);
                            return Encoding.ASCII.GetString(token);
                        }
                    }
                }
            }
            return null;
        }
    }
}
