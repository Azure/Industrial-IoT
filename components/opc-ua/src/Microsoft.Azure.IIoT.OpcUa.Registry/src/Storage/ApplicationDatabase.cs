// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Utils;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Microsoft.Azure.IIoT.Utils;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Application registration repository using a item container as storage
    /// </summary>
    public sealed class ApplicationDatabase : IApplicationRepository,
        IApplicationRecordQuery {

        /// <summary>
        /// Create registry services
        /// </summary>
        /// <param name="db"></param>
        /// <param name="logger"></param>
        public ApplicationDatabase(IItemContainerFactory db, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (db == null) {
                throw new ArgumentNullException(nameof(db));
            }
            var container = db.OpenAsync("applications").Result;
            _applications = container.AsDocuments();
            _index = new ContainerIndex(db, container.Name);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<ApplicationInfoModel>> ListAllAsync(
            string siteId, string supervisorId, CancellationToken ct) {

            // TODO
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<ApplicationSiteListModel> ListSitesAsync(
            string continuation, int? pageSize, CancellationToken ct) {
            var query = $"SELECT DISTINCT VALUE a.{nameof(ApplicationRegistration.SiteId)} " +
                $"FROM Applications a " +
                $"WHERE a.{nameof(ApplicationRegistration.DeviceType)} = 'Application' ";
            var client = _applications.OpenSqlClient();
            var compiled = continuation != null ?
                client.Continue<string>(continuation, pageSize) :
                client.Query<string>(query, null, pageSize);
            // Read results
            var results = await compiled.ReadAsync(ct);
            return new ApplicationSiteListModel {
                Sites = results.Select(r => r.Value).ToList(),
                ContinuationToken = compiled.ContinuationToken
            };
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoModel> GetAsync(string applicationId,
            bool throwIfNotFound, CancellationToken ct) {
            var document = await _applications.FindAsync<ApplicationRegistration>(
                applicationId, ct);
            if (document == null && throwIfNotFound) {
                throw new ResourceNotFoundException("Application does not exist");
            }
            return document?.Value?.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> ListAsync(
            string continuation, int? pageSize, bool? disabled, CancellationToken ct) {
            var query = "SELECT * FROM Applications a " +
                $"WHERE a.{nameof(ApplicationRegistration.DeviceType)} = 'Application' ";
            if (disabled != null) {
                if (disabled.Value) {
                    query +=
                        $"AND IS_DEFINED(a.{nameof(ApplicationRegistration.NotSeenSince)}) " +
                        $"AND NOT IS_NULL(a.{nameof(ApplicationRegistration.NotSeenSince)})";
                }
                else {
                    query +=
                        $"AND (NOT IS_DEFINED(a.{nameof(ApplicationRegistration.NotSeenSince)}) " +
                        $"OR IS_NULL(a.{nameof(ApplicationRegistration.NotSeenSince)}))";
                }
            }
            var client = _applications.OpenSqlClient();
            var compiled = continuation != null ?
                client.Continue<ApplicationRegistration>(continuation, pageSize) :
                client.Query<ApplicationRegistration>(query, null, pageSize);
            // Read results
            var results = await compiled.ReadAsync(ct);
            return new ApplicationInfoListModel {
                Items = results.Select(r => r.Value.ToServiceModel()).ToList(),
                ContinuationToken = compiled.ContinuationToken
            };
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoModel> UpdateAsync(string applicationId,
            Func<ApplicationInfoModel, bool?, (bool?, bool?)> updater, CancellationToken ct) {
            while (true) {
                try {
                    var document = await _applications.FindAsync<ApplicationRegistration>(applicationId, ct);
                    if (document == null) {
                        throw new ResourceNotFoundException("Application does not exist");
                    }
                    // Update registration from update request
                    var clone = document.Value.Clone();
                    var application = clone.ToServiceModel();
                    var (patch, disabled) = updater(application, clone.IsDisabled);
                    if (patch ?? false) {
                        var update = application.ToApplicationRegistration(disabled, document.Value.Etag);
                        var result = await _applications.ReplaceAsync(document, update, ct);
                        application = result.Value.ToServiceModel();
                    }
                    return application;
                }
                catch (ResourceOutOfDateException ex) {
                    // Retry create/update
                    _logger.Debug(ex, "Retry updating application...");
                    continue;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoModel> AddAsync(
            ApplicationInfoModel application, bool? disabled, CancellationToken ct) {
            if (application == null) {
                throw new ArgumentNullException(nameof(application));
            }
            var recordId = await _index.AllocateAsync(ct);
            try {
                var document = application.ToApplicationRegistration(null, null, recordId);
                var result = await _applications.AddAsync(document, ct);
                return result.Value.ToServiceModel();
            }
            catch {
                await Try.Async(() => _index.FreeAsync(recordId));
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoModel> DeleteAsync(string applicationId,
            Func<ApplicationInfoModel, bool> precondition, CancellationToken ct) {
            while (true) {
                var document = await _applications.FindAsync<ApplicationRegistration>(applicationId, ct);
                if (document == null) {
                    return null;
                }
                try {
                    var application = document.Value.ToServiceModel();
                    if (precondition != null) {
                        var shouldDelete = precondition(application);
                        if (!shouldDelete) {
                            return null;
                        }
                    }

                    // Try delete
                    await _applications.DeleteAsync(document, ct);
                    // Try free record id
                    if (document.Value.RecordId != null) {
                        await Try.Async(() => _index.FreeAsync(document.Value.RecordId.Value));
                    }
                    // return deleted entity
                    return application;
                }
                catch (ResourceOutOfDateException) {
                    _logger.Verbose("Retry delete application operation.");
                    continue;
                }
            }
        }





#if FALSE
        // TODO: Implement correctly.

        /// <inheritdoc/>
        public async Task<ApplicationRecordListModel> QueryApplicationsAsync(
            ApplicationRecordQueryModel request) {

            // TODO: implement last query time
            var lastCounterResetTime = DateTime.MinValue;

            var records = new List<ApplicationRecordModel>();
            // Get continuation token for the query from
            var nextRecordId = request.StartingRecordId ?? 1;
            string continuationToken = null;
            while (true) {
                var applications = await QueryRawAsync(request, continuationToken);
                foreach (var application in applications.Items) {

                    // pattern match
                    if (IsMatchPattern(request.ApplicationName)) {
                        if (!QueryPattern.Match(
                            application.ApplicationName, request.ApplicationName)) {
                            continue;
                        }
                    }
                    if (IsMatchPattern(request.ApplicationUri)) {
                        if (!QueryPattern.Match(
                            application.ApplicationUri, request.ApplicationUri)) {
                            continue;
                        }
                    }
                    if (IsMatchPattern(request.ProductUri)) {
                        if (!QueryPattern.Match(
                            application.ProductUri, request.ProductUri)) {
                            continue;
                        }
                    }

                    // Match capabilities
                    if (request.ServerCapabilities != null &&
                        request.ServerCapabilities.Count > 0) {
                        var match = true;
                        foreach (var cap in request.ServerCapabilities) {
                            if (application.Capabilities == null ||
                                !application.Capabilities.Contains(cap)) {
                                match = false;
                                break;
                            }
                        }
                        if (!match) {
                            continue;
                        }
                    }
                    records.Add(new ApplicationRecordModel {
                        Application = application,
                        RecordId = nextRecordId++
                    });
                }
                continuationToken = applications.ContinuationToken;
                if (records.Count != 0 || continuationToken == null) {
                    // Done
                    break;
                }
            }

            return new ApplicationRecordListModel {
                Applications = records,
                LastCounterResetTime = lastCounterResetTime,
                NextRecordId = continuationToken == null ? 0 : nextRecordId + 1
            };
        }

        /// <summary>
        /// Query raw
        /// </summary>
        /// <param name="request"></param>
        /// <param name="continuationToken"></param>
        /// <returns></returns>
        private async Task<ApplicationInfoListModel> QueryRawAsync(
            ApplicationRecordQueryModel request, string continuationToken) {
            // First get the continuation token for the query and starting record id
            if (continuationToken == null) {
                // Get the raw records
                var query = new ApplicationRegistrationQueryModel {
                    ApplicationType = request.ApplicationType,
                    ApplicationName = IsMatchPattern(request.ApplicationName) ?
                        null : request.ApplicationName,
                    ApplicationUri = IsMatchPattern(request.ApplicationUri) ?
                        null : request.ApplicationUri,
                    ProductUri = IsMatchPattern(request.ProductUri) ?
                        null : request.ProductUri
                };
                return await QueryAsync(query,
                    (int?)request.MaxRecordsToReturn ?? kDefaultRecordsPerQuery);
            }
            return await ListAsync(continuationToken,
                (int?)request.MaxRecordsToReturn ?? kDefaultRecordsPerQuery, null);
        }
#endif


        /// <inheritdoc/>
        public async Task<ApplicationRecordListModel> QueryApplicationsAsync(
            ApplicationRecordQueryModel request, CancellationToken ct) {

            // TODO: implement last query time
            var lastCounterResetTime = DateTime.MinValue;
            var records = new List<ApplicationRegistration>();
            var matchQuery = false;
            var complexQuery =
                !string.IsNullOrEmpty(request.ApplicationName) ||
                !string.IsNullOrEmpty(request.ApplicationUri) ||
                !string.IsNullOrEmpty(request.ProductUri) ||
                (request.ServerCapabilities != null && request.ServerCapabilities.Count > 0);
            if (complexQuery) {
                matchQuery =
                    IsMatchPattern(
                        request.ApplicationName) ||
                    IsMatchPattern(
                        request.ApplicationUri) ||
                    IsMatchPattern(
                        request.ProductUri);
            }

            var nextRecordId = request.StartingRecordId ?? 0;
            var maxRecordsToReturn = request.MaxRecordsToReturn ?? 0;
            var lastQuery = false;
            do {
                var queryRecords = complexQuery ? kDefaultRecordsPerQuery : maxRecordsToReturn;
                var query = CreateServerQuery(nextRecordId, (int)queryRecords);
                nextRecordId++;
                var applications = await query.ReadAsync(ct);
                lastQuery = queryRecords == 0 || applications.Count() < queryRecords;
                foreach (var application in applications.Select(a => a.Value)) {
                    if (application.RecordId == null) {
                        continue; // Unexpected
                    }
                    nextRecordId = application.RecordId.Value + 1;
                    if (!string.IsNullOrEmpty(request.ApplicationName)) {
                        if (!QueryPattern.Match(
                            application.ApplicationName, request.ApplicationName)) {
                            continue;
                        }
                    }
                    if (!string.IsNullOrEmpty(request.ApplicationUri)) {
                        if (!QueryPattern.Match(
                            application.ApplicationUri, request.ApplicationUri)) {
                            continue;
                        }
                    }
                    if (!string.IsNullOrEmpty(request.ProductUri)) {
                        if (!QueryPattern.Match(
                            application.ProductUri, request.ProductUri)) {
                            continue;
                        }
                    }

                    if (request.ServerCapabilities != null &&
                        request.ServerCapabilities.Count > 0) {
                        var match = true;
                        foreach (var cap in request.ServerCapabilities) {
                            if (!application.Capabilities.ContainsKey(cap)) {
                                match = false;
                                break;
                            }
                        }
                        if (!match) {
                            continue;
                        }
                    }
                    records.Add(application);
                    if (maxRecordsToReturn > 0 && --maxRecordsToReturn == 0) {
                        break;
                    }
                }
            } while (maxRecordsToReturn > 0 && !lastQuery);
            if (lastQuery) {
                nextRecordId = 0;
            }
            return new ApplicationRecordListModel {
                Applications = records.Select(a => new ApplicationRecordModel {
                    Application = a.ToServiceModel(),
                    RecordId = a.RecordId.Value
                }).ToList(),
                LastCounterResetTime = lastCounterResetTime,
                NextRecordId = nextRecordId
            };
        }

        /// <inheritdoc/>
        public async Task<ApplicationInfoListModel> QueryAsync(
            ApplicationRegistrationQueryModel request, int? maxRecordsToReturn, CancellationToken ct) {
            var records = new List<ApplicationRegistration>();
            var matchQuery = false;
            var complexQuery =
                !string.IsNullOrEmpty(request.ApplicationName) ||
                !string.IsNullOrEmpty(request.ApplicationUri) ||
                !string.IsNullOrEmpty(request.ProductUri) ||
                !string.IsNullOrEmpty(request.Capability);

            if (complexQuery) {
                matchQuery =
                    IsMatchPattern(
                        request.ApplicationName) ||
                    IsMatchPattern(
                        request.ApplicationUri) ||
                    IsMatchPattern(
                        request.ProductUri);
            }

            if (maxRecordsToReturn == null || maxRecordsToReturn < 0) {
                maxRecordsToReturn = kDefaultRecordsPerQuery;
            }
            var query = CreateServerQuery(0, maxRecordsToReturn.Value);
            while (query.HasMore()) {
                var applications = await query.ReadAsync(ct);
                foreach (var application in applications.Select(a => a.Value)) {
                    if (!string.IsNullOrEmpty(request.ApplicationName)) {
                        if (!QueryPattern.Match(
                            application.ApplicationName, request.ApplicationName)) {
                            continue;
                        }
                    }
                    if (!string.IsNullOrEmpty(request.ApplicationUri)) {
                        if (!QueryPattern.Match(
                            application.ApplicationUri, request.ApplicationUri)) {
                            continue;
                        }
                    }
                    if (!string.IsNullOrEmpty(request.ProductUri)) {
                        if (!QueryPattern.Match(
                            application.ProductUri, request.ProductUri)) {
                            continue;
                        }
                    }
                    if (!string.IsNullOrEmpty(request.Capability)) {
                        if (!application.Capabilities?.ContainsKey(request.Capability) ?? false) {
                            continue;
                        }
                    }
                    records.Add(application);
                    if (maxRecordsToReturn > 0 && records.Count >= maxRecordsToReturn) {
                        break;
                    }
                }
            }
            return new ApplicationInfoListModel {
                Items = records.Select(a => a.ToServiceModel()).ToList(),
                ContinuationToken = null
            };
        }

        /// <summary>
        /// Helper to create a SQL query for CosmosDB.
        /// </summary>
        /// <param name="startingRecordId">The first record Id</param>
        /// <param name="maxRecordsToQuery">The max number of records</param>
        /// <returns></returns>
        private IResultFeed<IDocumentInfo<ApplicationRegistration>> CreateServerQuery(
            uint startingRecordId, int maxRecordsToQuery) {
            string query;
            var queryParameters = new Dictionary<string, object>();
            if (maxRecordsToQuery != 0) {
                query = "SELECT TOP @maxRecordsToQuery * FROM Applications a ";
                queryParameters.Add("@maxRecordsToQuery", maxRecordsToQuery);
            }
            else {
                query = "SELECT * FROM Applications a ";
            }
            query += $"WHERE a.{nameof(ApplicationRegistration.RecordId)} >= @startingRecord";
            queryParameters.Add("@startingRecord", startingRecordId);
            query += $" AND a.{ nameof(ApplicationRegistration.DeviceType)} = @classType";
            queryParameters.Add("@classType", "Application");
            query += $" ORDER BY a.{nameof(ApplicationRegistration.RecordId)}";

            var client = _applications.OpenSqlClient();
            return client.Query<ApplicationRegistration>(query, queryParameters, maxRecordsToQuery);
        }

        /// <summary>
        /// Test whether the string is a pattern
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private static bool IsMatchPattern(string pattern) {
            if (!string.IsNullOrEmpty(pattern)) {
                return false;
            }
            return QueryPattern.IsMatchPattern(
                pattern);
        }

        private const int kDefaultRecordsPerQuery = 10;
        private readonly ILogger _logger;
        private readonly IDocuments _applications;
        private readonly IContainerIndex _index;
    }
}
