// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class CertificateGroupConfigurationApiModel
    {
        [JsonProperty(PropertyName = "name", Order = 10)]
        [Required]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "certificateType", Order = 15)]
        [Required]
        public string CertificateType { get; set; }

        [JsonProperty(PropertyName = "subjectName", Order = 20)]
        [Required]
        public string SubjectName { get; set; }

        [JsonProperty(PropertyName = "defaultCertificateLifetime", Order = 30)]
        [Required]
        public ushort DefaultCertificateLifetime { get; set; }

        [JsonProperty(PropertyName = "defaultCertificateKeySize", Order = 40)]
        [Required]
        public ushort DefaultCertificateKeySize { get; set; }

        [JsonProperty(PropertyName = "defaultCertificateHashSize", Order = 50)]
        [Required]
        public ushort DefaultCertificateHashSize { get; set; }

        [JsonProperty(PropertyName = "issuerCACertificateLifetime", Order = 60)]
        [Required]
        public ushort IssuerCACertificateLifetime { get; set; }

        [JsonProperty(PropertyName = "issuerCACertificateKeySize", Order = 70)]
        [Required]
        public ushort IssuerCACertificateKeySize { get; set; }

        [JsonProperty(PropertyName = "issuerCACertificateHashSize", Order = 80)]
        [Required]
        public ushort IssuerCACertificateHashSize { get; set; }

        public CertificateGroupConfigurationApiModel() { }

        public CertificateGroupConfigurationApiModel(string id, CertificateGroupConfigurationModel config)
        {
            this.Id = id;
            this.CertificateType = config.CertificateType;
            this.SubjectName = config.SubjectName;
            this.DefaultCertificateLifetime = config.DefaultCertificateLifetime;
            this.DefaultCertificateKeySize = config.DefaultCertificateKeySize;
            this.DefaultCertificateHashSize = config.DefaultCertificateHashSize;
            this.IssuerCACertificateLifetime = config.IssuerCACertificateLifetime;
            this.IssuerCACertificateKeySize = config.IssuerCACertificateKeySize;
            this.IssuerCACertificateHashSize = config.IssuerCACertificateHashSize;
        }

        public CertificateGroupConfigurationModel ToServiceModel()
        {
            var serviceModel = new CertificateGroupConfigurationModel();
            serviceModel.Id = this.Id;
            serviceModel.CertificateType = this.CertificateType;
            serviceModel.SubjectName = this.SubjectName;
            serviceModel.DefaultCertificateLifetime = this.DefaultCertificateLifetime;
            serviceModel.DefaultCertificateKeySize = this.DefaultCertificateKeySize;
            serviceModel.DefaultCertificateHashSize = this.DefaultCertificateHashSize;
            serviceModel.IssuerCACertificateLifetime = this.IssuerCACertificateLifetime;
            serviceModel.IssuerCACertificateKeySize = this.IssuerCACertificateKeySize;
            serviceModel.IssuerCACertificateHashSize = this.IssuerCACertificateHashSize;
            return serviceModel;
        }
    }
}
