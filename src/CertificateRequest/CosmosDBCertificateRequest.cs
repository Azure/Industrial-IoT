// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Documents;
using Microsoft.Azure.IIoT.Diagnostics;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;
using Opc.Ua;
using Opc.Ua.Gds.Server;
using CertificateRequest = Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models.CertificateRequest;
using CertificateRequestState = Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models.CertificateRequestState;
using StatusCodes = Opc.Ua.StatusCodes;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault
{
    internal sealed class CosmosDBCertificateRequest : Object, ICertificateRequest
    {
        public readonly ExpandedNodeId DefaultApplicationGroupId;
        public readonly ExpandedNodeId DefaultHttpsGroupId;
        public readonly ExpandedNodeId DefaultUserTokenGroupId;

        internal IApplicationsDatabase _applicationsDatabase;
        internal ICertificateGroup _certificateGroup;
        private readonly string _endpoint;
        private readonly string _dataBaseId;
        private readonly string _collectionId;
        private readonly ILogger _log;
        private SecureString _authKeyOrResourceToken;

        public CosmosDBCertificateRequest(
            IApplicationsDatabase database,
            ICertificateGroup certificateGroup,
            IServicesConfig config,
            ILogger logger)
        {
            _applicationsDatabase = database;
            _certificateGroup = certificateGroup;
            _dataBaseId = config.CosmosDBDatabase;
            _endpoint = config.CosmosDBEndpoint;
            _collectionId = config.CosmosDBCollection;
            _authKeyOrResourceToken = new SecureString();
            foreach (char ch in config.CosmosDBToken)
            {
                _authKeyOrResourceToken.AppendChar(ch);
            }
            _log = logger;
            _log.Debug("Creating new instance of `CosmosDBApplicationsDatabase` service " + config.CosmosDBEndpoint, () => { });
            Initialize();

            // well known groups
            DefaultApplicationGroupId = Opc.Ua.Gds.ObjectIds.Directory_CertificateGroups_DefaultApplicationGroup;
            DefaultHttpsGroupId = Opc.Ua.Gds.ObjectIds.Directory_CertificateGroups_DefaultHttpsGroup;
            DefaultUserTokenGroupId = Opc.Ua.Gds.ObjectIds.Directory_CertificateGroups_DefaultUserTokenGroup;

        }

        #region ICertificateRequest
        public Task Initialize()
        {
            var db = new DocumentDBRepository(_endpoint, _dataBaseId, _authKeyOrResourceToken);
            _certificateRequests = new DocumentDBCollection<CosmosDB.Models.CertificateRequest>(db, _collectionId);
            return Task.CompletedTask;
        }

        public async Task<ICertificateRequest> OnBehalfOfRequest(HttpRequest request)
        {
            try
            {
                var onBehalfOfCertificateGroup = await _certificateGroup.OnBehalfOfRequest(request);
                var certRequest = (CosmosDBCertificateRequest)this.MemberwiseClone();
                certRequest._certificateGroup = onBehalfOfCertificateGroup;
                return certRequest;
            }
            catch (Exception ex)
            {
                // try default 
                _log.Error("Failed to create on behalf ICertificateRequest. ", () => new { ex });
            }
            return this;
        }

        public async Task<string> StartSigningRequestAsync(
            string applicationId,
            string certificateGroupId,
            string certificateTypeId,
            byte[] certificateSigningRequest,
            string authorityId)
        {
            Application application = await _applicationsDatabase.GetApplicationAsync(applicationId);
            if (application == null)
            {
                throw new ServiceResultException(StatusCodes.BadNotFound, "The ApplicationId does not refer to a valid application.");
            }

            if (string.IsNullOrEmpty(certificateGroupId))
            {
                //TODO:
            }

            if (string.IsNullOrEmpty(certificateTypeId))
            {
                //TODO
            }

            CertificateRequest request = null;
            bool isNew = false;

            if (request == null)
            {
                request = new CertificateRequest() { RequestId = Guid.NewGuid(), AuthorityId = authorityId };
                isNew = true;
            }

            request.State = (int)CertificateRequestState.New;
            request.CertificateGroupId = certificateGroupId;
            request.CertificateTypeId = certificateTypeId;
            request.SubjectName = null;
            request.DomainNames = null;
            request.PrivateKeyFormat = null;
            request.PrivateKeyPassword = null;
            request.SigningRequest = certificateSigningRequest;
            request.ApplicationId = applicationId;
            request.RequestTime = DateTime.UtcNow;

            if (isNew)
            {
                await _certificateRequests.CreateAsync(request);
            }

            return request.RequestId.ToString();
        }

        public async Task<string> StartNewKeyPairRequestAsync(
            string applicationId,
            string certificateGroupId,
            string certificateTypeId,
            string subjectName,
            string[] domainNames,
            string privateKeyFormat,
            string privateKeyPassword,
            string authorityId)
        {
            Application application = await _applicationsDatabase.GetApplicationAsync(applicationId);
            if (application == null)
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
            }

            if (string.IsNullOrEmpty(certificateGroupId))
            {
                //TODO:
            }

            if (string.IsNullOrEmpty(certificateTypeId))
            {
                //TODO
            }

            CertificateRequest request = null;
            bool isNew = false;
            if (request == null)
            {
                request = new CertificateRequest()
                {
                    RequestId = Guid.NewGuid(),
                    AuthorityId = authorityId
                };
                isNew = true;
            }

            List<string> discoveryUrlDomainNames = new List<string>();
            if (domainNames != null)
            {
                foreach (var domainName in domainNames)
                {
                    if (!String.IsNullOrWhiteSpace(domainName))
                    {
                        string ipAddress = Opc.Ua.Utils.NormalizedIPAddress(domainName);
                        if (!String.IsNullOrEmpty(ipAddress))
                        {
                            discoveryUrlDomainNames.Add(ipAddress);
                        }
                        else
                        {
                            discoveryUrlDomainNames.Add(domainName);
                        }
                    }
                }
            }
            else
            {
                discoveryUrlDomainNames = new List<string>();
            }

            if (application.DiscoveryUrls != null)
            {
                foreach (var discoveryUrl in application.DiscoveryUrls)
                {
                    Uri url = Opc.Ua.Utils.ParseUri(discoveryUrl);

                    if (url == null)
                    {
                        continue;
                    }

                    string domainName = url.DnsSafeHost;

                    if (url.HostNameType != UriHostNameType.Dns)
                    {
                        domainName = Opc.Ua.Utils.NormalizedIPAddress(domainName);
                    }

                    if (!Opc.Ua.Utils.FindStringIgnoreCase(discoveryUrlDomainNames, domainName))
                    {
                        discoveryUrlDomainNames.Add(domainName);
                    }
                }
            }

            request.State = (int)CertificateRequestState.New;
            request.CertificateGroupId = certificateGroupId;
            request.CertificateTypeId = certificateTypeId;
            request.SubjectName = subjectName;
            request.DomainNames = discoveryUrlDomainNames.ToArray();
            request.PrivateKeyFormat = privateKeyFormat;
            request.PrivateKeyPassword = privateKeyPassword;
            request.SigningRequest = null;
            request.ApplicationId = application.ApplicationId.ToString();
            request.RequestTime = DateTime.UtcNow;

            if (isNew)
            {
                await _certificateRequests.CreateAsync(request);
            }

            return request.RequestId.ToString();
        }

        public async Task ApproveAsync(
            string requestId,
            bool isRejected
            )
        {
            Guid reqId = GetIdFromString(requestId);

            bool retryUpdate;
            do
            {
                retryUpdate = false;
                CertificateRequest request = await _certificateRequests.GetAsync(reqId);

                if (request == null)
                {
                    throw new ServiceResultException(StatusCodes.BadNodeIdUnknown, "Unknown request id");
                }

                if (request.State != CertificateRequestState.New)
                {
                    throw new ServiceResultException(StatusCodes.BadInvalidState);
                }

                Application application = await _applicationsDatabase.GetApplicationAsync(request.ApplicationId);
                if (application == null)
                {
                    throw new ServiceResultException(StatusCodes.BadNodeIdUnknown, "Unknown application id");
                }

                if (isRejected)
                {
                    request.State = CertificateRequestState.Rejected;
                    // erase information which is not required anymore
                    request.PrivateKeyFormat = null;
                    request.SigningRequest = null;
                    request.PrivateKeyPassword = null;
                }
                else
                {
                    request.State = CertificateRequestState.Approved;

                    X509Certificate2 certificate;
                    if (request.SigningRequest != null)
                    {
                        try
                        {
                            certificate = await _certificateGroup.SigningRequestAsync(
                                request.CertificateGroupId,
                                application.ApplicationUri,
                                request.SigningRequest
                                );

                            request.Certificate = certificate.RawData;
                        }
                        catch (Exception e)
                        {
                            StringBuilder error = new StringBuilder();

                            error.Append("Error Generating Certificate=" + e.Message);
                            error.Append("\r\nApplicationId=" + application.ApplicationId);
                            error.Append("\r\nApplicationUri=" + application.ApplicationUri);
                            error.Append("\r\nApplicationName=" + application.ApplicationNames[0].Text);

                            throw new ServiceResultException(StatusCodes.BadConfigurationError, error.ToString());
                        }
                    }
                    else
                    {
                        X509Certificate2KeyPair newKeyPair = null;
                        try
                        {
                            newKeyPair = await _certificateGroup.NewKeyPairRequestAsync(
                                request.CertificateGroupId,
                                requestId,
                                application.ApplicationUri,
                                request.SubjectName,
                                request.DomainNames,
                                request.PrivateKeyFormat,
                                request.PrivateKeyPassword);
                        }
                        catch (Exception e)
                        {
                            StringBuilder error = new StringBuilder();

                            error.Append("Error Generating New Key Pair Certificate=" + e.Message);
                            error.Append("\r\nApplicationId=" + application.ApplicationId);
                            error.Append("\r\nApplicationUri=" + application.ApplicationUri);

                            throw new ServiceResultException(StatusCodes.BadConfigurationError, error.ToString());
                        }

                        request.Certificate = newKeyPair.Certificate.RawData;
                        // ignore private key, it is stored in KeyVault
                    }
                }

                request.ApproveRejectTime = DateTime.UtcNow;
                try
                {
                    await _certificateRequests.UpdateAsync(reqId, request, request.ETag);
                }
                catch (DocumentClientException dce)
                {
                    if (dce.StatusCode == HttpStatusCode.PreconditionFailed)
                    {
                        retryUpdate = true;
                    }
                }
            } while (retryUpdate);

        }

        public async Task AcceptAsync(
                string requestId)
        {
            Guid reqId = GetIdFromString(requestId);
            bool retryUpdate;
            do
            {
                retryUpdate = false;

                CertificateRequest request = await _certificateRequests.GetAsync(reqId);
                if (request == null)
                {
                    throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
                }

                if (request.State != CertificateRequestState.Approved)
                {
                    throw new ServiceResultException(StatusCodes.BadInvalidState);
                }

                await _certificateGroup.AcceptPrivateKeyAsync(request.CertificateGroupId, requestId);

                request.State = CertificateRequestState.Accepted;

                // erase information which is not required anymore
                request.SigningRequest = null;
                request.PrivateKeyFormat = null;
                request.PrivateKeyPassword = null;
                request.AcceptTime = DateTime.UtcNow;
                try
                {
                    await _certificateRequests.UpdateAsync(request.RequestId, request, request.ETag);
                }
                catch (DocumentClientException dce)
                {
                    if (dce.StatusCode == HttpStatusCode.PreconditionFailed)
                    {
                        retryUpdate = true;
                    }
                }
            } while (retryUpdate);

        }

        public async Task DeleteAsync(string requestId)
        {
            Guid reqId = GetIdFromString(requestId);

            bool retryUpdate;
            do
            {
                retryUpdate = false;

                CertificateRequest request = await _certificateRequests.GetAsync(reqId);
                if (request == null)
                {
                    throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
                }

                request.State = CertificateRequestState.Deleted;

                await _certificateGroup.DeletePrivateKeyAsync(request.CertificateGroupId, requestId);

                // erase information which is not required anymore
                request.SigningRequest = null;
                request.PrivateKeyFormat = null;
                request.PrivateKeyPassword = null;
                request.DeleteTime = DateTime.UtcNow;
                try
                {
                    await _certificateRequests.UpdateAsync(request.RequestId, request, request.ETag);
                }
                catch (DocumentClientException dce)
                {
                    if (dce.StatusCode == HttpStatusCode.PreconditionFailed)
                    {
                        retryUpdate = true;
                    }
                }
            } while (retryUpdate);

        }

        public async Task RevokeAsync(string requestId)
        {
            Guid reqId = GetIdFromString(requestId);

            bool retryUpdate;
            do
            {
                retryUpdate = false;
                CertificateRequest request = await _certificateRequests.GetAsync(reqId);

                if (request == null)
                {
                    throw new ServiceResultException(StatusCodes.BadNodeIdUnknown, "Unknown request id");
                }

                if (request.State != CertificateRequestState.Deleted)
                {
                    throw new ServiceResultException(StatusCodes.BadInvalidState);
                }

                if (request.Certificate == null)
                {
                    throw new ServiceResultException(StatusCodes.BadInvalidState);
                }

                request.State = CertificateRequestState.Revoked;
                // erase information which is not required anymore
                request.PrivateKeyFormat = null;
                request.SigningRequest = null;
                request.PrivateKeyPassword = null;

                try
                {
                    var cert = new X509Certificate2(request.Certificate);
                    var crl = await _certificateGroup.RevokeCertificateAsync(request.CertificateGroupId, cert);
                }
                catch (Exception e)
                {
                    StringBuilder error = new StringBuilder();

                    error.Append("Error Revoking Certificate=" + e.Message);
                    error.Append("\r\nGroupId=" + request.CertificateGroupId);
                    throw new ServiceResultException(StatusCodes.BadConfigurationError, error.ToString());
                }

                request.RevokeTime = DateTime.UtcNow;

                try
                {
                    await _certificateRequests.UpdateAsync(reqId, request, request.ETag);
                }
                catch (DocumentClientException dce)
                {
                    if (dce.StatusCode == HttpStatusCode.PreconditionFailed)
                    {
                        retryUpdate = true;
                    }
                }
            } while (retryUpdate);
        }

        public async Task PurgeAsync(string requestId)
        {
            Guid reqId = GetIdFromString(requestId);

            CertificateRequest request = await _certificateRequests.GetAsync(reqId);
            if (request == null)
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
            }
            if (request.State != CertificateRequestState.Revoked &&
                request.State != CertificateRequestState.Rejected &&
                request.State != CertificateRequestState.New)
            {
                throw new ServiceResultException(StatusCodes.BadInvalidState);
            }

            await _certificateRequests.DeleteAsync(request.RequestId);
        }

        public async Task RevokeGroupAsync(string groupId, bool? allVersions)
        {
            // TODO: implement all versions to renew all CSR for all CA versions
            var deletedRequests = await _certificateRequests.GetAsync(x => x.State == CertificateRequestState.Deleted);
            if (deletedRequests == null ||
                deletedRequests.Count() == 0)
            {
                return;
            }

            var revokedId = new List<Guid>();
            var certCollection = new X509Certificate2Collection();
            foreach (var request in deletedRequests)
            {
                if (String.Compare(request.CertificateGroupId, groupId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    try
                    {
                        var cert = new X509Certificate2(request.Certificate);
                        certCollection.Add(cert);
                        revokedId.Add(request.RequestId);
                    }
                    catch
                    {
                        // skip 
                    }

                }
            }

            await _certificateGroup.RevokeCertificatesAsync(groupId, certCollection);

            foreach (var reqId in deletedRequests)
            {
                bool retryUpdate;
                do
                {
                    retryUpdate = false;
                    CertificateRequest request = await _certificateRequests.GetAsync(reqId.RequestId);

                    if (request == null)
                    {
                        throw new ServiceResultException(StatusCodes.BadNodeIdUnknown, "Unknown request id");
                    }

                    if (request.State != CertificateRequestState.Deleted)
                    {
                        throw new ServiceResultException(StatusCodes.BadInvalidState);
                    }

                    if (request.Certificate == null)
                    {
                        throw new ServiceResultException(StatusCodes.BadInvalidState);
                    }

                    request.State = CertificateRequestState.Revoked;
                    request.RevokeTime = DateTime.UtcNow;
                    // erase information which is not required anymore
                    request.Certificate = null;
                    request.PrivateKeyFormat = null;
                    request.SigningRequest = null;
                    request.PrivateKeyPassword = null;

                    try
                    {
                        await _certificateRequests.UpdateAsync(reqId.RequestId, request, request.ETag);
                    }
                    catch (DocumentClientException dce)
                    {
                        if (dce.StatusCode == HttpStatusCode.PreconditionFailed)
                        {
                            retryUpdate = true;
                        }
                    }
                } while (retryUpdate);
            }
        }


        public async Task<FinishRequestResultModel> FinishRequestAsync(
            string requestId,
            string applicationId)
        {
            Guid reqId = GetIdFromString(requestId);

            Application application = await _applicationsDatabase.GetApplicationAsync(applicationId);
            if (application == null)
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
            }

            CertificateRequest request = await _certificateRequests.GetAsync(reqId);
            if (request == null)
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
            }

            switch (request.State)
            {
                case CertificateRequestState.New:
                case CertificateRequestState.Rejected:
                case CertificateRequestState.Accepted:
                    return new FinishRequestResultModel(request.State);
                case CertificateRequestState.Approved:
                    break;
                default:
                    throw new ServiceResultException(StatusCodes.BadInvalidArgument);
            }

            if (request.ApplicationId != application.ApplicationId.ToString())
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
            }

            // get private key
            byte[] privateKey = await _certificateGroup.LoadPrivateKeyAsync(request.CertificateGroupId, requestId, request.PrivateKeyFormat);

            return new FinishRequestResultModel(
                request.State,
                applicationId,
                requestId,
                request.CertificateGroupId,
                request.CertificateTypeId,
                request.Certificate,
                request.PrivateKeyFormat,
                privateKey,
                request.AuthorityId);
        }

        public async Task<ReadRequestResultModel> ReadAsync(
            string requestId
            )
        {
            Guid reqId = GetIdFromString(requestId);

            CertificateRequest request = await _certificateRequests.GetAsync(reqId);
            if (request == null)
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
            }

            switch (request.State)
            {
                case CertificateRequestState.New:
                case CertificateRequestState.Rejected:
                case CertificateRequestState.Accepted:
                case CertificateRequestState.Approved:
                case CertificateRequestState.Deleted:
                case CertificateRequestState.Revoked:
                    break;
                default:
                    throw new ServiceResultException(StatusCodes.BadInvalidArgument);
            }

            return new ReadRequestResultModel(
                requestId,
                request.ApplicationId,
                request.State,
                request.CertificateGroupId,
                request.CertificateTypeId,
                request.SigningRequest,
                request.SubjectName,
                request.DomainNames,
                request.PrivateKeyFormat);

        }

        public async Task<(string, ReadRequestResultModel[])> QueryPageAsync(
            string appId,
            CertificateRequestState? state,
            string nextPageLink,
            int? maxResults)
        {
            IEnumerable<CertificateRequest> requests;
            if (appId == null && state == null)
            {
                (nextPageLink, requests) = await _certificateRequests.GetPageAsync(x => x.State < CertificateRequestState.Deleted, nextPageLink, maxResults);
            }
            else if (appId != null && state != null)
            {
                (nextPageLink, requests) = await _certificateRequests.GetPageAsync(x => x.ApplicationId == appId && x.State == state, nextPageLink, maxResults);
            }
            else if (appId != null)
            {
                (nextPageLink, requests) = await _certificateRequests.GetPageAsync(x => x.ApplicationId == appId, nextPageLink, maxResults);
            }
            else
            {
                (nextPageLink, requests) = await _certificateRequests.GetPageAsync(x => x.State == state, nextPageLink, maxResults);
            }
            List<ReadRequestResultModel> result = new List<ReadRequestResultModel>();
            foreach (CertificateRequest request in requests)
            {
                result.Add(new ReadRequestResultModel(request));
            }
            return (nextPageLink, result.ToArray());
        }
        #endregion

        #region Private Members
        private string CreateServerQuery(uint startingRecordId, uint maxRecordsToQuery)
        {
            string query;
            if (maxRecordsToQuery != 0)
            {
                query = string.Format("SELECT TOP {0}", maxRecordsToQuery);
            }
            else
            {
                query = string.Format("SELECT");
            }
            query += string.Format(" * FROM Applications a WHERE a.ID >= {0} ORDER BY a.ID", startingRecordId);
            return query;
        }

        private Guid VerifyRegisterApplication(Application application)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (application.ApplicationUri == null)
            {
                throw new ArgumentNullException("ApplicationUri");
            }

            if (!Uri.IsWellFormedUriString(application.ApplicationUri, UriKind.Absolute))
            {
                throw new ArgumentException(application.ApplicationUri + " is not a valid URI.", "ApplicationUri");
            }

            if (application.ApplicationType < (int)Opc.Ua.ApplicationType.Server ||
                application.ApplicationType > (int)Opc.Ua.ApplicationType.DiscoveryServer)
            {
                throw new ArgumentException(application.ApplicationType.ToString() + " is not a valid ApplicationType.", "ApplicationType");
            }

            if (application.ApplicationNames == null || application.ApplicationNames.Length == 0 || string.IsNullOrEmpty(application.ApplicationNames[0].Text))
            {
                throw new ArgumentException("At least one ApplicationName must be provided.", "ApplicationNames");
            }

            if (string.IsNullOrEmpty(application.ProductUri))
            {
                throw new ArgumentException("A ProductUri must be provided.", "ProductUri");
            }

            if (!Uri.IsWellFormedUriString(application.ProductUri, UriKind.Absolute))
            {
                throw new ArgumentException(application.ProductUri + " is not a valid URI.", "ProductUri");
            }

            if (application.DiscoveryUrls != null)
            {
                foreach (string discoveryUrl in application.DiscoveryUrls)
                {
                    if (string.IsNullOrEmpty(discoveryUrl))
                    {
                        continue;
                    }

                    if (!Uri.IsWellFormedUriString(discoveryUrl, UriKind.Absolute))
                    {
                        throw new ArgumentException(discoveryUrl + " is not a valid URL.", "DiscoveryUrls");
                    }
                }
            }

            if (application.ApplicationType != (int)Opc.Ua.ApplicationType.Client)
            {
                if (application.DiscoveryUrls == null || application.DiscoveryUrls.Length == 0)
                {
                    throw new ArgumentException("At least one DiscoveryUrl must be provided.", "DiscoveryUrls");
                }

                if (application.ServerCapabilities == null || application.ServerCapabilities.Length == 0)
                {
                    throw new ArgumentException("At least one Server Capability must be provided.", "ServerCapabilities");
                }
            }
            else
            {
                if (application.DiscoveryUrls != null && application.DiscoveryUrls.Length > 0)
                {
                    throw new ArgumentException("DiscoveryUrls must not be specified for clients.", "DiscoveryUrls");
                }
            }

            // TODO check type
            //if (application.ApplicationId == Guid.Empty)
            //{
            //    throw new ArgumentException("The ApplicationId has invalid type {0}", application.ApplicationId.ToString());
            //}

            return application.ApplicationId;
        }

        private static string ServerCapabilities(Application application)
        {
            if (application.ApplicationType != (int)Opc.Ua.ApplicationType.Client)
            {
                if (application.ServerCapabilities == null || application.ServerCapabilities.Length == 0)
                {
                    throw new ArgumentException("At least one Server Capability must be provided.", "ServerCapabilities");
                }
            }

            StringBuilder capabilities = new StringBuilder();
            if (application.ServerCapabilities != null)
            {
                System.Collections.Generic.List<string> sortedCaps = application.ServerCapabilities.Split(",").ToList();
                sortedCaps.Sort();
                foreach (string capability in sortedCaps)
                {
                    if (string.IsNullOrEmpty(capability))
                    {
                        continue;
                    }

                    if (capabilities.Length > 0)
                    {
                        capabilities.Append(',');
                    }

                    capabilities.Append(capability);
                }
            }

            return capabilities.ToString();
        }

        private Guid GetIdFromString(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            Guid guidId = new Guid(id);

            if (guidId == null || guidId == Guid.Empty)
            {
                throw new ArgumentException("The id must be provided.", nameof(id));
            }

            if (id == null)
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
            }

            return guidId;
        }
        #endregion

        #region Private Fields
        private DateTime _queryCounterResetTime = DateTime.UtcNow;
        private IDocumentDBCollection<CertificateRequest> _certificateRequests;
        #endregion
    }
}
