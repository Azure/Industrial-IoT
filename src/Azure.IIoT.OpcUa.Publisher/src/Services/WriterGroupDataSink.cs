// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;

    /// <summary>
    /// Target of network messages when in subscriber mode.
    /// <para>
    /// We should also collect metadata here!  This anyway feels logically better
    /// here than in subscription/stack.
    /// </para>
    /// <para>
    /// In handler we receive bytes, we can tryDecode but if it fails we need to
    /// buffer, we need to clear the queues after some timeout. The cache contains
    /// queue of buffers and topic. Once decoded we have a topic and network message
    /// </para>
    /// <para>
    /// The network message can have one or more datasets (key/value, or just value).
    /// If network message has group, group must match or we fail
    /// For each data set, we might get a writer name, and resolve the writer by
    /// name from the central writer group lookup.
    /// If we fail to find a writer with the name, we fail.
    /// --> Writer id is a integer, which we should track in central object now
    /// not in subscription
    /// </para>
    /// </summary>
    public sealed class WriterGroupDataSink : IDisposable
    {
        /// <summary>
        /// Create writer group sink
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="options"></param>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public WriterGroupDataSink(IWriterGroup writerGroup,
            IOptions<PublisherOptions> options, IOpcUaClientManager<ConnectionModel> client,
            ILogger<WriterGroupDataSink> logger)
        {
            ArgumentNullException.ThrowIfNull(writerGroup, nameof(writerGroup));

            _options = options ??
                throw new ArgumentNullException(nameof(options));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _client = client ??
                throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        private readonly IOpcUaClientManager<ConnectionModel> _client;
        private readonly IOptions<PublisherOptions> _options;
        private readonly ILogger<WriterGroupDataSink> _logger;
    }
}
