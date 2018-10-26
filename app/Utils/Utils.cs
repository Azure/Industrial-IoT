// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//


using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Utils
{
    public class ContentType
    {

        public const string Cert = "application/pkix-cert";
        public const string Crl = "application/pkix-crl";
        // see CertificateContentType.Pfx
        public const string Pfx = "application/x-pkcs12";
        // see CertificateContentType.Pem
        public const string Pem = "application/x-pem-file";
    }

    public class Utils
    {
        public static string CertFileName(string signedCertificate)
        {
            try
            {
                var signedCertByteArray = Convert.FromBase64String(signedCertificate);
                X509Certificate2 cert = new X509Certificate2(signedCertByteArray);
                var dn = Opc.Ua.Utils.ParseDistinguishedName(cert.Subject);
                var prefix = dn.Where(x => x.StartsWith("CN=", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Substring(3);
                return prefix + " [" + cert.Thumbprint + "]";
            }
            catch
            {
                return "Certificate";
            }
        }
    }
}
