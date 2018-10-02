// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Newtonsoft.Json;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class TrustListApiModel
    {

        [JsonProperty(PropertyName = "Id", Order = 10)]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "IssuerCertificates", Order = 20)]
        public X509Certificate2CollectionApiModel IssuerCertificates { get; set; }

        [JsonProperty(PropertyName = "IssuerCrls", Order = 30)]
        public X509CrlCollectionApiModel IssuerCrls { get; set; }

        [JsonProperty(PropertyName = "TrustedCertificates", Order = 40)]
        public X509Certificate2CollectionApiModel TrustedCertificates { get; set; }

        [JsonProperty(PropertyName = "TrustedCrls", Order = 50)]
        public X509CrlCollectionApiModel TrustedCrls { get; set; }

        public TrustListApiModel(KeyVaultTrustListModel keyVaultTrustList)
        {
            Id = keyVaultTrustList.Id;
            IssuerCertificates = new X509Certificate2CollectionApiModel(keyVaultTrustList.IssuerCertificates);
            IssuerCrls = new X509CrlCollectionApiModel(keyVaultTrustList.IssuerCrls);
            TrustedCertificates = new X509Certificate2CollectionApiModel(keyVaultTrustList.TrustedCertificates);
            TrustedCrls = new X509CrlCollectionApiModel(keyVaultTrustList.TrustedCrls);
        }

    }
}
