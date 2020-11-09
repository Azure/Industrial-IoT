// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment {

    using System;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    class X509CertificateHelper {

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
