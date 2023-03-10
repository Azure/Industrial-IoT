// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Furly.Extensions.Hosting;
    using Furly.Extensions.Messaging;
    using System;

    /// <summary>
    /// Publisher identity
    /// </summary>
    public sealed class PublisherIdentity : IProcessIdentity
    {
        /// <inheritdoc/>
        public string Id => _events.Identity;
        /// <inheritdoc/>
        public string Name => "OPC Publisher";
        /// <inheritdoc/>
        public string Description => "Connect OPC UA servers to Azure.";

        /// <summary>
        /// Create identity
        /// </summary>
        /// <param name="events"></param>
        public PublisherIdentity(IEventClient events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        private readonly IEventClient _events;
    }
}
