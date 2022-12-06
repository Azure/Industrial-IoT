// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client.Tests {
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    public class MqttClientConnectionStringBuilderTestsBase : IDisposable {
        private static readonly TimeSpan kIoTHubRootCertificateValidity = TimeSpan.FromDays(1);

        protected string IoTHubRootCertificateFile { get; private set; }

        public MqttClientConnectionStringBuilderTestsBase() {
            IoTHubRootCertificateFile = $"./IoTHubRootCertificateFile-{Guid.NewGuid()}.pem";
            Environment.SetEnvironmentVariable("IoTHubRootCertificateFile", IoTHubRootCertificateFile);
            CreateSelfSignedCertificate();
        }

        private void CreateSelfSignedCertificate() {
            if (File.Exists(IoTHubRootCertificateFile)) {
                File.Delete(IoTHubRootCertificateFile);
            }

            var rsa = RSA.Create();
            var certificateRequest = new CertificateRequest("CN=Contoso", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var x509cert = certificateRequest.CreateSelfSigned(DateTimeOffset.UtcNow, DateTime.UtcNow.Add(kIoTHubRootCertificateValidity));
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("-----BEGIN CERTIFICATE-----");
            stringBuilder.AppendLine(Convert.ToBase64String(x509cert.RawData, Base64FormattingOptions.InsertLineBreaks));
            stringBuilder.AppendLine("-----END CERTIFICATE-----");
            File.WriteAllText(IoTHubRootCertificateFile, stringBuilder.ToString());
        }

        public void Dispose() {
            try {
                if (File.Exists(IoTHubRootCertificateFile)) {
                    File.Delete(IoTHubRootCertificateFile);
                }
            }
            catch {
                // Ignored.
            }
        }
    }
}
