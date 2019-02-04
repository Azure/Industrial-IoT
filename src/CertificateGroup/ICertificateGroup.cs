// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault
{
    public interface ICertificateGroup
    {
        Task<ICertificateGroup> OnBehalfOfRequest(HttpRequest request);
        Task<string[]> GetCertificateGroupIds();
        Task<CertificateGroupConfigurationModel> GetCertificateGroupConfiguration(string id);
        Task<CertificateGroupConfigurationModel> UpdateCertificateGroupConfiguration(string id, CertificateGroupConfigurationModel config);
        Task<CertificateGroupConfigurationModel> CreateCertificateGroupConfiguration(string id, string subject, string certType);
        Task<IList<CertificateGroupConfigurationModel>> GetCertificateGroupConfigurationCollection();
        Task<X509Certificate2Collection> GetIssuerCACertificateVersionsAsync(string id, bool? withCertificates, string nextPageLink = null, int? pageSize = null);
        Task<X509Certificate2Collection> GetIssuerCACertificateChainAsync(string id, string thumbPrint = null, string nextPageLink = null, int? pageSize = null);
        Task<IList<Opc.Ua.X509CRL>> GetIssuerCACrlChainAsync(string id, string thumbPrint, string nextPageLink = null, int? pageSize = null);
        Task<KeyVaultTrustListModel> GetTrustListAsync(string id, string nextPageLink = null, int? pageSize = null);

        Task<X509Certificate2> SigningRequestAsync(
            string id,
            string applicationUri,
            byte[] certificateRequest
            );
        Task<Opc.Ua.X509CRL> RevokeCertificateAsync(
            string id,
            X509Certificate2 certificate
            );
        Task<X509Certificate2Collection> RevokeCertificatesAsync(
            string id,
            X509Certificate2Collection certificates);

        Task<X509Certificate2> CreateIssuerCACertificateAsync(
            string id
            );
        Task<Opc.Ua.Gds.Server.X509Certificate2KeyPair> NewKeyPairRequestAsync(
            string id,
            string requestId,
            string applicationUri,
            string subjectName,
            string[] domainNames,
            string privateKeyFormat,
            string privateKeyPassword
            );

        Task<byte[]> LoadPrivateKeyAsync(string id, string requestId, string privateKeyFormat);
        Task AcceptPrivateKeyAsync(string id, string requestId);
        Task DeletePrivateKeyAsync(string id, string requestId);
    }
}
