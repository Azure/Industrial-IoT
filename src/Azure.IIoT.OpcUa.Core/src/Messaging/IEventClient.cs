// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging
{
    /// <summary>
    /// Send events
    /// </summary>
    public interface IEventClient
    {
        /// <summary>
        /// Name of the technology implementing the event client
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Max event payload size. This can be used to serialize
        /// a larger payload and send them as chunks.
        /// </summary>
        int MaxEventPayloadSizeInBytes { get; }

        /// <summary>
        /// The opaque circuit or broker identity that the
        /// event will be sent with or through.
        /// </summary>
        string Identity { get; }

        /// <summary>
        /// Create event
        /// </summary>
        /// <returns></returns>
        IEvent CreateEvent();
    }
}
