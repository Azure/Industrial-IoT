// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Azure.IIoT.Diagnostics;
using Microsoft.Azure.IIoT.Exceptions;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.CosmosDB;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.CosmosDB.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.Runtime;
using Opc.Ua.Gds.Server.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault
{
    internal sealed class CosmosDBApplicationsDatabase : IApplicationsDatabase
    {
        private readonly ILogger _log;
        private readonly string Endpoint;
        private SecureString AuthKeyOrResourceToken;

        public CosmosDBApplicationsDatabase(
            IServicesConfig config,
            ILogger logger)
        {
            this.Endpoint = config.CosmosDBEndpoint;
            this.AuthKeyOrResourceToken = new SecureString();
            foreach (char ch in config.CosmosDBToken)
            {
                this.AuthKeyOrResourceToken.AppendChar(ch);
            }
            _log = logger;
            _log.Debug("Creating new instance of `CosmosDBApplicationsDatabase` service " + config.CosmosDBEndpoint, () => { });
            Initialize();
        }

        #region IApplicationsDatabase 
        public async Task<string> RegisterApplicationAsync(Application application)
        {
            bool isNew = false;
            Guid applicationId = VerifyRegisterApplication(application);
            if (applicationId == null || Guid.Empty == applicationId)
            {
                isNew = true;
            }

            string capabilities = ServerCapabilities(application);

            if (applicationId != Guid.Empty)
            {
                Application record = await Applications.GetAsync(applicationId);
                if (record == null)
                {
                    application.ApplicationId = Guid.NewGuid();
                    isNew = true;
                }
            }

            if (isNew)
            {
                // find new ID for QueryServers
                var maxAppIDEnum = await Applications.GetAsync("SELECT TOP 1 * FROM Applications a ORDER BY a.ID DESC");
                var maxAppID = maxAppIDEnum.SingleOrDefault();
                application.ID = (maxAppID != null) ? maxAppID.ID + 1 : 1;
                application.ApplicationId = Guid.NewGuid();
                var result = await Applications.CreateAsync(application);
                applicationId = new Guid(result.Id);
            }
            else
            {
                await Applications.UpdateAsync(applicationId, application);
            }

            return applicationId.ToString();
        }

        public async Task<string> UpdateApplicationAsync(string id, Application application)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentException("The id must be provided", "id");
            }

            if (application == null)
            {
                throw new ArgumentException("The application must be provided", "application");
            }

            Guid recordId = VerifyRegisterApplication(application);
            Guid applicationId = new Guid(id);

            if (recordId == null || recordId == Guid.Empty)
            {
                application.ApplicationId = applicationId;
            }

            string capabilities = ServerCapabilities(application);

            if (applicationId != Guid.Empty)
            {
                var record = await Applications.GetAsync(applicationId);
                if (record == null)
                {
                    throw new ArgumentException("A record with the specified application id does not exist.", nameof(id));
                }

                record.ApplicationUri = application.ApplicationUri;
                record.ApplicationName = application.ApplicationName;
                record.ApplicationType = application.ApplicationType;
                record.ProductUri = application.ProductUri;
                record.ServerCapabilities = capabilities;
                record.ApplicationNames = application.ApplicationNames;
                record.DiscoveryUrls = application.DiscoveryUrls;

                await Applications.UpdateAsync(applicationId, record);
            }
            return applicationId.ToString();
        }

        public async Task UnregisterApplicationAsync(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentException("The id must be provided", "id");
            }

            Guid appId = new Guid(id);

            List<byte[]> certificates = new List<byte[]>();

            var application = await Applications.GetAsync(appId);
            if (application == null)
            {
                throw new ResourceNotFoundException("A record with the specified application id does not exist.");
            }

            var certificateRequests = await CertificateRequests.GetAsync(ii => ii.ApplicationId == appId.ToString());
            foreach (var entry in new List<CertificateRequest>(certificateRequests))
            {
                await CertificateRequests.DeleteAsync(entry.RequestId);
            }

            await Applications.DeleteAsync(appId);
        }

        public async Task<Application> GetApplicationAsync(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentException("The id must be provided", "id");
            }

            Guid appId = new Guid(id);
            return await Applications.GetAsync(appId);
        }

        public async Task<Application[]> FindApplicationAsync(string applicationUri)
        {
            if (String.IsNullOrEmpty(applicationUri))
            {
                throw new ArgumentException("The applicationUri must be provided", "applicationUri");
            }

            var results = await Applications.GetAsync(ii => ii.ApplicationUri == applicationUri);
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
            const uint defaultRecordsPerQuery = 10;

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
                uint queryRecords = complexQuery ? defaultRecordsPerQuery : maxRecordsToReturn;
                string query = CreateServerQuery(startingRecordId, queryRecords);
                nextRecordId = startingRecordId + 1;
                var applications = await Applications.GetAsync(query);
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
        #endregion
        #region Private Members
        private void Initialize()
        {
            db = new DocumentDBRepository(Endpoint, AuthKeyOrResourceToken);
            Applications = new DocumentDBCollection<Application>(db);
            CertificateRequests = new DocumentDBCollection<CertificateRequest>(db);
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

            if (application.ApplicationNames == null || application.ApplicationNames.Length == 0 || String.IsNullOrEmpty(application.ApplicationNames[0].Text))
            {
                throw new ArgumentException("At least one ApplicationName must be provided.", "ApplicationNames");
            }

            if (String.IsNullOrEmpty(application.ProductUri))
            {
                throw new ArgumentException("A ProductUri must be provided.", "ProductUri");
            }

            if (!Uri.IsWellFormedUriString(application.ProductUri, UriKind.Absolute))
            {
                throw new ArgumentException(application.ProductUri + " is not a valid URI.", "ProductUri");
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

        public static string ServerCapabilities(Application application)
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

        #endregion
        #region Private Fields
        private DateTime queryCounterResetTime = DateTime.UtcNow;
        private DocumentDBRepository db;
        private IDocumentDBCollection<Application> Applications;
        private IDocumentDBCollection<CertificateRequest> CertificateRequests;
        #endregion
    }
}
