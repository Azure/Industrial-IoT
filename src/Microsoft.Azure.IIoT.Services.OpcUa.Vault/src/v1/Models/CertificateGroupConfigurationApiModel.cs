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
        /// <summary>
        /// The name of the certificate group, ofter referred to as group id.
        /// </summary>
        [JsonProperty(PropertyName = "name", Order = 10)]
        [Required]
        public string Id { get; set; }
        /// <summary>
        /// The certificate type as specified in the OPC UA spec 1.04.
        /// supported values:
        /// - RsaSha256ApplicationCertificateType (default)
        /// - ApplicationCertificateType
        /// </summary>
        [JsonProperty(PropertyName = "certificateType", Order = 15)]
        [Required]
        public string CertificateType { get; set; }
        /// <summary>
        /// The subject as distinguished name.
        /// </summary>
        [JsonProperty(PropertyName = "subjectName", Order = 20)]
        [Required]
        public string SubjectName { get; set; }
        /// <summary>
        /// The default certificate lifetime in months.
        /// Default: 24 months.
        /// </summary>
        [JsonProperty(PropertyName = "defaultCertificateLifetime", Order = 30)]
        [Required]
        public ushort DefaultCertificateLifetime { get; set; }
        /// <summary>
        /// The default certificate key size in bits.
        /// Allowed values: 2048, 3072, 4096
        /// </summary>
        [JsonProperty(PropertyName = "defaultCertificateKeySize", Order = 40)]
        [Required]
        public ushort DefaultCertificateKeySize { get; set; }
        /// <summary>
        /// The default certificate SHA-2 hash size in bits.
        /// Allowed values: 256 (default), 384, 512
        /// </summary>
        [JsonProperty(PropertyName = "defaultCertificateHashSize", Order = 50)]
        [Required]
        public ushort DefaultCertificateHashSize { get; set; }
        /// <summary>
        /// The default issuer CA certificate lifetime in months.
        /// Default: 60 months.
        /// </summary>
        [JsonProperty(PropertyName = "issuerCACertificateLifetime", Order = 60)]
        [Required]
        public ushort IssuerCACertificateLifetime { get; set; }
        /// <summary>
        /// The default issuer CA certificate key size in bits.
        /// Allowed values: 2048, 3072, 4096
        /// </summary>
        [JsonProperty(PropertyName = "issuerCACertificateKeySize", Order = 70)]
        [Required]
        public ushort IssuerCACertificateKeySize { get; set; }
        /// <summary>
        /// The default issuer CA certificate key size in bits.
        /// Allowed values: 2048, 3072, 4096
        /// </summary>
        [JsonProperty(PropertyName = "issuerCACertificateHashSize", Order = 80)]
        [Required]
        public ushort IssuerCACertificateHashSize { get; set; }
        /// <summary>
        /// The endpoint URL for the CRL Distributionpoint in the Issuer CA certificate.
        /// The names %servicehost%, %serial% and %group% are replaced with cert values.
        /// default: 'http://%servicehost%/certs/crl/%serial%/%group%.crl'
        /// </summary>
        [JsonProperty(PropertyName = "issuerCACRLDistributionPoint", Order = 90)]
        public string IssuerCACrlDistributionPoint { get; set; }
        /// <summary>
        /// The endpoint URL for the Issuer CA Authority Information Access.
        /// The names %servicehost%, %serial% and %group% are replaced with cert values.
        /// default: 'http://%servicehost%/certs/issuer/%serial%/%group%.cer'
        /// </summary>
        [JsonProperty(PropertyName = "issuerCAAuthorityInformationAccess", Order = 100)]
        public string IssuerCAAuthorityInformationAccess { get; set; }

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
            this.IssuerCACrlDistributionPoint = config.IssuerCACrlDistributionPoint;
            this.IssuerCAAuthorityInformationAccess = config.IssuerCAAuthorityInformationAccess;
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
            serviceModel.IssuerCACrlDistributionPoint = this.IssuerCACrlDistributionPoint;
            serviceModel.IssuerCAAuthorityInformationAccess = this.IssuerCAAuthorityInformationAccess;
            return serviceModel;
        }
    }
}
