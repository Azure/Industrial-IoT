// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using Microsoft.Azure.IIoT.Messaging;
    using System.Threading.Tasks;

    /// <summary>
    /// Message sink
    /// </summary>
    public interface IMessageSink {

        /// <summary>
        /// Max message size sink can handle
        /// </summary>
        int MaxMessageSize { get; }

        /// <summary>
        /// Create message
        /// </summary>
        /// <returns></returns>
        ITelemetryEvent CreateMessage();

        /// <summary>
        /// Send messages and dispose them
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        Task SendAsync(ITelemetryEvent messages);
    }
}