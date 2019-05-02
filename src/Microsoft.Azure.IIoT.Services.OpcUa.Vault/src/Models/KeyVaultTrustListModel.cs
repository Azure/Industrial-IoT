// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Opc.Ua;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models
{
    public class KeyVaultTrustListModel
    {
        public readonly string Group;
        public X509Certificate2Collection IssuerCertificates;
        public IList<X509CRL> IssuerCrls;
        public X509Certificate2Collection TrustedCertificates;
        public IList<X509CRL> TrustedCrls;
        public string NextPageLink;

        public KeyVaultTrustListModel(string id)
        {
            Group = id;
            IssuerCertificates = new X509Certificate2Collection();
            IssuerCrls = new List<X509CRL>();
            TrustedCertificates = new X509Certificate2Collection();
            TrustedCrls = new List<X509CRL>();
            NextPageLink = null;
        }
    }
}
