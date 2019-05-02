// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Documents;
using Microsoft.Azure.IIoT.Exceptions;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Types;
using Microsoft.Azure.KeyVault.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CertificateRequest = Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models.CertificateRequest;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault
{
    public static class CosmosDBCertificateRequestFactory
    {
        public static ICertificateRequest Create(
            IApplicationsDatabase database,
            ICertificateGroup certificateGroup,
            IServicesConfig config,
            IDocumentDBRepository db,
            ILogger logger
            )
        {
            return new CosmosDBCertificateRequest(database, certificateGroup, config, db, logger);
        }
    }

    internal sealed class CosmosDBCertificateRequest : Object, ICertificateRequest
    {
        internal IApplicationsDatabase _applicationsDatabase;
        internal ICertificateGroup _certificateGroup;
        private readonly ILogger _log;
        private int _certRequestIdCounter = 1;

        public CosmosDBCertificateRequest(
            IApplicationsDatabase database,
            ICertificateGroup certificateGroup,
            IServicesConfig config,
            IDocumentDBRepository db,
            ILogger logger)
        {
            _applicationsDatabase = database;
            _certificateGroup = certificateGroup;
            _log = logger;
            _certificateRequests = new DocumentDBCollection<CosmosDB.Models.CertificateRequest>(db, config.CosmosDBCollection);
            // set unique key in CosmosDB for Certificate ID ()
            // db.UniqueKeyPolicy.UniqueKeys.Add(new UniqueKey { Paths = new Collection<string> { "/" + nameof(CertificateRequest.ClassType), "/" + nameof(CertificateRequest.ID) } });
            _log.Debug("Created new instance of `CosmosDBApplicationsDatabase` service " + config.CosmosDBCollection);
        }

        #region ICertificateRequest
        public async Task Initialize()
        {
            await _certificateRequests.CreateCollectionIfNotExistsAsync();
            _certRequestIdCounter = await GetMaxCertIDAsync();
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
                _log.Error(ex, "Failed to create on behalf ICertificateRequest. ");
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

            if (string.IsNullOrEmpty(certificateGroupId))
            {
                //TODO:
            }

            if (string.IsNullOrEmpty(certificateTypeId))
            {
                //TODO
            }

            CertificateRequest request = new CertificateRequest() { RequestId = Guid.NewGuid(), AuthorityId = authorityId };
            request.ID = _certRequestIdCounter++;
            request.CertificateRequestState = (int)CertificateRequestState.New;
            request.CertificateGroupId = certificateGroupId;
            request.CertificateTypeId = certificateTypeId;
            request.SubjectName = null;
            request.DomainNames = null;
            request.PrivateKeyFormat = null;
            request.PrivateKeyPassword = null;
            request.SigningRequest = certificateSigningRequest;
            request.ApplicationId = applicationId;
            request.RequestTime = DateTime.UtcNow;

            bool retry;
            do
            {
                retry = false;
                try
                {
                    var result = await _certificateRequests.CreateAsync(request);
                }
                catch (DocumentClientException dce)
                {
                    if (dce.StatusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        // retry with new guid and id
                        request.RequestId = Guid.NewGuid();
                        _certRequestIdCounter = await GetMaxCertIDAsync();
                        request.ID = _certRequestIdCounter++;
                        retry = true;
                    }
                }
            } while (retry);

            return request.RequestId.ToString();
        }

        public async Task<string> StartNewKeyPairRequestAsync(
            string applicationId,
            string certificateGroupId,
            string certificateTypeId,
            string subjectName,
            IList<string> domainNames,
            string privateKeyFormat,
            string privateKeyPassword,
            string authorityId)
        {
            Application application = await _applicationsDatabase.GetApplicationAsync(applicationId);

            if (string.IsNullOrEmpty(certificateGroupId))
            {
                //TODO
            }

            if (string.IsNullOrEmpty(certificateTypeId))
            {
                //TODO
            }

            if (string.IsNullOrEmpty(subjectName))
            {
                throw new ArgumentNullException(nameof(subjectName));
            }

            CertificateRequest request = null;
            request = new CertificateRequest()
            {
                RequestId = Guid.NewGuid(),
                AuthorityId = authorityId
            };

            var subjectList = Opc.Ua.Utils.ParseDistinguishedName(subjectName);
            if (subjectList == null ||
                subjectList.Count == 0)
            {
                throw new ArgumentException("Invalid Subject", nameof(subjectName));
            }

            if (!subjectList.Any(c => c.StartsWith("CN=", StringComparison.InvariantCulture)))
            {
                throw new ArgumentException("Invalid Subject, must have a common name (CN=).", nameof(subjectName));
            }

            // enforce proper formatting for the subject name string
            subjectName = string.Join(", ", subjectList);

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

            request.ID = _certRequestIdCounter++;
            request.CertificateRequestState = (int)CertificateRequestState.New;
            request.CertificateGroupId = certificateGroupId;
            request.CertificateTypeId = certificateTypeId;
            request.SubjectName = subjectName;
            request.DomainNames = discoveryUrlDomainNames.ToArray();
            request.PrivateKeyFormat = privateKeyFormat;
            request.PrivateKeyPassword = privateKeyPassword;
            request.SigningRequest = null;
            request.ApplicationId = application.ApplicationId.ToString();
            request.RequestTime = DateTime.UtcNow;

            bool retry;
            do
            {
                retry = false;
                try
                {
                    var result = await _certificateRequests.CreateAsync(request);
                }
                catch (DocumentClientException dce)
                {
                    if (dce.StatusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        // retry with new guid and id
                        request.RequestId = Guid.NewGuid();
                        _certRequestIdCounter = await GetMaxCertIDAsync();
                        request.ID = _certRequestIdCounter++;
                        retry = true;
                    }
                }
            } while (retry);

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

                if (request.CertificateRequestState != CertificateRequestState.New)
                {
                    throw new ResourceInvalidStateException("The record is not in a valid state for this operation.");
                }

                Application application = await _applicationsDatabase.GetApplicationAsync(request.ApplicationId);

                if (isRejected)
                {
                    request.CertificateRequestState = CertificateRequestState.Rejected;
                    // erase information which is not required anymore
                    request.PrivateKeyFormat = null;
                    request.SigningRequest = null;
                    request.PrivateKeyPassword = null;
                }
                else
                {
                    request.CertificateRequestState = CertificateRequestState.Approved;

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
                            throw new ResourceInvalidStateException(error.ToString());
                        }
                    }
                    else
                    {
                        Opc.Ua.Gds.Server.X509Certificate2KeyPair newKeyPair = null;
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
                            throw new ResourceInvalidStateException(error.ToString());
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
            bool first = true;
            do
            {
                retryUpdate = false;

                CertificateRequest request = await _certificateRequests.GetAsync(reqId);

                if (request.CertificateRequestState != CertificateRequestState.Approved)
                {
                    throw new ResourceInvalidStateException("The record is not in a valid state for this operation.");
                }

                if (request.PrivateKeyFormat != null && first)
                {
                    try
                    {
                        await _certificateGroup.DeletePrivateKeyAsync(request.CertificateGroupId, requestId);
                    }
                    catch (KeyVaultErrorException kex)
                    {
                        if (kex.Response.StatusCode != HttpStatusCode.Forbidden)
                        {
                            throw kex;
                        }
                        // ok to ignore, default KeyVault secret access 'Delete' is not granted.
                        // private key not deleted, must be handled by manager role
                    }
                }

                first = false;
                request.CertificateRequestState = CertificateRequestState.Accepted;

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
            bool first = true;
            do
            {
                retryUpdate = false;

                CertificateRequest request = await _certificateRequests.GetAsync(reqId);

                bool newStateRemoved =
                    request.CertificateRequestState == CertificateRequestState.New ||
                    request.CertificateRequestState == CertificateRequestState.Rejected;

                if (!newStateRemoved &&
                    request.CertificateRequestState != CertificateRequestState.Approved &&
                    request.CertificateRequestState != CertificateRequestState.Accepted)
                {
                    throw new ResourceInvalidStateException("The record is not in a valid state for this operation.");
                }

                request.CertificateRequestState = newStateRemoved ? CertificateRequestState.Removed : CertificateRequestState.Deleted;

                // no need to delete pk for new & rejected requests
                if (!newStateRemoved && first &&
                    request.PrivateKeyFormat != null)
                {
                    try
                    {
                        await _certificateGroup.DeletePrivateKeyAsync(request.CertificateGroupId, requestId);
                    }
                    catch (KeyVaultErrorException kex)
                    {
                        if (kex.Response.StatusCode != HttpStatusCode.Forbidden)
                        {
                            throw kex;
                        }
                        // ok to ignore, default KeyVault secret access 'Delete' is not granted.
                        // private key not deleted, must be handled by manager role
                    }
                }
                first = false;

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

                if (request.Certificate == null ||
                    request.CertificateRequestState != CertificateRequestState.Deleted)
                {
                    throw new ResourceInvalidStateException("The record is not in a valid state for this operation.");
                }

                request.CertificateRequestState = CertificateRequestState.Revoked;
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
                    throw new ResourceInvalidStateException(error.ToString());
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

            if (request.CertificateRequestState != CertificateRequestState.Revoked &&
                request.CertificateRequestState != CertificateRequestState.Rejected &&
                request.CertificateRequestState != CertificateRequestState.New &&
                request.CertificateRequestState != CertificateRequestState.Removed)
            {
                throw new ResourceInvalidStateException("The record is not in a valid state for this operation.");
            }

            await _certificateRequests.DeleteAsync(request.RequestId);
        }

        public async Task RevokeGroupAsync(string groupId, bool? allVersions)
        {
            var queryParameters = new SqlParameterCollection();
            string query = "SELECT * FROM CertificateRequest r WHERE ";
            query += " r.CertificateRequestState = @state";
            queryParameters.Add(new SqlParameter("@state", CertificateRequestState.Deleted.ToString()));
            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = query,
                Parameters = queryParameters
            };
            var deletedRequests = await _certificateRequests.GetAsync(sqlQuerySpec);
            if (deletedRequests == null ||
                deletedRequests.Count() == 0)
            {
                return;
            }

            var revokedId = new List<Guid>();
            var certCollection = new X509Certificate2Collection();
            foreach (var request in deletedRequests)
            {
                if (request.Certificate != null)
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
            }

            var remainingCertificates = await _certificateGroup.RevokeCertificatesAsync(groupId, certCollection);

            foreach (var reqId in deletedRequests)
            {
                bool retryUpdate;
                do
                {
                    retryUpdate = false;
                    CertificateRequest request = await _certificateRequests.GetAsync(reqId.RequestId);

                    if (request.CertificateRequestState != CertificateRequestState.Deleted)
                    {
                        // skip, there may have been a concurrent update to the database.
                        continue;
                    }

                    // TODO: test for remaining certificates

                    request.CertificateRequestState = CertificateRequestState.Revoked;
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

        public async Task<FetchRequestResultModel> FetchRequestAsync(
            string requestId,
            string applicationId)
        {
            Guid reqId = GetIdFromString(requestId);

            Application application = await _applicationsDatabase.GetApplicationAsync(applicationId);

            CertificateRequest request = await _certificateRequests.GetAsync(reqId);

            if (request.ApplicationId != application.ApplicationId.ToString())
            {
                throw new ArgumentException("The recordId does not match the applicationId.");
            }

            switch (request.CertificateRequestState)
            {
                case CertificateRequestState.New:
                case CertificateRequestState.Rejected:
                case CertificateRequestState.Revoked:
                case CertificateRequestState.Deleted:
                case CertificateRequestState.Removed:
                    return new FetchRequestResultModel(request.CertificateRequestState)
                    {
                        ApplicationId = applicationId,
                        RequestId = requestId
                    };
                case CertificateRequestState.Accepted:
                case CertificateRequestState.Approved:
                    break;
                default:
                    throw new ResourceInvalidStateException("The record is not in a valid state for this operation.");
            }

            // get private key
            byte[] privateKey = null;
            if (request.CertificateRequestState == CertificateRequestState.Approved &&
                request.PrivateKeyFormat != null)
            {
                try
                {
                    privateKey = await _certificateGroup.LoadPrivateKeyAsync(request.CertificateGroupId, requestId, request.PrivateKeyFormat);
                }
                catch
                {
                    // intentionally ignore error when reading private key
                    // it may have been disabled by keyvault due to inactivity...
                    request.PrivateKeyFormat = null;
                    privateKey = null;
                }
            }

            return new FetchRequestResultModel(
                request.CertificateRequestState,
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

            switch (request.CertificateRequestState)
            {
                case CertificateRequestState.New:
                case CertificateRequestState.Rejected:
                case CertificateRequestState.Accepted:
                case CertificateRequestState.Approved:
                case CertificateRequestState.Deleted:
                case CertificateRequestState.Revoked:
                case CertificateRequestState.Removed:
                    break;
                default:
                    throw new ResourceInvalidStateException("The record is not in a valid state for this operation.");
            }

            return new ReadRequestResultModel(
                requestId,
                request.ApplicationId,
                request.CertificateRequestState,
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
            var queryParameters = new SqlParameterCollection();
            string query = "SELECT * FROM CertificateRequest r WHERE ";
            if (appId == null && state == null)
            {
                query += " r.CertificateRequestState != @state";
                queryParameters.Add(new SqlParameter("@state", CertificateRequestState.Deleted.ToString()));
            }
            else if (appId != null && state != null)
            {
                query += " r.ApplicationId = @appId AND r.CertificateRequestState = @state ";
                queryParameters.Add(new SqlParameter("@appId", appId));
                queryParameters.Add(new SqlParameter("@state", state.ToString()));
            }
            else if (appId != null)
            {
                query += " r.ApplicationId = @appId";
                queryParameters.Add(new SqlParameter("@appId", appId));
            }
            else
            {
                query += " r.CertificateRequestState = @state ";
                queryParameters.Add(new SqlParameter("@state", state.ToString()));
            }
            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = query,
                Parameters = queryParameters
            };
            (nextPageLink, requests) = await _certificateRequests.GetPageAsync(sqlQuerySpec, nextPageLink, maxResults);
            List<ReadRequestResultModel> result = new List<ReadRequestResultModel>();
            foreach (CertificateRequest request in requests)
            {
                result.Add(new ReadRequestResultModel(request));
            }
            return (nextPageLink, result.ToArray());
        }
        #endregion

        #region Private Members
        private async Task<int> GetMaxCertIDAsync()
        {
            try
            {
                // find new ID for QueryServers
                SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
                {
                    QueryText = "SELECT TOP 1 * FROM Applications a WHERE a.ClassType = @classType ORDER BY a.ID DESC",
                    Parameters = new SqlParameterCollection { new SqlParameter("@classType", CertificateRequest.ClassTypeName) }
                };
                var maxIDEnum = await _certificateRequests.GetAsync(sqlQuerySpec);
                var maxID = maxIDEnum.SingleOrDefault();
                return (maxID != null) ? maxID.ID + 1 : 1;
            }
            catch
            {
                return 1;
            }
        }

        private Guid GetIdFromString(string requestId)
        {
            try
            {
                if (String.IsNullOrEmpty(requestId))
                {
                    throw new ArgumentNullException(nameof(requestId), "The request id must be provided");
                }
                Guid guidId = new Guid(requestId);
                if (guidId == null || guidId == Guid.Empty)
                {
                    throw new ArgumentException("The id must be provided.", nameof(requestId));
                }
                return guidId;
            }
            catch (FormatException)
            {
                throw new ArgumentException("The requestId is invalid.");
            }
        }
        #endregion

        #region Private Fields
        private DateTime _queryCounterResetTime = DateTime.UtcNow;
        private IDocumentDBCollection<CertificateRequest> _certificateRequests;
        #endregion
    }
}
