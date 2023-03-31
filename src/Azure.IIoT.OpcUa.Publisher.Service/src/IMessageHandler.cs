// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Handles events
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Event message schema
        /// </summary>
        string MessageSchema { get; }

        /// <summary>
        /// Handle event
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="payload"></param>
        /// <param name="properties"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask HandleAsync(string deviceId, string? moduleId,
            ReadOnlyMemory<byte> payload,
            IReadOnlyDictionary<string, string?> properties,
            CancellationToken ct = default);
    }
}
