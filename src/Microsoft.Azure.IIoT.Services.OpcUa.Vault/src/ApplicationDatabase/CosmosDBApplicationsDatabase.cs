// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Autofac;
using Microsoft.Azure.Documents;
using Microsoft.Azure.IIoT.Exceptions;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Types;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ApplicationsDatabaseBase = Opc.Ua.Gds.Server.Database.ApplicationsDatabaseBase;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault
{
    /// <summary>
    /// Helper to create app darabase for unit tests.
    /// </summary>
    public static class CosmosDBApplicationsDatabaseFactory
    {
        public static IApplicationsDatabase Create(
            ILifetimeScope scope,
            IServicesConfig config,
            IDocumentDBRepository db,
            ILogger logger)
        {
            return new CosmosDBApplicationsDatabase(scope, config, db, logger);
        }
    }

    /// <summary>
    /// The CosmosDB implementation of the application database.
    /// </summary>
    internal sealed class CosmosDBApplicationsDatabase : IApplicationsDatabase
    {
        const int _defaultRecordsPerQuery = 10;
        private readonly ILogger _log;
        private readonly bool _autoApprove;
        private readonly ILifetimeScope _scope = null;
        private int _appIdCounter = 1;

        public CosmosDBApplicationsDatabase(
            ILifetimeScope scope,
            IServicesConfig config,
            IDocumentDBRepository db,
            ILogger logger)
        {
            _scope = scope;
            _autoApprove = config.ApplicationsAutoApprove;
            _log = logger;
            _log.Debug("Creating new instance of `CosmosDBApplicationsDatabase` service " + config.CosmosDBCollection);
            // set unique key in CosmosDB for application ID 
            db.UniqueKeyPolicy.UniqueKeys.Add(new UniqueKey { Paths = new Collection<string> { "/" + nameof(Application.ClassType), "/" + nameof(Application.ID) } });
            _applications = new DocumentDBCollection<Application>(db, config.CosmosDBCollection);
        }

        #region IApplicationsDatabase
        public async Task Initialize()
        {
            await _applications.CreateCollectionIfNotExistsAsync();
            _appIdCounter = await GetMaxAppIDAsync();
        }

        /// <inheritdoc/>
        public async Task<Application> RegisterApplicationAsync(Application application)
        {
            Guid applicationId = VerifyRegisterApplication(application);
            if (Guid.Empty != applicationId)
            {
                return await UpdateApplicationAsync(application.ApplicationId.ToString(), application);
            }

            // normalize Server Caps
            application.ServerCapabilities = ServerCapabilities(application);
            application.ApplicationId = Guid.NewGuid();
            application.ID = _appIdCounter++;
            application.ApplicationState = ApplicationState.New;
            application.CreateTime = DateTime.UtcNow;

            // depending on use case, new applications can be auto approved.
            if (_autoApprove)
            {
                application.ApplicationState = ApplicationState.Approved;
                application.ApproveTime = application.CreateTime;
            }
            bool retry;
            string resourceId = null;
            do
            {
                retry = false;
                try
                {
                    var result = await _applications.CreateAsync(application);
                    resourceId = result.Id;
                }
                catch (DocumentClientException dce)
                {
                    if (dce.StatusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        // retry with new guid and keys
                        application.ApplicationId = Guid.NewGuid();
                        _appIdCounter = await GetMaxAppIDAsync();
                        application.ID = _appIdCounter++;
                        retry = true;
                    }
                }
            } while (retry);
            applicationId = ToGuidAndVerify(resourceId);
            return await _applications.GetAsync(applicationId);
        }

        /// <inheritdoc/>
        public Task<Application> GetApplicationAsync(string applicationId)
        {
            Guid appId = ToGuidAndVerify(applicationId);
            return _applications.GetAsync(appId);
        }

        /// <inheritdoc/>
        public async Task<Application> UpdateApplicationAsync(string applicationId, Application application)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application), "The application must be provided");
            }

            Guid appGuid = ToGuidAndVerify(applicationId);
            Guid recordId = VerifyRegisterApplication(application);

            string capabilities = ServerCapabilities(application);

            bool retryUpdate;
            do
            {
                retryUpdate = false;

                var record = await _applications.GetAsync(appGuid);
                if (record == null)
                {
                    throw new ResourceNotFoundException("A record with the specified application id does not exist.");
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
                    await _applications.UpdateAsync(appGuid, record, record.ETag);
                }
                catch (DocumentClientException dce)
                {
                    if (dce.StatusCode == HttpStatusCode.PreconditionFailed)
                    {
                        retryUpdate = true;
                    }
                }
            } while (retryUpdate);
            return await _applications.GetAsync(appGuid);
        }

        /// <inheritdoc/>
        public async Task<Application> ApproveApplicationAsync(string applicationId, bool approved, bool force)
        {
            Guid appId = ToGuidAndVerify(applicationId);
            bool retryUpdate;
            Application record;
            do
            {
                retryUpdate = false;

                record = await _applications.GetAsync(appId);
                if (record == null)
                {
                    throw new ResourceNotFoundException("A record with the specified application id does not exist.");
                }

                if (!force &&
                    record.ApplicationState != ApplicationState.New)
                {
                    throw new ResourceInvalidStateException("The record is not in a valid state for this operation.");
                }

                record.ApplicationState = approved ? ApplicationState.Approved : ApplicationState.Rejected;
                record.ApproveTime = DateTime.UtcNow;

                try
                {
                    await _applications.UpdateAsync(appId, record, record.ETag);
                }
                catch (DocumentClientException dce)
                {
                    if (dce.StatusCode == HttpStatusCode.PreconditionFailed)
                    {
                        retryUpdate = true;
                    }
                }
            } while (retryUpdate);

            return record;
        }

        /// <inheritdoc/>
        public async Task<Application> UnregisterApplicationAsync(string applicationId)
        {
            Guid appId = ToGuidAndVerify(applicationId);
            bool retryUpdate;
            bool first = true;
            Application record;
            do
            {
                retryUpdate = false;

                List<byte[]> certificates = new List<byte[]>();

                record = await _applications.GetAsync(appId);
                if (record == null)
                {
                    throw new ResourceNotFoundException("A record with the specified application id does not exist.");
                }

                if (record.ApplicationState >= ApplicationState.Unregistered)
                {
                    throw new ResourceInvalidStateException("The record is not in a valid state for this operation.");
                }

                if (first && _scope != null)
                {
                    ICertificateRequest certificateRequestsService = _scope.Resolve<ICertificateRequest>();
                    // mark all requests as deleted
                    ReadRequestResultModel[] certificateRequests;
                    string nextPageLink = null;
                    do
                    {
                        (nextPageLink, certificateRequests) = await certificateRequestsService.QueryPageAsync(appId.ToString(), null, nextPageLink);
                        foreach (var request in certificateRequests)
                        {
                            if (request.State < CertificateRequestState.Deleted)
                            {
                                await certificateRequestsService.DeleteAsync(request.RequestId);
                            }
                        }
                    } while (nextPageLink != null);
                }
                first = false;

                record.ApplicationState = ApplicationState.Unregistered;
                record.DeleteTime = DateTime.UtcNow;

                try
                {
                    await _applications.UpdateAsync(appId, record, record.ETag);
                }
                catch (DocumentClientException dce)
                {
                    if (dce.StatusCode == HttpStatusCode.PreconditionFailed)
                    {
                        retryUpdate = true;
                    }
                }
            } while (retryUpdate);

            return record;
        }

        /// <inheritdoc/>
        public async Task DeleteApplicationAsync(string applicationId, bool force)
        {
            Guid appId = ToGuidAndVerify(applicationId);
            var application = await _applications.GetAsync(appId);
            if (!force &&
                application.ApplicationState < ApplicationState.Unregistered)
            {
                throw new ResourceInvalidStateException("The record is not in a valid state for this operation.");
            }

            if (_scope != null)
            {
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
            }
            await _applications.DeleteAsync(appId);
        }

        /// <inheritdoc/>
        public async Task<IList<Application>> ListApplicationAsync(string applicationUri)
        {
            if (String.IsNullOrEmpty(applicationUri))
            {
                throw new ArgumentNullException(nameof(applicationUri), "The applicationUri must be provided.");
            }
            if (!Uri.IsWellFormedUriString(applicationUri, UriKind.Absolute))
            {
                throw new ArgumentException(nameof(applicationUri), "The applicationUri is invalid.");
            }

            var queryParameters = new SqlParameterCollection();
            string query = "SELECT * FROM Applications a WHERE";
            query += " a.ApplicationUri = @applicationUri";
            queryParameters.Add(new SqlParameter("@applicationUri", applicationUri));
            query += " AND a.ApplicationState = @applicationState";
            queryParameters.Add(new SqlParameter("@applicationState", ApplicationState.Approved.ToString()));
            query += " AND a.ClassType = @classType";
            queryParameters.Add(new SqlParameter("@classType", Application.ClassTypeName));
            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = query,
                Parameters = queryParameters
            };
            var sqlResults = await _applications.GetAsync(sqlQuerySpec);
            return sqlResults.ToList();
        }

        /// <inheritdoc/>
        public async Task<QueryApplicationsByIdResponseModel> QueryApplicationsByIdAsync(
            uint startingRecordId,
            uint maxRecordsToReturn,
            string applicationName,
            string applicationUri,
            uint applicationType,
            string productUri,
            IList<string> serverCapabilities,
            QueryApplicationState? applicationState
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
                (serverCapabilities != null && serverCapabilities.Count > 0);

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
                SqlQuerySpec sqlQuerySpec = CreateServerQuery(startingRecordId, queryRecords, applicationState);
                nextRecordId = startingRecordId + 1;
                var applications = await _applications.GetAsync(sqlQuerySpec);
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

                    if (serverCapabilities != null && serverCapabilities.Count > 0)
                    {
                        bool match = true;
                        for (int ii = 0; ii < serverCapabilities.Count; ii++)
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

            if (lastQuery)
            {
                nextRecordId = 0;
            }

            return new QueryApplicationsByIdResponseModel(records.ToArray(), lastCounterResetTime, nextRecordId);
        }

        /// <inheritdoc/>
        public async Task<QueryApplicationsResponseModel> QueryApplicationsAsync(
            string applicationName,
            string applicationUri,
            uint applicationType,
            string productUri,
            IList<string> serverCapabilities,
            QueryApplicationState? applicationState,
            string nextPageLink,
            int? maxRecordsToReturn)
        {
            List<Application> records = new List<Application>();
            bool matchQuery = false;
            bool complexQuery =
                !String.IsNullOrEmpty(applicationName) ||
                !String.IsNullOrEmpty(applicationUri) ||
                !String.IsNullOrEmpty(productUri) ||
                (serverCapabilities != null && serverCapabilities.Count > 0);

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
            SqlQuerySpec sqlQuerySpec = CreateServerQuery(0, 0, applicationState);
            do
            {
                IEnumerable<Application> applications;
                (nextPageLink, applications) = await _applications.GetPageAsync(sqlQuerySpec, nextPageLink, maxRecordsToReturn - records.Count);

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

                    if (serverCapabilities != null && serverCapabilities.Count > 0)
                    {
                        bool match = true;
                        for (int ii = 0; ii < serverCapabilities.Count; ii++)
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

            return new QueryApplicationsResponseModel(records.ToArray(), nextPageLink);
        }
        #endregion

        #region Private Members
        /// <summary>
        /// Helper to create a SQL query for CosmosDB.
        /// </summary>
        /// <param name="startingRecordId">The first record Id</param>
        /// <param name="maxRecordsToQuery">The max number of records</param>
        /// <param name="applicationState">The application state query filter</param>
        /// <returns></returns>
        private SqlQuerySpec CreateServerQuery(uint startingRecordId, uint maxRecordsToQuery, QueryApplicationState? applicationState)
        {
            string query;
            var queryParameters = new SqlParameterCollection();
            if (maxRecordsToQuery != 0)
            {
                query = "SELECT TOP @maxRecordsToQuery";
                queryParameters.Add(new SqlParameter("@maxRecordsToQuery", maxRecordsToQuery));
            }
            else
            {
                query = "SELECT";
            }
            query += " * FROM Applications a WHERE a.ID >= @startingRecord";
            queryParameters.Add(new SqlParameter("@startingRecord", startingRecordId));
            QueryApplicationState queryState = applicationState ?? QueryApplicationState.Approved;
            if (queryState != 0)
            {
                bool first = true;
                foreach (QueryApplicationState state in Enum.GetValues(typeof(QueryApplicationState)))
                {
                    if (state == 0)
                    {
                        continue;
                    }

                    if ((queryState & state) == state)
                    {
                        var sqlParm = "@" + state.ToString().ToLower();
                        if (first)
                        {
                            query += " AND (";
                        }
                        else
                        {
                            query += " OR";
                        }
                        query += " a.ApplicationState = " + sqlParm;
                        queryParameters.Add(new SqlParameter(sqlParm, state.ToString()));
                        first = false;
                    }
                }
                if (!first)
                {
                    query += " )";
                }
            }
            query += " AND a.ClassType = @classType";
            queryParameters.Add(new SqlParameter("@classType", Application.ClassTypeName));
            query += " ORDER BY a.ID";
            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = query,
                Parameters = queryParameters
            };
            return sqlQuerySpec;
        }

        /// <summary>
        /// Validates all fields in an application record to be consistent with the OPC UA specification.
        /// </summary>
        /// <param name="application">The application</param>
        /// <returns>The application Guid.</returns>
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

            if ((application.ApplicationType < ApplicationType.Server) ||
                (application.ApplicationType > ApplicationType.DiscoveryServer))
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

                    // TODO: check for https:/hostname:62541, typo is not detected here
                }
            }

            if ((int)application.ApplicationType != (int)Opc.Ua.ApplicationType.Client)
            {
                if (application.DiscoveryUrls == null || application.DiscoveryUrls.Length == 0)
                {
                    throw new ArgumentException("At least one DiscoveryUrl must be provided.", nameof(application.DiscoveryUrls));
                }

                if (application.ServerCapabilities == null || application.ServerCapabilities.Length == 0)
                {
                    throw new ArgumentException("At least one Server Capability must be provided.", nameof(application.ServerCapabilities));
                }

                // TODO: check for valid servercapabilities
            }
            else
            {
                if (application.DiscoveryUrls != null && application.DiscoveryUrls.Length > 0)
                {
                    throw new ArgumentException("DiscoveryUrls must not be specified for clients.", nameof(application.DiscoveryUrls));
                }
            }

            return application.ApplicationId;
        }

        /// <summary>
        /// Returns server capabilities as comma separated string.
        /// </summary>
        /// <param name="application">The application record.</param>
        public static string ServerCapabilities(Application application)
        {
            if ((int)application.ApplicationType != (int)ApplicationType.Client)
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

        /// <summary>
        /// Convert the application Id string to Guid.
        /// Throws on invalid guid.
        /// </summary>
        /// <param name="applicationId"></param>
        private Guid ToGuidAndVerify(string applicationId)
        {
            try
            {
                if (String.IsNullOrEmpty(applicationId))
                {
                    throw new ArgumentNullException(nameof(applicationId), "The application id must be provided");
                }
                Guid guid = new Guid(applicationId);
                if (guid == Guid.Empty)
                {
                    throw new ArgumentException("The applicationId is invalid");
                }
                return guid;
            }
            catch (FormatException)
            {
                throw new ArgumentException("The applicationId is invalid.");
            }
        }

        /// <summary>
        /// Returns the next free, largest, application ID value.
        /// This is the ID value used for sorting in GDS queries.
        /// </summary>
        private async Task<int> GetMaxAppIDAsync()
        {
            try
            {
                // find new ID for QueryServers
                SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
                {
                    QueryText = "SELECT TOP 1 * FROM Applications a WHERE a.ClassType = @classType ORDER BY a.ID DESC",
                    Parameters = new SqlParameterCollection { new SqlParameter("@classType", Application.ClassTypeName) }
                };
                var maxIDEnum = await _applications.GetAsync(sqlQuerySpec);
                var maxID = maxIDEnum.SingleOrDefault();
                return (maxID != null) ? maxID.ID + 1 : 1;
            }
            catch
            {
                return 1;
            }
        }
        #endregion
        #region Private Fields
        private IDocumentDBCollection<Application> _applications;
        #endregion
    }
}
