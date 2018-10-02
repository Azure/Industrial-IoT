// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class CertificateGroupConfigurationApiModel
    {
        [JsonProperty(PropertyName = "Name", Order = 10)]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "CertificateType", Order = 15)]
        public string CertificateType { get; set; }

        [JsonProperty(PropertyName = "SubjectName", Order = 20)]
        public string SubjectName { get; set; }

        [JsonProperty(PropertyName = "DefaultCertificateLifetime", Order = 30)]
        public ushort DefaultCertificateLifetime { get; set; }

        [JsonProperty(PropertyName = "DefaultCertificateKeySize", Order = 40)]
        public ushort DefaultCertificateKeySize { get; set; }

        [JsonProperty(PropertyName = "DefaultCertificateHashSize", Order = 50)]
        public ushort DefaultCertificateHashSize { get; set; }

        [JsonProperty(PropertyName = "CACertificateLifetime", Order = 60)]
        public ushort CACertificateLifetime { get; set; }

        [JsonProperty(PropertyName = "CACertificateKeySize", Order = 70)]
        public ushort CACertificateKeySize { get; set; }

        [JsonProperty(PropertyName = "CACertificateHashSize", Order = 80)]
        public ushort CACertificateHashSize { get; set; }

        public CertificateGroupConfigurationApiModel() { }

        public CertificateGroupConfigurationApiModel(string id, Opc.Ua.Gds.Server.CertificateGroupConfiguration config)
        {
            this.Id = id;
            this.CertificateType = config.CertificateType;
            this.SubjectName = config.SubjectName;
            this.DefaultCertificateLifetime = config.DefaultCertificateLifetime;
            this.DefaultCertificateKeySize = config.DefaultCertificateKeySize;
            this.DefaultCertificateHashSize = config.DefaultCertificateHashSize;
            this.CACertificateLifetime = config.CACertificateLifetime;
            this.CACertificateKeySize = config.CACertificateKeySize;
            this.CACertificateHashSize = config.CACertificateHashSize;
        }

        public Opc.Ua.Gds.Server.CertificateGroupConfiguration ToServiceModel()
        {
            var serviceModel = new Opc.Ua.Gds.Server.CertificateGroupConfiguration();
            serviceModel.Id = this.Id;
            serviceModel.CertificateType = this.CertificateType;
            serviceModel.SubjectName = this.SubjectName;
            serviceModel.DefaultCertificateLifetime = this.DefaultCertificateLifetime;
            serviceModel.DefaultCertificateKeySize = this.DefaultCertificateKeySize;
            serviceModel.DefaultCertificateHashSize = this.DefaultCertificateHashSize;
            serviceModel.CACertificateLifetime = this.CACertificateLifetime;
            serviceModel.CACertificateKeySize = this.CACertificateKeySize;
            serviceModel.CACertificateHashSize = this.CACertificateHashSize;
            return serviceModel;
        }
    }
}
