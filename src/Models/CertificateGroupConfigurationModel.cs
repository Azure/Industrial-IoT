// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models
{
    public sealed class CertificateGroupConfigurationModel
    {
        /// <summary>
        /// The name of the certificate group, ofter referred to as group id.
        /// </summary>
        [JsonProperty(PropertyName = "id", Order = 10)]
        public string Id { get; set; }

        /// <summary>
        /// The certificate type as specified in the OPC UA spec 1.04.
        /// supported values:
        /// - RsaSha256ApplicationCertificateType (default)
        /// - ApplicationCertificateType
        /// </summary>
        [JsonProperty(PropertyName = "certificateType", Order = 15)]
        public string CertificateType { get; set; }
        /// <summary>
        /// The subject as distinguished name.
        /// </summary>
        [JsonProperty(PropertyName = "subjectName", Order = 20)]
        public string SubjectName { get; set; }
        /// <summary>
        /// The default certificate lifetime in months.
        /// Default: 24 months.
        /// </summary>
        [JsonProperty(PropertyName = "defaultCertificateLifetime", Order = 30)]
        public ushort DefaultCertificateLifetime { get; set; }
        /// <summary>
        /// The default certificate key size in bits.
        /// Allowed values: 2048, 3072, 4096
        /// </summary>
        [JsonProperty(PropertyName = "defaultCertificateKeySize", Order = 40)]
        public ushort DefaultCertificateKeySize { get; set; }
        /// <summary>
        /// The default certificate SHA-2 hash size in bits.
        /// Allowed values: 256 (default), 384, 512
        /// </summary>
        [JsonProperty(PropertyName = "defaultCertificateHashSize", Order = 50)]
        public ushort DefaultCertificateHashSize { get; set; }
        /// <summary>
        /// The default issuer CA certificate lifetime in months.
        /// Default: 60 months.
        /// </summary>
        [JsonProperty(PropertyName = "issuerCACertificateLifetime", Order = 60)]
        public ushort IssuerCACertificateLifetime { get; set; }
        /// <summary>
        /// The default issuer CA certificate key size in bits.
        /// Allowed values: 2048, 3072, 4096
        /// </summary>
        [JsonProperty(PropertyName = "issuerCACertificateKeySize", Order = 70)]
        public ushort IssuerCACertificateKeySize { get; set; }
        /// <summary>
        /// The default issuer CA certificate key size in bits.
        /// Allowed values: 2048, 3072, 4096
        /// </summary>
        [JsonProperty(PropertyName = "issuerCACertificateHashSize", Order = 80)]
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
