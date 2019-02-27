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
    /// <summary>
    /// Certificate Group interface.
    /// </summary>
    public interface ICertificateGroup
    {
        /// <summary>
        /// Returns a shallow copy of the certificate group which uses
        /// a token on behalf of a user. 
        /// </summary>
        /// <param name="request">The http request with the user token</param>
        Task<ICertificateGroup> OnBehalfOfRequest(HttpRequest request);
        /// <summary>
        /// Return the names of the certificate groups.
        /// </summary>
        /// <returns>The certificate group ids</returns>
        Task<string[]> GetCertificateGroupIds();
        /// <summary>
        /// Get the configuration for a group Id.
        /// </summary>
        /// <param name="id">The group Id</param>
        /// <returns>The configuration</returns>
        Task<CertificateGroupConfigurationModel> GetCertificateGroupConfiguration(string id);
        /// <summary>
        /// Update settings of a certificate group.
        /// The update is sanity checked against default policies.
        /// </summary>
        /// <param name="id">The group Id</param>
        /// <param name="config">The updated configuration</param>
        /// <returns>The updated group</returns>
        Task<CertificateGroupConfigurationModel> UpdateCertificateGroupConfiguration(string id, CertificateGroupConfigurationModel config);
        /// <summary>
        /// Create a new certificate group with default settings.
        /// Default settings depend on certificate type.
        /// </summary>
        /// <param name="id">The new group Id</param>
        /// <param name="subject">The subject of the new Issuer CA certificate</param>
        /// <param name="certType">The certificate type for the new group</param>
        Task<CertificateGroupConfigurationModel> CreateCertificateGroupConfiguration(string id, string subject, string certType);
        /// <summary>
        /// Get the configuration of all certificate groups.
        /// </summary>
        /// <returns>The configurations</returns>
        Task<IList<CertificateGroupConfigurationModel>> GetCertificateGroupConfigurationCollection();
        /// <summary>
        /// Get all Issuer certificate versions in a pageable call.
        /// </summary>
        /// <param name="id">The group Id</param>
        /// <param name="withCertificates">true to return the base64 encoded certificates</param>
        /// <param name="nextPageLink">The next page</param>
        /// <param name="pageSize">max number of versions per call</param>
        /// <returns></returns>
        Task<(X509Certificate2Collection, string)> GetIssuerCACertificateVersionsAsync(string id, bool? withCertificates, string nextPageLink = null, int? pageSize = null);
        /// <summary>
        /// Get all certificates in the chain of the Issuer CA.
        /// </summary>
        /// <param name="id">The group id</param>
        /// <param name="thumbPrint">null for the latest Issuer CA cert, thumbprint to get a specific older version</param>
        /// <param name="nextPageLink">The next page</param>
        /// <param name="pageSize">max number of certificates per call</param>
        /// <returns></returns>
        Task<X509Certificate2Collection> GetIssuerCACertificateChainAsync(string id, string thumbPrint = null, string nextPageLink = null, int? pageSize = null);
        /// <summary>
        /// Get the CRLs for all certificates in the Issuer CA chain.
        /// </summary>
        /// <param name="id">The group id</param>
        /// <param name="thumbPrint">null for the latest Issuer CA cert, thumbprint to get a specific older version</param>
        /// <param name="nextPageLink">The next page</param>
        /// <param name="pageSize">max number of CRL per call</param>
        /// <returns></returns>
        Task<IList<Opc.Ua.X509CRL>> GetIssuerCACrlChainAsync(string id, string thumbPrint, string nextPageLink = null, int? pageSize = null);
        /// <summary>
        /// Get the default trustlist of a certificate group.
        /// Pageable.
        /// A trustlist contains an Issuer list and a Trusted list.
        /// Issuer and Trusted list each contain a list of certificates and CRLs.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nextPageLink"></param>
        /// <param name="pageSize"></param>
        /// <returns>The trust list page</returns>
        Task<KeyVaultTrustListModel> GetTrustListAsync(string id, string nextPageLink = null, int? pageSize = null);
        /// <summary>
        /// Create a new certificate request with a CSR.
        /// </summary>
        /// <param name="id">The group id</param>
        /// <param name="applicationUri">The application Uri</param>
        /// <param name="certificateRequest">The binary CSR</param>
        /// <returns></returns>
        Task<X509Certificate2> SigningRequestAsync(
            string id,
            string applicationUri,
            byte[] certificateRequest
            );
        /// <summary>
        /// Revoke a single certificate.
        /// Creates a new CRL version Issuer CA matching the certificate.
        /// </summary>
        /// <param name="id">The group Id</param>
        /// <param name="certificate">The certificate to revoke</param>
        /// <returns>The new CRL version</returns>
        Task<Opc.Ua.X509CRL> RevokeCertificateAsync(
            string id,
            X509Certificate2 certificate
            );
        /// <summary>
        /// Revoke a group of certificates.
        /// Matches certificates with all active Issuer CA versions.
        /// Creates a new CRL for all Issuer CA versions.
        /// </summary>
        /// <param name="id">The group Id</param>
        /// <param name="certificates">The certificates to revoke</param>
        /// <returns>Returns certificates which could not be revoked</returns>
        Task<X509Certificate2Collection> RevokeCertificatesAsync(
            string id,
            X509Certificate2Collection certificates);
        /// <summary>
        /// Creates a new self signed Issuer CA certificate and an empty CRL.
        /// Uses subject and lifetime parameters of group configuration.
        /// </summary>
        /// <param name="id">The group id</param>
        /// <returns>The new Issuer CA cert</returns>
        Task<X509Certificate2> CreateIssuerCACertificateAsync(
            string id
            );
        /// <summary>
        /// Create a new Issuer CA signed certificate and private key.
        /// </summary>
        /// <param name="id">The group Id</param>
        /// <param name="requestId">The request Id</param>
        /// <param name="applicationUri">The application Uri for the certificate</param>
        /// <param name="subjectName">The subject for the certificate</param>
        /// <param name="domainNames">The domain names in the certificate</param>
        /// <param name="privateKeyFormat">The private key format, PFX or PEM</param>
        /// <param name="privateKeyPassword">The password for the private key</param>
        /// <returns>The new key pair</returns>
        Task<Opc.Ua.Gds.Server.X509Certificate2KeyPair> NewKeyPairRequestAsync(
            string id,
            string requestId,
            string applicationUri,
            string subjectName,
            string[] domainNames,
            string privateKeyFormat,
            string privateKeyPassword
            );

        /// <summary>
        /// Load the private key of a request from secure storage.
        /// </summary>
        /// <param name="id">The group id</param>
        /// <param name="requestId">The request id</param>
        /// <param name="privateKeyFormat">The format of the private key</param>
        /// <returns></returns>
        Task<byte[]> LoadPrivateKeyAsync(string id, string requestId, string privateKeyFormat);
        /// <summary>
        /// Accept a private key.
        /// Returns the private key and tags the key as accepted.
        /// </summary>
        /// <param name="id">The group id</param>
        /// <param name="requestId">The request id</param>
        /// <returns>The private key</returns>
        Task AcceptPrivateKeyAsync(string id, string requestId);
        /// <summary>
        /// Delete a private key.
        /// Physically deletes the private key from secure storage.
        /// </summary>
        /// <param name="id">The group id</param>
        /// <param name="requestId">The request id</param>
        Task DeletePrivateKeyAsync(string id, string requestId);
    }
}
