// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Migration {
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Migrate from Vault v1 application database documents to new repo
    /// </summary>
    public sealed class VaultApplicationMigration : IMigrationTask {

        /// <summary>
        /// Create migrator
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="db"></param>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public VaultApplicationMigration(IApplicationRepository repo,
            IDatabaseServer db, IConfigurationRoot configuration, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));

            // Bind legacy configuration
            var config = new ServicesConfig();
            configuration?.Bind("OpcVault", config);

            var database = db.OpenAsync(config.CosmosDBDatabase).Result;
            try {
                _source = database.OpenContainerAsync(config.CosmosDBCollection)
                    .Result
                    .AsDocuments();
            }
            catch (Exception ex) {
                logger.Error(ex, "Failed to open container - not migrating");
            }
        }

        /// <inheritdoc/>
        public async Task MigrateAsync() {
            if (_source == null) {
                return;
            }
            var query = _source.OpenSqlClient().Query<Application>(
                    "SELECT * FROM Applications a WHERE " +
                    $"a.{nameof(Application.ClassType)} = '{Application.ClassTypeName}'",
                null, null);
            // Read results
            while (query.HasMore()) {
                var results = await query.ReadAsync();
                foreach (var document in results) {
                    var application = ToServiceModel(document.Value);
                    try {
                        application.ApplicationId =
                            ApplicationInfoModelEx.CreateApplicationId(application);
                        await _repo.AddAsync(application);
                    }
                    catch (ConflictingResourceException ex) {
                        _logger.Error(ex,
                            "Application {application} already exists - not migrating...",
                            application.ApplicationName);
                        continue;
                    }
                    catch (Exception e) {
                        _logger.Error(e, "Error adding {application} - skip migration...",
                            application.ApplicationName);
                        continue;
                    }
                    // Force delete now
                    await _source.DeleteAsync(document.Id);
                }
            }
        }


        /// <summary>
        /// Returns server capabilities as comma separated string.
        /// </summary>
        internal static string GetApplicationName(Application application, out string locale) {
            locale = null;
            if (!string.IsNullOrEmpty(application.ApplicationName)) {
                return application.ApplicationName;
            }
            if (application.ApplicationNames != null &&
                application.ApplicationNames.Length != 0 &&
                !string.IsNullOrEmpty(application.ApplicationNames[0].Text)) {
                locale = application.ApplicationNames[0].Locale;
                return application.ApplicationNames[0].Text;
            }
            return null;
        }

        /// <summary>
        /// Convert to document model
        /// </summary>
        /// <returns></returns>
        internal static ApplicationInfoModel ToServiceModel(Application application) {
            var app = new ApplicationInfoModel {
                ApplicationUri = application.ApplicationUri,
                ApplicationName = GetApplicationName(application, out var locale),
                Locale = locale,
                ApplicationType = application.ApplicationType,
                LocalizedNames = ToServiceModel(application.ApplicationNames),
                ProductUri = application.ProductUri,
                DiscoveryUrls = application.DiscoveryUrls.ToHashSetSafe(),
                Capabilities = ToServiceModel(application.ServerCapabilities),
                GatewayServerUri = application.GatewayServerUri,
                DiscoveryProfileUri = application.DiscoveryProfileUri,
                Created = ToServiceModel(
                    application.CreateTime, null),
                Updated = ToServiceModel(
                    application.UpdateTime, null),
                ApplicationId = null,
                Certificate = null,
                HostAddresses = null,
                NotSeenSince = null,
                SiteId = null,
                SupervisorId = null
            };
            app.ApplicationId = ApplicationInfoModelEx.CreateApplicationId(app);
            return app;
        }

        /// <summary>from Vault v1</summary>
        [Serializable]
        internal class ApplicationName {
            internal string Locale { get; set; }
            internal string Text { get; set; }
        }

        /// <summary>from Vault v1</summary>
        [Serializable]
        internal class Application {
            internal static readonly string ClassTypeName = "Application";
            internal Application() {
                ClassType = ClassTypeName;
            }
            [JsonProperty(PropertyName = "id")]
            internal Guid ApplicationId { get; set; }
            [JsonProperty(PropertyName = "_etag")]
            internal string ETag { get; set; }
            internal string ClassType { get; set; }
            internal int ID { get; set; }
            internal string ApplicationUri { get; set; }
            internal string ApplicationName { get; set; }
            internal ApplicationType ApplicationType { get; set; }
            internal string ProductUri { get; set; }
            internal string ServerCapabilities { get; set; }
            internal ApplicationName[] ApplicationNames { get; set; }
            internal string[] DiscoveryUrls { get; set; }
            internal string GatewayServerUri { get; set; }
            internal string DiscoveryProfileUri { get; set; }
            internal string AuthorityId { get; set; }
            internal DateTime? CreateTime { get; set; }
            internal DateTime? UpdateTime { get; set; }
            internal DateTime? DeleteTime { get; set; }
        }

        /// <summary>
        /// Old vault configuration
        /// </summary>
        internal class ServicesConfig {

            public ServicesConfig() {
                CosmosDBDatabase = "OpcVault";
                CosmosDBCollection = "AppsAndCertRequests";
            }

            /// <inheritdoc/>
            public string CosmosDBDatabase { get; set; }
            /// <inheritdoc/>
            public string CosmosDBCollection { get; set; }
        }

        /// <summary>
        /// Returns server capabilities hash set
        /// </summary>
        /// <param name="caps">Capabilities.</param>
        /// <returns></returns>
        internal static HashSet<string> ToServiceModel(string caps) {
            return caps
                .Split(',')
                .Select(c => c.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToHashSetSafe();
        }

        /// <summary>
        /// Registry registry operation model from fields
        /// </summary>
        /// <param name="time"></param>
        /// <param name="authorityId"></param>
        /// <returns></returns>
        internal static RegistryOperationContextModel ToServiceModel(DateTime? time,
            string authorityId) {
            if (time == null) {
                return null;
            }
            return new RegistryOperationContextModel {
                AuthorityId = string.IsNullOrEmpty(authorityId) ?
                    "Unknown" : authorityId,
                Time = time.Value
            };
        }

        /// <summary>
        /// Convert table to localized text
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private static Dictionary<string, string> ToServiceModel(ApplicationName[] table) {
            return table?.ToDictionary(n => n.Locale, n => n.Text);
        }

        private readonly ILogger _logger;
        private readonly IDocuments _source;
        private readonly IApplicationRepository _repo;
    }
}
