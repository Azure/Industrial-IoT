// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging
{
    /// <summary>
    /// Schema of an event
    /// </summary>
    public interface IEventSchema
    {
        /// <summary>
        /// Mime type
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Schema name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Version
        /// </summary>
        ulong Version { get; }

        /// <summary>
        /// Schema content
        /// </summary>
        string Schema { get; }

        /// <summary>
        /// An identifier that provides context
        /// </summary>
        string Id { get; }
    }
}
