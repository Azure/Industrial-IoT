// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models
{
    public sealed class CertificateGroupConfigurationModel
    {
        [JsonProperty(PropertyName = "id", Order = 10)]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "certificateType", Order = 15)]
        public string CertificateType { get; set; }

        [JsonProperty(PropertyName = "subjectName", Order = 20)]
        public string SubjectName { get; set; }

        [JsonProperty(PropertyName = "defaultCertificateLifetime", Order = 30)]
        public ushort DefaultCertificateLifetime { get; set; }

        [JsonProperty(PropertyName = "defaultCertificateKeySize", Order = 40)]
        public ushort DefaultCertificateKeySize { get; set; }

        [JsonProperty(PropertyName = "defaultCertificateHashSize", Order = 50)]
        public ushort DefaultCertificateHashSize { get; set; }

        [JsonProperty(PropertyName = "issuerCACertificateLifetime", Order = 60)]
        public ushort IssuerCACertificateLifetime { get; set; }

        [JsonProperty(PropertyName = "issuerCACertificateKeySize", Order = 70)]
        public ushort IssuerCACertificateKeySize { get; set; }

        [JsonProperty(PropertyName = "issuerCACertificateHashSize", Order = 80)]
        public ushort IssuerCACertificateHashSize { get; set; }

        public CertificateGroupConfigurationModel() { }

        public Opc.Ua.Gds.Server.CertificateGroupConfiguration ToGdsServerModel()
        {
            return new Opc.Ua.Gds.Server.CertificateGroupConfiguration()
            {
                Id = this.Id,
                CertificateType = this.CertificateType,
                SubjectName = this.SubjectName,
                BaseStorePath = "/"+Id.ToLower(),
                DefaultCertificateHashSize = this.DefaultCertificateHashSize,
                DefaultCertificateKeySize = this.DefaultCertificateKeySize,
                DefaultCertificateLifetime = this.DefaultCertificateLifetime,
                CACertificateHashSize = this.IssuerCACertificateHashSize,
                CACertificateKeySize = this.IssuerCACertificateKeySize,
                CACertificateLifetime = this.IssuerCACertificateLifetime
            };
        }

    }
}
