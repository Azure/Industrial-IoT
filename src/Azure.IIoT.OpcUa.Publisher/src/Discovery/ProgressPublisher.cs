// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Discovery
{
    using Azure.IIoT.OpcUa.Shared.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Utils;
    using Microsoft.Azure.IIoT;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Tasks;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery progress message sender
    /// </summary>
    public class ProgressPublisher : ProgressLogger
    {
        /// <summary>
        /// Create listener
        /// </summary>
        /// <param name="events"></param>
        /// <param name="processor"></param>
        /// <param name="serializer"></param>
        /// <param name="identity"></param>
        /// <param name="logger"></param>
        public ProgressPublisher(IEventEmitter events, ITaskProcessor processor,
            IJsonSerializer serializer, IProcessIdentity identity, ILogger logger)
            : base(logger)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }

        /// <summary>
        /// Send progress
        /// </summary>
        /// <param name="progress"></param>
        protected override void Send(DiscoveryProgressModel progress)
        {
            progress.DiscovererId = PublisherModelEx.CreatePublisherId(
                _identity.ProcessId, _identity.Id);
            base.Send(progress);
            _processor.TrySchedule(() => SendAsync(progress));
        }

        /// <summary>
        /// Send progress
        /// </summary>
        /// <param name="progress"></param>
        /// <returns></returns>
        private Task SendAsync(DiscoveryProgressModel progress)
        {
            return Try.Async(() => _events.SendEventAsync(
                _serializer.SerializeToMemory((object)progress).ToArray(), ContentMimeType.Json,
                MessageSchemaTypes.DiscoveryMessage, "utf-8"));
        }

        private readonly IJsonSerializer _serializer;
        private readonly IProcessIdentity _identity;
        private readonly IEventEmitter _events;
        private readonly ITaskProcessor _processor;
    }
}
