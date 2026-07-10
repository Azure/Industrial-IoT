// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.EventHubs
{
    using Azure.IIoT.OpcUa.Core.AzureSdk;
    using global::Azure.Data.SchemaRegistry;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Schema registry group in Event Hubs.
    /// </summary>
    public sealed class SchemaGroup : ISchemaRegistry
    {
        /// <summary>
        /// Create schema group.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="credential"></param>
        /// <param name="logger"></param>
        public SchemaGroup(IOptions<SchemaRegistryOptions> options,
            ICredentialProvider credential, ILogger<SchemaGroup> logger)
            : this(options.Value, credential, logger)
        {
        }

        /// <summary>
        /// Create schema group.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="credential"></param>
        /// <param name="logger"></param>
        internal SchemaGroup(SchemaRegistryOptions options, ICredentialProvider credential,
            ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemaGroupName = options.SchemaGroupName;
            _schemaRegistry = new SchemaRegistryClient(options.FullyQualifiedNamespace,
                credential.Credential);
        }

        /// <inheritdoc/>
        public async ValueTask<string> RegisterAsync(IEventSchema schema,
            CancellationToken ct = default)
        {
            var key = string.Concat(schema.Name, schema.Version.ToString());
            if (_schemaToIdMap.TryGetValue(key, out var value))
            {
                return value;
            }

            var schemaProperties = await _schemaRegistry.RegisterSchemaAsync(
                _schemaGroupName, schema.Name, schema.Schema, schema.Type,
                ct).ConfigureAwait(false);

            var id = schemaProperties.Value.Id ?? string.Empty;
            _schemaToIdMap[key] = id;
            _logger.SchemaRegisteredSuccessfully(schema.Name);
            return id;
        }

        private readonly SchemaRegistryClient _schemaRegistry;
        private readonly ILogger _logger;
        private readonly string _schemaGroupName;
        private readonly ConcurrentDictionary<string, string> _schemaToIdMap = [];
    }

    /// <summary>
    /// Source-generated logging for <see cref="SchemaGroup"/>.
    /// </summary>
    internal static partial class SchemaGroupLogging
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information,
            Message = "Schema {Name} registered successfully.")]
        public static partial void SchemaRegisteredSuccessfully(this ILogger logger, string name);
    }
}

