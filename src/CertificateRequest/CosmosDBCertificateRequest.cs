// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Azure.IIoT.Diagnostics;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.CosmosDB;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.CosmosDB.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.Runtime;
using Opc.Ua;
using Opc.Ua.Gds.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CertificateRequest = Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.CosmosDB.Models.CertificateRequest;
using CertificateRequestState = Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.CosmosDB.Models.CertificateRequestState;

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault
{
    internal sealed class CosmosDBCertificateRequest : ICertificateRequest
    {
        private ExpandedNodeId DefaultApplicationGroupId;
        private ExpandedNodeId DefaultHttpsGroupId;
        private ExpandedNodeId DefaultUserTokenGroupId;

        private readonly ILogger _log;
        private readonly IApplicationsDatabase _database;
        private readonly ICertificateGroup _certificateGroup;
        private readonly string _endpoint;
        private SecureString _authKeyOrResourceToken;

        public CosmosDBCertificateRequest(
            IApplicationsDatabase database,
            ICertificateGroup certificateGroup,
            IServicesConfig config,
            ILogger logger)
        {
            _database = database;
            _certificateGroup = certificateGroup;
            _endpoint = config.CosmosDBEndpoint;
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
            db = new DocumentDBRepository(_endpoint, _authKeyOrResourceToken);
            Applications = new DocumentDBCollection<Application>(db);
            CertificateRequests = new DocumentDBCollection<CosmosDB.Models.CertificateRequest>(db);
            return Task.CompletedTask;
        }

        public async Task<string> StartSigningRequestAsync(
            string applicationId,
            string certificateGroupId,
            string certificateTypeId,
            byte[] certificateSigningRequest,
            string authorityId)
        {
            Guid appId = GetIdFromString(applicationId);

            // TODO: use IApplicationsDatabase
            Application application = await Applications.GetAsync(appId);
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

            // TODO: do we need updates?

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
                await CertificateRequests.CreateAsync(request);
            }
            else
            {
                await CertificateRequests.UpdateAsync(request.RequestId, request);
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
            Guid appId = GetIdFromString(applicationId);

            Application application = await Applications.GetAsync(appId);
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

            request.State = (int)CertificateRequestState.New;
            request.CertificateGroupId = certificateGroupId;
            request.CertificateTypeId = certificateTypeId;
            request.SubjectName = subjectName;
            request.DomainNames = domainNames;
            request.PrivateKeyFormat = privateKeyFormat;
            request.PrivateKeyPassword = privateKeyPassword;
            request.SigningRequest = null;
            request.ApplicationId = appId.ToString();
            request.RequestTime = DateTime.UtcNow;

            if (isNew)
            {
                await CertificateRequests.CreateAsync(request);
            }
            else
            {
                await CertificateRequests.UpdateAsync(request.RequestId, request);
            }

            return request.RequestId.ToString();
        }

        public async Task ApproveAsync(
            string requestId,
            bool isRejected
            )
        {
            Guid reqId = GetIdFromString(requestId);

            CertificateRequest request = await CertificateRequests.GetAsync(reqId);
            if (request == null)
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdUnknown, "Unknown request id");
            }

            if (request.State != CertificateRequestState.New)
            {
                throw new ServiceResultException(StatusCodes.BadInvalidState);
            }

            Guid appId = new Guid(request.ApplicationId);
            Application application = await Applications.GetAsync(appId);
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
                        request.PrivateKey = null;
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
                    request.PrivateKey = newKeyPair.PrivateKey;

                }
            }

            request.ApproveRejectTime = DateTime.UtcNow;

            await CertificateRequests.UpdateAsync(reqId, request);
        }

        public async Task AcceptAsync(
            string requestId)
        {
            Guid reqId = GetIdFromString(requestId);

            CertificateRequest request = await CertificateRequests.GetAsync(reqId);
            if (request == null)
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
            }

            if (request.State != CertificateRequestState.Approved)
            {
                throw new ServiceResultException(StatusCodes.BadInvalidState);
            }

            request.State = CertificateRequestState.Accepted;

            // erase information which is not required anymore
            request.SigningRequest = null;
            request.PrivateKeyFormat = null;
            request.PrivateKeyPassword = null;
            request.AcceptTime = DateTime.UtcNow;

            await CertificateRequests.UpdateAsync(request.RequestId, request);
        }

        public async Task<FinishRequestResultModel> FinishRequestAsync(
            string requestId,
            string applicationId)
        {
            Guid reqId = GetIdFromString(requestId);
            Guid appId = GetIdFromString(applicationId);

            Application application = await Applications.GetAsync(appId);
            if (application == null)
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
            }

            CertificateRequest request = await CertificateRequests.GetAsync(reqId);
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

            if (request.ApplicationId != appId.ToString())
            {
                throw new ServiceResultException(StatusCodes.BadNodeIdUnknown);
            }

            return new FinishRequestResultModel(
                request.State,
                applicationId,
                requestId,
                request.CertificateGroupId,
                request.CertificateTypeId,
                request.Certificate,
                request.PrivateKeyFormat,
                request.PrivateKey,
                request.AuthorityId);
        }

        public async Task<ReadRequestResultModel> ReadAsync(
            string requestId
            )
        {
            Guid reqId = GetIdFromString(requestId);

            CertificateRequest request = await CertificateRequests.GetAsync(reqId);
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
                request.PrivateKeyFormat,
                request.PrivateKeyPassword);

        }

        public async Task<ReadRequestResultModel[]> QueryAsync(
            string appId,
            CertificateRequestState? state)
        {
            IEnumerable<CertificateRequest> requests;
            if (appId == null && state == null)
            {
                requests = await CertificateRequests.GetAsync(x => true);
            }
            else if (appId != null && state != null)
            {
                requests = await CertificateRequests.GetAsync(x => x.ApplicationId == appId && x.State == state);
            }
            else if (appId != null)
            {
                requests = await CertificateRequests.GetAsync(x => x.ApplicationId == appId);
            }
            else
            {
                requests = await CertificateRequests.GetAsync(x => x.State == state);
            }
            List<ReadRequestResultModel> result = new List<ReadRequestResultModel>();
            foreach (CertificateRequest request in requests)
            {
                result.Add(new ReadRequestResultModel(request));
            }
            return result.ToArray();
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
        private DateTime queryCounterResetTime = DateTime.UtcNow;
        private DocumentDBRepository db;
        // TODO: remove direct access to aplication DB
        private IDocumentDBCollection<Application> Applications;
        private IDocumentDBCollection<CertificateRequest> CertificateRequests;
        #endregion
    }
}
