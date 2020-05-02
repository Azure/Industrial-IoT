// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Migration {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Services;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Migrate from device twin repo to defined repo
    /// </summary>
    public sealed class ApplicationTwinsMigration : IMigrationTask {

        /// <summary>
        /// Create migrator
        /// </summary>
        /// <param name="source"></param>
        /// <param name="repo"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public ApplicationTwinsMigration(IIoTHubTwinServices source, IApplicationRepository repo,
            IJsonSerializer serializer, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _source = new ApplicationTwins(source, serializer, logger);
        }

        /// <inheritdoc/>
        public async Task MigrateAsync() {
            string continuation = null;
            do {
                var results = await _source.ListAsync(continuation, null);
                continuation = results.ContinuationToken;
                foreach (var application in results.Items) {
                    try {
                        var clone = application.Clone();
                        clone.ApplicationId =
                            ApplicationInfoModelEx.CreateApplicationId(application);
                        await _repo.AddAsync(clone);
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
                    await _source.DeleteAsync(application.ApplicationId);
                }
            }
            while (continuation != null);
        }

        private readonly ILogger _logger;
        private readonly IApplicationRepository _source;
        private readonly IApplicationRepository _repo;
    }
}
