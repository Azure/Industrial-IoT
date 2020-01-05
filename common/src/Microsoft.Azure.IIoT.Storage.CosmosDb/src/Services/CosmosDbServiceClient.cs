// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.CosmosDb.Services {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides document db and graph functionality for storage interfaces.
    /// </summary>
    public sealed class CosmosDbServiceClient : IDatabaseServer {

        /// <summary>
        /// Creates server
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <param name="jsonConfig"></param>
        public CosmosDbServiceClient(ICosmosDbConfig config,
            ILogger logger, IJsonSerializerConfig jsonConfig = null) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonConfig = jsonConfig;
            if (string.IsNullOrEmpty(_config?.DbConnectionString)) {
                throw new ArgumentNullException(nameof(_config.DbConnectionString));
            }
        }

        /// <inheritdoc/>
        public async Task<IDatabase> OpenAsync(string databaseId, DatabaseOptions options) {
            if (string.IsNullOrEmpty(databaseId)) {
                databaseId = "default";
            }
            var cs = ConnectionString.Parse(_config.DbConnectionString);
            var settings = _jsonConfig?.Serializer ?? JsonConvert.DefaultSettings?.Invoke();

            var client = new DocumentClient(new Uri(cs.Endpoint), cs.SharedAccessKey,
                settings, null, options?.Consistency.ToConsistencyLevel());
            await client.CreateDatabaseIfNotExistsAsync(new Database {
                Id = databaseId
            });
            return new DocumentDatabase(client, databaseId, settings,
                _config.ThroughputUnits, _logger);
        }

        private readonly ICosmosDbConfig _config;
        private readonly ILogger _logger;
        private readonly IJsonSerializerConfig _jsonConfig;
    }
}
