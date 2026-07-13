// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging
{
    using System;

    /// <summary>
    /// Declares the event features that an event client applies without
    /// reducing or ignoring their requested semantics.
    /// </summary>
    [Flags]
    public enum EventClientCapabilities
    {
        /// <summary>
        /// The client can send event payloads.
        /// </summary>
        Payload = 1,

        /// <summary>
        /// The client preserves the event topic.
        /// </summary>
        Topic = 1 << 1,

        /// <summary>
        /// The client preserves the requested quality of service.
        /// </summary>
        QualityOfService = 1 << 2,

        /// <summary>
        /// The client preserves the requested retain setting.
        /// </summary>
        Retain = 1 << 3,

        /// <summary>
        /// The client preserves the requested time to live.
        /// </summary>
        TimeToLive = 1 << 4,

        /// <summary>
        /// The client preserves the content type.
        /// </summary>
        ContentType = 1 << 5,

        /// <summary>
        /// The client preserves the content encoding.
        /// </summary>
        ContentEncoding = 1 << 6,

        /// <summary>
        /// The client preserves custom event properties.
        /// </summary>
        CustomProperties = 1 << 7,

        /// <summary>
        /// The client applies CloudEvents headers.
        /// </summary>
        CloudEvents = 1 << 8,

        /// <summary>
        /// The client registers and applies event schemas.
        /// </summary>
        Schema = 1 << 9
    }

    /// <summary>
    /// Optional explicit event-client capability contract. Consumers that
    /// require a feature must reject clients that do not declare it rather
    /// than relying on a fluent <see cref="IEvent"/> implementation to
    /// silently ignore the setting.
    /// </summary>
    public interface IEventClientCapabilities
    {
        /// <summary>
        /// Gets the faithfully supported event features.
        /// </summary>
        EventClientCapabilities Capabilities { get; }
    }
}
