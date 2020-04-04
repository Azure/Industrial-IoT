// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment {

    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    class X509CertificateHelper {

        public static X509Certificate2 BuildSelfSignedServerCertificate(
            string certificateName,
            string dnsName,
            string password
        ) {
            var sanBuilder = new SubjectAlternativeNameBuilder();

            //sanBuilder.AddIpAddress(IPAddress.Loopback);
            //sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);

            sanBuilder.AddDnsName(dnsName);

            var distinguishedName = new X500DistinguishedName($"CN={certificateName}");

            using (var rsa = RSA.Create(2048)) {
                var request = new CertificateRequest(
                    distinguishedName,
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                );

                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature,
                        false
                    )
                );

                var serverAuthenticationOid = new Oid("1.3.6.1.5.5.7.3.1");
                var clientAuthenticationOid = new Oid("1.3.6.1.5.5.7.3.2");

                request.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                        new OidCollection {
                            serverAuthenticationOid,
                            clientAuthenticationOid
                        },
                        false
                    )
                );

                request.CertificateExtensions.Add(sanBuilder.Build());

                var certificate = request.CreateSelfSigned(
                    new DateTimeOffset(DateTime.UtcNow.AddDays(-1)),
                    new DateTimeOffset(DateTime.UtcNow.AddYears(1))
                );

                certificate.FriendlyName = certificateName;

                var x509Certificate2 = new X509Certificate2(
                    certificate.Export(X509ContentType.Pfx, password),
                    password,
                    X509KeyStorageFlags.MachineKeySet
                );

                return x509Certificate2;
            }
        }

        public static void AddCertificateToUserPersonalStore(X509Certificate2 cert) {
            using(var certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser)) {
                certStore.Open(OpenFlags.ReadWrite);
                certStore.Add(cert);
            }
        }

        public static string GetPemPublicKey(X509Certificate2 cert) {
            var x509CertificateParser = new Org.BouncyCastle.X509.X509CertificateParser();
            var bcCert = x509CertificateParser.ReadCertificate(cert.RawData);
            var asymmetricKeyParameter = bcCert.GetPublicKey();

            var stringWrite = new StringWriter();

            var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(stringWrite);
            pemWriter.WriteObject(asymmetricKeyParameter);
            stringWrite.Close();

            return stringWrite.ToString();
        }

        public static string GetPemCertificate(X509Certificate2 cert) {
            var certRawDataBase64 = Convert.ToBase64String(cert.RawData, Base64FormattingOptions.InsertLineBreaks);

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("-----BEGIN CERTIFICATE-----");
            stringBuilder.AppendLine(certRawDataBase64);
            stringBuilder.AppendLine("-----END CERTIFICATE-----");

            return stringBuilder.ToString();
        }

        public static string GetPemPrivateKey(X509Certificate2 cert) {
            var rsaPrivateKey = cert.GetRSAPrivateKey();

            if (null == rsaPrivateKey) {
                throw new ArgumentException("Certificate does not contain RSA Private key.", "cert");
            }

            var rsaParameters = rsaPrivateKey.ExportParameters(true);
            var asymmetricCipherKeyPair = Org.BouncyCastle.Security.DotNetUtilities.GetRsaKeyPair(rsaParameters);

            var stringWrite = new StringWriter();

            var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(stringWrite);
            pemWriter.WriteObject(asymmetricCipherKeyPair.Private);
            stringWrite.Close();

            return stringWrite.ToString();
        }

        public static string GetOpenSSHPublicKey(X509Certificate2 cert) {
            const string sshRsaPrefix = "ssh-rsa";
            var sshRsaBytes = Encoding.Default.GetBytes(sshRsaPrefix);

            var rsaPublicKey = cert.GetRSAPublicKey();

            if (null == rsaPublicKey) {
                throw new ArgumentException("Certificate does not contain RSA Public key.", nameof(cert));
            }

            var rsaParameters = rsaPublicKey.ExportParameters(false);

            var modulus = rsaParameters.Modulus;
            var exponent = rsaParameters.Exponent;

            string buffer64;

            using (var memoryStream = new MemoryStream()) {
                memoryStream.Write(IntToBytes(sshRsaBytes.Length), 0, 4);
                memoryStream.Write(sshRsaBytes, 0, sshRsaBytes.Length);

                memoryStream.Write(IntToBytes(exponent.Length), 0, 4);
                memoryStream.Write(exponent, 0, exponent.Length);

                // ToDo: Investigate further why is 0 necessary before modulus.
                // Some useful links:
                // https://stackoverflow.com/a/47917364/1451497
                // https://stackoverflow.com/questions/35663650/explanation-of-a-rsa-key-file
                // https://stackoverflow.com/a/17286288/1451497
                // https://stackoverflow.com/questions/12749858/rsa-public-key-format

                memoryStream.Write(IntToBytes(modulus.Length + 1), 0, 4);  // Add +1 to Emulate PuttyGen
                memoryStream.Write(new byte[] { 0 }, 0, 1);                // Add a 0 to Emulate PuttyGen
                memoryStream.Write(modulus, 0, modulus.Length);

                memoryStream.Flush();

                buffer64 = Convert.ToBase64String(memoryStream.ToArray());
            }

            var openSSHPublicKey = $"{sshRsaPrefix} {buffer64} generated-key";

            return openSSHPublicKey;
        }

        private static byte[] IntToBytes(int i) {
            var bts = BitConverter.GetBytes(i);

            if (BitConverter.IsLittleEndian) {
                Array.Reverse(bts);
            }

            return bts;
        }
    }
}
