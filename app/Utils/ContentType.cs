// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//


namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Utils
{
    public class ContentType
    {
        // see RFC 2585
        public const string Cert = "application/pkix-cert";
        public const string Crl = "application/pkix-crl";
        // see CertificateContentType.Pfx
        public const string Pfx = "application/x-pkcs12";
        // see CertificateContentType.Pem
        public const string Pem = "application/x-pem-file";

    }
}
