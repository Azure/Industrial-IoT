// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Selects and configures the event client a writer group publishes
    /// through.
    /// </summary>
    /// <remarks>
    /// This is how a writer group's <c>Transport</c> and
    /// <c>TransportConfiguration</c> become an actual client, including the
    /// factory-created client a group-specific configuration asks for and the
    /// scope that owns it.
    ///
    /// It was a nested type of the custom encoder's sink and also computed
    /// that sink's batching and queue limits. Those went with the sink: the
    /// native runtime emits a message per sample rather than batching, and its
    /// send queue is bounded by the egress options instead. Only the client
    /// selection outlived it.
    /// </remarks>
    internal sealed record class WriterGroupTransportOptions : IDisposable
    {
        /// <summary>
        /// Event client selected
        /// </summary>
        public IEventClient EventClient { get; }

        /// <summary>
        /// Create options
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="eventClients"></param>
        /// <param name="factories"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public WriterGroupTransportOptions(WriterGroupModel writerGroup,
            List<IEventClient> eventClients,
            Dictionary<string, IEventClientFactory> factories,
            IOptions<PublisherOptions> options, ILogger logger)
        {
            EventClient = eventClients
                    .Find(e => e.Name.Equals(writerGroup.Transport?.ToString(),
                        StringComparison.OrdinalIgnoreCase))
                ?? eventClients
                    .Find(e => e.Name.Equals(options.Value.DefaultTransport?.ToString(),
                        StringComparison.OrdinalIgnoreCase))
                ?? eventClients[0];

            if (string.IsNullOrEmpty(writerGroup.TransportConfiguration))
            {
                return;
            }
            if (!factories.TryGetValue(EventClient.Name, out var factory))
            {
                logger.CustomWriterGroupConfigurationCouldNotBeApplied(
                    EventClient.Name);
                return;
            }
            // Create event client with configuration from factory.
            try
            {
                _scope = factory.CreateEventClient(
                    writerGroup.TransportConfiguration, out var client);

                EventClient = client;
                logger.UsingTransportWithCustomWriterGroupConfiguration(
                    EventClient.Name);
            }
            catch (Exception e)
            {
                logger.CustomWriterGroupConfigurationCouldNotBeAppliedWithError(
                    EventClient.Name, e.Message);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _scope?.Dispose();
        }

        private readonly IDisposable? _scope;
    }

    /// <summary>
    /// Source-generated logging definitions for the writer group transport
    /// </summary>
    internal static partial class WriterGroupTransportLogging
    {
        private const int EventClass = 200;

        [LoggerMessage(EventId = EventClass + 13, Level = LogLevel.Information,
            Message = "Using transport {Transport} with custom writer group configuration.")]
        public static partial void UsingTransportWithCustomWriterGroupConfiguration(this ILogger logger,
            string transport);

        [LoggerMessage(EventId = EventClass + 14, Level = LogLevel.Warning,
            Message = "Custom writer group configuration could not be applied to transport {Transport} " +
            "- using default.")]
        public static partial void CustomWriterGroupConfigurationCouldNotBeApplied(this ILogger logger,
            string transport);

        [LoggerMessage(EventId = EventClass + 15, Level = LogLevel.Error,
            Message = "Custom writer group configuration could not be applied to transport {Transport} " +
            "due to bad configuration (Error: {Error}) - using default.")]
        public static partial void CustomWriterGroupConfigurationCouldNotBeAppliedWithError(this ILogger logger,
            string transport, string error);
    }
}
