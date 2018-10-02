// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Newtonsoft.Json;
using System;
using System.Security.Cryptography.X509Certificates;
#if CERTSIGNER
namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{

    public sealed class CertificateKeyPairApiModel
    {
        [JsonProperty(PropertyName = "Subject", Order = 10)]
        public string Subject { get; set; }

        [JsonProperty(PropertyName = "Thumbprint", Order = 20)]
        public string Thumbprint { get; set; }

        [JsonProperty(PropertyName = "Certificate", Order = 30)]
        public string Certificate { get; set; }

        [JsonProperty(PropertyName = "PrivateKeyFormat", Order = 40)]
        public string PrivateKeyFormat { get; set; }

        [JsonProperty(PropertyName = "PrivateKey", Order = 50)]
        public string PrivateKey { get; set; }

        public CertificateKeyPairApiModel(Opc.Ua.Gds.Server.X509Certificate2KeyPair certificate)
        {
            this.Subject = certificate.Certificate.Subject;
            this.Thumbprint = certificate.Certificate.Thumbprint;
            this.Certificate = Convert.ToBase64String(certificate.Certificate.RawData);
            this.PrivateKeyFormat = certificate.PrivateKeyFormat;
            this.PrivateKey = Convert.ToBase64String(certificate.PrivateKey);
        }

        public Opc.Ua.Gds.Server.X509Certificate2KeyPair ToServiceModel()
        {
            return new Opc.Ua.Gds.Server.X509Certificate2KeyPair(new X509Certificate2(Convert.FromBase64String(Certificate)), PrivateKeyFormat, Convert.FromBase64String(PrivateKey));
        }

    }
}
#endif
