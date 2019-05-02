// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Types;


namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault
{
    /// <summary>
    /// An abstract interface to the certificate request database
    /// </summary>
    public interface ICertificateRequest
    {
        /// <summary>
        /// Performs setup tasks. Used to create the database if it doesn't exist.
        /// </summary>
        Task Initialize();
        /// <summary>
        /// Returns a shallow copy of the certificate group which uses
        /// a token on behalf of a user. 
        /// </summary>
        /// <param name="request">The http request with the user token</param>
        Task<ICertificateRequest> OnBehalfOfRequest(HttpRequest request);
        /// <summary>
        /// Create a new certificate request with CSR.
        /// The CSR is validated and added to the database as new request.
        /// </summary>
        /// <param name="applicationId">The applicationId</param>
        /// <param name="certificateGroupId">The certificate Group Id</param>
        /// <param name="certificateTypeId">The certificate Type Id</param>
        /// <param name="certificateRequest">The CSR</param>
        /// <param name="authorityId">The authority Id adding the request</param>
        /// <returns></returns>
        Task<string> StartSigningRequestAsync(
            string applicationId,
            string certificateGroupId,
            string certificateTypeId,
            byte[] certificateRequest,
            string authorityId);
        /// <summary>
        /// Create a new certificate request with a public/private key pair.
        /// </summary>
        /// <param name="applicationId">The application Id</param>
        /// <param name="certificateGroupId">The certificate group Id</param>
        /// <param name="certificateTypeId">The certificate Type Id</param>
        /// <param name="subjectName">The subject of the certificate</param>
        /// <param name="domainNames">The domain names for the certificate</param>
        /// <param name="privateKeyFormat">The private key format: PFX or PEM</param>
        /// <param name="privateKeyPassword">The password for the private key</param>
        /// <param name="authorityId">The authority Id adding the request</param>
        /// <returns>The request Id</returns>
        Task<string> StartNewKeyPairRequestAsync(
            string applicationId,
            string certificateGroupId,
            string certificateTypeId,
            string subjectName,
            IList<string> domainNames,
            string privateKeyFormat,
            string privateKeyPassword,
            string authorityId);
        /// <summary>
        /// Approve or reject a certificate request.
        /// The request is in approved or rejected state after the call.
        /// </summary>
        /// <param name="requestId">The request Id</param>
        /// <param name="isRejected">true if rejected, false if approved</param>
        Task ApproveAsync(
            string requestId,
            bool isRejected);
        /// <summary>
        /// Accept a certificate request.
        /// The private key of an accepted certificate request is deleted.
        /// </summary>
        /// <param name="requestId">The request Id</param>
        Task AcceptAsync(
            string requestId);
        /// <summary>
        /// Delete a certificate request.
        /// The request is marked deleted until revocation.
        /// </summary>
        /// <param name="requestId">The request Id</param>
        Task DeleteAsync(
            string requestId);
        /// <summary>
        /// The request is removed from the database.
        /// </summary>
        /// <param name="requestId">The request Id</param>
        Task PurgeAsync(
            string requestId);
        /// <summary>
        /// Revoke the certificate of a request.
        /// </summary>
        /// <param name="requestId">The request Id</param>
        Task RevokeAsync(
            string requestId);
        /// <summary>
        /// Revoke all deleted certificate requests in a group.
        /// </summary>
        /// <param name="groupId">The group Id</param>
        /// <param name="allVersions">false to revoke only the lates Issuer CA cert</param>
        Task RevokeGroupAsync(
            string groupId,
            bool? allVersions);
        /// <summary>
        /// Fetch the data of a certificate requests.
        /// Can be used to query the request state and to read an
        /// issued certificate with a private key.
        /// </summary>
        /// <param name="requestId">The request Id</param>
        /// <param name="applicationId">The application Id</param>
        /// <returns>The request</returns>
        Task<FetchRequestResultModel> FetchRequestAsync(
            string requestId,
            string applicationId
            );
        /// <summary>
        /// Read a certificate request.
        /// Returns only public information, e.g. signed certificate.
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns>The request</returns>
        Task<ReadRequestResultModel> ReadAsync(
            string requestId);
        /// <summary>
        /// Query the certificate request database.
        /// </summary>
        /// <param name="appId">Filter by ApplicationId</param>
        /// <param name="state">Filter by state, default null</param>
        /// <param name="NextPageLink">The next page</param>
        /// <param name="maxResults">max number of requests in a query</param>
        /// <returns>Array of certificate requests, next page link</returns>
        Task<(string, ReadRequestResultModel[])> QueryPageAsync(
            string appId,
            CertificateRequestState? state,
            string NextPageLink,
            int? maxResults = null
            );

    }

}
