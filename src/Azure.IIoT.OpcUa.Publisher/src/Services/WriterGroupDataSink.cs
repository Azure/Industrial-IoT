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
            // Create a central control object
            //
            // We should pull the keys (display names) in a central controller object
            // This controller object becomes the live view of the writers and groups
            // It feeds the entries to the subscription system as template
            //
            // We move the lookup of relative paths and display name here then. Then we 
            // have it available for later matching of incomding messages. 
            // Good: We can also resolve nodes to subscribe to recursively here as well.
            // Bad: if it fails?  In subscription (stack) we retry, I guess we have to
            // retry at the writer group level as well then?
            // We should not move this all to subscription or else we cannot handle
            // writes until we have the subscription applied - that feels too late.
            //
            // We should also collect metadata here!  This anyway feels logically better
            // here than in subscription/stack.
            //
            // In handler we receive bytes, we can tryDecode but if it fails we need to
            // buffer, we need to clear the queues after some timeout. The cache contains
            // queue of buffers and topic. Once decoded we have a topic and network message
            //
            // The network message can have one or more datasets (key/value, or just value).
            // If network message has group, group must match or we fail
            // For each data set, we might get a writer name, and resolve the writer by
            // name from the central writer group lookup.
            // If we fail to find a writer with the name, we fail.
            // --> Writer id is a integer, which we should track in central object now 
            // not in subscription
            //
            // If we do not have a writer name because the message does not contain it
            // We match the key/values to a writer, all of them should be in one and
            // we select the first one.
            // 
            // Matching logic to find the publishedVariables:
            // For all keys in the dataset
            //  key name should == field name in writer
            //  topic of variable should match passed in topic if available
            //  topic of writer should match passed in topic if available
            //  
            // We try and find the topic that matches a variable/event if we do not find
            // we find the writer object that matches the topic, if we do not find that
            // (because no topic at writer level) we use the group.
            //
            // Now we have a set of PublishedVariable entries, use those to send a batch
            // write to the server.
            //
            // What about event messages?
            // What about actions?
            // 


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
