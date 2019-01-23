// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Azure.Documents;
using Microsoft.Azure.IIoT.Diagnostics;
using Microsoft.Azure.IIoT.Exceptions;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;
using Opc.Ua.Gds.Server.Database;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault
{
    internal sealed class CosmosDBApplicationsDatabase : IApplicationsDatabase
    {
        const int _defaultRecordsPerQuery = 10;
        private readonly ILogger _log;
        private readonly string _endpoint;
        private readonly string _dataBaseId;
        private readonly string _collectionId;
        private readonly SecureString _authKeyOrResourceToken;
        private readonly ILifetimeScope _scope = null;

        public CosmosDBApplicationsDatabase(
            ILifetimeScope scope,
            IServicesConfig config,
            ILogger logger)
        {
            _scope = scope;
            _endpoint = config.CosmosDBEndpoint;
            _dataBaseId = config.CosmosDBDatabase;
            _collectionId = config.CosmosDBCollection;
            _authKeyOrResourceToken = new SecureString();
            foreach (char ch in config.CosmosDBToken)
            {
                _authKeyOrResourceToken.AppendChar(ch);
            }
            _log = logger;
            _log.Debug("Creating new instance of `CosmosDBApplicationsDatabase` service " + config.CosmosDBEndpoint, () => { });
            Initialize();
        }

        #region IApplicationsDatabase
        public async Task<string> RegisterApplicationAsync(Application application)
        {
            Guid applicationId = VerifyRegisterApplication(application);
            if (Guid.Empty != applicationId)
            {
                return await UpdateApplicationAsync(application.ApplicationId.ToString(), application);
            }

            application.ID = await GetMaxAppIDAsync();
            application.CreateTime = application.UpdateTime = DateTime.UtcNow;
            application.ApplicationId = Guid.NewGuid();
            var result = await _applications.CreateAsync(application);
            applicationId = new Guid(result.Id);

            return applicationId.ToString();
        }

        public async Task<string> UpdateApplicationAsync(string id, Application application)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentException("The id must be provided", nameof(id));
            }

            if (application == null)
            {
                throw new ArgumentException("The application must be provided", nameof(application));
            }

            Guid recordId = VerifyRegisterApplication(application);
            Guid applicationId = new Guid(id);
            if (applicationId == Guid.Empty)
            {
                throw new ArgumentException("The applicationId is invalid", nameof(id));
            }

            string capabilities = ServerCapabilities(application);

            if (applicationId != Guid.Empty)
            {
                bool retryUpdate;
                do
                {
                    retryUpdate = false;

                    var record = await _applications.GetAsync(applicationId);
                    if (record == null)
                    {
                        throw new ArgumentException("A record with the specified application id does not exist.", nameof(id));
                    }

                    if (record.ID == 0)
                    {
                        record.ID = await GetMaxAppIDAsync();
                    }
                    record.UpdateTime = DateTime.UtcNow;

                    record.ApplicationUri = application.ApplicationUri;
                    record.ApplicationName = application.ApplicationName;
                    record.ApplicationType = application.ApplicationType;
                    record.ProductUri = application.ProductUri;
                    record.ServerCapabilities = capabilities;
                    record.ApplicationNames = application.ApplicationNames;
                    record.DiscoveryUrls = application.DiscoveryUrls;
                    try
                    {
                        await _applications.UpdateAsync(applicationId, record, record.ETag);
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
            return applicationId.ToString();
        }

        public async Task UnregisterApplicationAsync(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentException("The id must be provided", nameof(id));
            }

            Guid appId = new Guid(id);

            List<byte[]> certificates = new List<byte[]>();

            var application = await _applications.GetAsync(appId);
            if (application == null)
            {
                throw new ResourceNotFoundException("A record with the specified application id does not exist.");
            }

            ICertificateRequest certificateRequestsService = _scope.Resolve<ICertificateRequest>();
            // mark all requests as deleted
            ReadRequestResultModel[] certificateRequests;
            string nextPageLink = null;
            do
            {
                (nextPageLink, certificateRequests) = await certificateRequestsService.QueryPageAsync(appId.ToString(), null, nextPageLink);
                foreach (var request in certificateRequests)
                {
                    await certificateRequestsService.DeleteAsync(request.RequestId);
                }
            } while (nextPageLink != null);

            await _applications.DeleteAsync(appId);
        }

        public async Task<Application> GetApplicationAsync(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentException("The id must be provided", nameof(id));
            }

            Guid appId = new Guid(id);
            return await _applications.GetAsync(appId);
        }

        public async Task<Application[]> FindApplicationAsync(string applicationUri)
        {
            if (String.IsNullOrEmpty(applicationUri))
            {
                throw new ArgumentException("The applicationUri must be provided", nameof(applicationUri));
            }

            var results = await _applications.GetAsync(ii => ii.ApplicationUri == applicationUri);
            return results.ToArray();
        }

        public async Task<QueryApplicationsResponseModel> QueryApplicationsAsync(
            uint startingRecordId,
            uint maxRecordsToReturn,
            string applicationName,
            string applicationUri,
            uint applicationType,
            string productUri,
            string[] serverCapabilities
            )
        {
            // TODO: implement last query time
            DateTime lastCounterResetTime = DateTime.MinValue;
            uint nextRecordId = 0;

            List<Application> records = new List<Application>();

            bool matchQuery = false;
            bool complexQuery =
                !String.IsNullOrEmpty(applicationName) ||
                !String.IsNullOrEmpty(applicationUri) ||
                !String.IsNullOrEmpty(productUri) ||
                (serverCapabilities != null && serverCapabilities.Length > 0);

            if (complexQuery)
            {
                matchQuery =
                    ApplicationsDatabaseBase.IsMatchPattern(applicationName) ||
                    ApplicationsDatabaseBase.IsMatchPattern(applicationUri) ||
                    ApplicationsDatabaseBase.IsMatchPattern(productUri);
            }

            bool lastQuery = false;
            do
            {
                uint queryRecords = complexQuery ? _defaultRecordsPerQuery : maxRecordsToReturn;
                string query = CreateServerQuery(startingRecordId, queryRecords);
                nextRecordId = startingRecordId + 1;
                var applications = await _applications.GetAsync(query);
                lastQuery = queryRecords == 0 || applications.Count() < queryRecords || applications.Count() == 0;

                foreach (var application in applications)
                {
                    startingRecordId = (uint)application.ID + 1;
                    nextRecordId = startingRecordId;

                    if (!String.IsNullOrEmpty(applicationName))
                    {
                        if (!ApplicationsDatabaseBase.Match(application.ApplicationName, applicationName))
                        {
                            continue;
                        }
                    }

                    if (!String.IsNullOrEmpty(applicationUri))
                    {
                        if (!ApplicationsDatabaseBase.Match(application.ApplicationUri, applicationUri))
                        {
                            continue;
                        }
                    }

                    if (!String.IsNullOrEmpty(productUri))
                    {
                        if (!ApplicationsDatabaseBase.Match(application.ProductUri, productUri))
                        {
                            continue;
                        }
                    }

                    string[] capabilities = null;
                    if (!String.IsNullOrEmpty(application.ServerCapabilities))
                    {
                        capabilities = application.ServerCapabilities.Split(',');
                    }

                    if (serverCapabilities != null && serverCapabilities.Length > 0)
                    {
                        bool match = true;
                        for (int ii = 0; ii < serverCapabilities.Length; ii++)
                        {
                            if (capabilities == null || !capabilities.Contains(serverCapabilities[ii]))
                            {
                                match = false;
                                break;
                            }
                        }

                        if (!match)
                        {
                            continue;
                        }
                    }

                    records.Add(application);

                    if (maxRecordsToReturn > 0 && --maxRecordsToReturn == 0)
                    {
                        break;
                    }
                }
            } while (maxRecordsToReturn > 0 && !lastQuery);

            return new QueryApplicationsResponseModel(records.ToArray(), lastCounterResetTime, nextRecordId);
        }

        public async Task<QueryApplicationsPageResponseModel> QueryApplicationsPageAsync(
            string applicationName,
            string applicationUri,
            uint applicationType,
            string productUri,
            string[] serverCapabilities,
            string nextPageLink,
            int maxRecordsToReturn)
        {
            List<Application> records = new List<Application>();
            bool matchQuery = false;
            bool complexQuery =
                !String.IsNullOrEmpty(applicationName) ||
                !String.IsNullOrEmpty(applicationUri) ||
                !String.IsNullOrEmpty(productUri) ||
                (serverCapabilities != null && serverCapabilities.Length > 0);

            if (complexQuery)
            {
                matchQuery =
                    ApplicationsDatabaseBase.IsMatchPattern(applicationName) ||
                    ApplicationsDatabaseBase.IsMatchPattern(applicationUri) ||
                    ApplicationsDatabaseBase.IsMatchPattern(productUri);
            }

            if (maxRecordsToReturn < 0)
            {
                maxRecordsToReturn = _defaultRecordsPerQuery;
            }
            string query = CreateServerQuery(0, 0);
            do
            {
                IEnumerable<Application> applications;
                (nextPageLink, applications) = await _applications.GetPageAsync(query, nextPageLink, maxRecordsToReturn - records.Count);

                foreach (var application in applications)
                {
                    if (!String.IsNullOrEmpty(applicationName))
                    {
                        if (!ApplicationsDatabaseBase.Match(application.ApplicationName, applicationName))
                        {
                            continue;
                        }
                    }

                    if (!String.IsNullOrEmpty(applicationUri))
                    {
                        if (!ApplicationsDatabaseBase.Match(application.ApplicationUri, applicationUri))
                        {
                            continue;
                        }
                    }

                    if (!String.IsNullOrEmpty(productUri))
                    {
                        if (!ApplicationsDatabaseBase.Match(application.ProductUri, productUri))
                        {
                            continue;
                        }
                    }

                    string[] capabilities = null;
                    if (!String.IsNullOrEmpty(application.ServerCapabilities))
                    {
                        capabilities = application.ServerCapabilities.Split(',');
                    }

                    if (serverCapabilities != null && serverCapabilities.Length > 0)
                    {
                        bool match = true;
                        for (int ii = 0; ii < serverCapabilities.Length; ii++)
                        {
                            if (capabilities == null || !capabilities.Contains(serverCapabilities[ii]))
                            {
                                match = false;
                                break;
                            }
                        }

                        if (!match)
                        {
                            continue;
                        }
                    }

                    records.Add(application);

                    if (maxRecordsToReturn > 0 && records.Count >= maxRecordsToReturn)
                    {
                        break;
                    }
                }
            } while (nextPageLink != null);

            return new QueryApplicationsPageResponseModel(records.ToArray(), nextPageLink);
        }
        #endregion

        #region Private Members
        private void Initialize()
        {
            _db = new DocumentDBRepository(_endpoint, _dataBaseId, _authKeyOrResourceToken);
            _applications = new DocumentDBCollection<Application>(_db, _collectionId);
        }

        private string CreateServerQuery(uint startingRecordId, uint maxRecordsToQuery)
        {
            string query;
            if (maxRecordsToQuery != 0)
            {
                query = String.Format("SELECT TOP {0}", maxRecordsToQuery);
            }
            else
            {
                query = String.Format("SELECT");
            }
            query += String.Format(" * FROM Applications a WHERE a.ID >= {0} ORDER BY a.ID", startingRecordId);
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
                throw new ArgumentNullException(nameof(application.ApplicationUri));
            }

            if (!Uri.IsWellFormedUriString(application.ApplicationUri, UriKind.Absolute))
            {
                throw new ArgumentException(application.ApplicationUri + " is not a valid URI.", nameof(application.ApplicationUri));
            }

            if (application.ApplicationType < (int)Opc.Ua.ApplicationType.Server ||
                application.ApplicationType > (int)Opc.Ua.ApplicationType.DiscoveryServer)
            {
                throw new ArgumentException(application.ApplicationType.ToString() + " is not a valid ApplicationType.", nameof(application.ApplicationType));
            }

            if (application.ApplicationNames == null || application.ApplicationNames.Length == 0 || String.IsNullOrEmpty(application.ApplicationNames[0].Text))
            {
                throw new ArgumentException("At least one ApplicationName must be provided.", nameof(application.ApplicationNames));
            }

            if (String.IsNullOrEmpty(application.ProductUri))
            {
                throw new ArgumentException("A ProductUri must be provided.", nameof(application.ProductUri));
            }

            if (!Uri.IsWellFormedUriString(application.ProductUri, UriKind.Absolute))
            {
                throw new ArgumentException(application.ProductUri + " is not a valid URI.", nameof(application.ProductUri));
            }

            if (application.DiscoveryUrls != null)
            {
                foreach (var discoveryUrl in application.DiscoveryUrls)
                {
                    if (String.IsNullOrEmpty(discoveryUrl))
                    {
                        continue;
                    }

                    if (!Uri.IsWellFormedUriString(discoveryUrl, UriKind.Absolute))
                    {
                        throw new ArgumentException(discoveryUrl + " is not a valid URL.", nameof(application.DiscoveryUrls));
                    }
                }
            }

            if (application.ApplicationType != (int)Opc.Ua.ApplicationType.Client)
            {
                if (application.DiscoveryUrls == null || application.DiscoveryUrls.Length == 0)
                {
                    throw new ArgumentException("At least one DiscoveryUrl must be provided.", nameof(application.DiscoveryUrls));
                }

                if (application.ServerCapabilities == null || application.ServerCapabilities.Length == 0)
                {
                    throw new ArgumentException("At least one Server Capability must be provided.", nameof(application.ServerCapabilities));
                }
            }
            else
            {
                if (application.DiscoveryUrls != null && application.DiscoveryUrls.Length > 0)
                {
                    throw new ArgumentException("DiscoveryUrls must not be specified for clients.", nameof(application.DiscoveryUrls));
                }
            }

            // TODO check type
            //if (application.ApplicationId == Guid.Empty)
            //{
            //    throw new ArgumentException("The ApplicationId has invalid type {0}", application.ApplicationId.ToString());
            //}

            return application.ApplicationId;
        }

        public static string ServerCapabilities(Application application)
        {
            if (application.ApplicationType != (int)Opc.Ua.ApplicationType.Client)
            {
                if (application.ServerCapabilities == null || application.ServerCapabilities.Length == 0)
                {
                    throw new ArgumentException("At least one Server Capability must be provided.", nameof(application.ServerCapabilities));
                }
            }

            StringBuilder capabilities = new StringBuilder();
            if (application.ServerCapabilities != null)
            {
                var sortedCaps = application.ServerCapabilities.Split(",").ToList();
                sortedCaps.Sort();
                foreach (var capability in sortedCaps)
                {
                    if (String.IsNullOrEmpty(capability))
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

        private async Task<int> GetMaxAppIDAsync()
        {
            // find new ID for QueryServers
            var maxAppIDEnum = await _applications.GetAsync("SELECT TOP 1 * FROM Applications a ORDER BY a.ID DESC");
            var maxAppID = maxAppIDEnum.SingleOrDefault();
            return (maxAppID != null) ? maxAppID.ID + 1 : 1;
        }
        #endregion
        #region Private Fields
        private DocumentDBRepository _db;
        private IDocumentDBCollection<Application> _applications;
        #endregion
    }
}
