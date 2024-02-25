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
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Target of network messages when in subscriber mode.
    /// </summary>
    public sealed class WriterGroupDataSink : IWriterGroup
    {
        /// <summary>
        /// Create writer group sink
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="options"></param>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public WriterGroupDataSink(WriterGroupModel writerGroup,
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
        public ValueTask StartAsync(CancellationToken ct)
        {
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public ValueTask UpdateAsync(WriterGroupModel writerGroup, CancellationToken ct)
        {
            return ValueTask.CompletedTask;
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
