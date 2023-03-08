// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Furly.Extensions.Messaging;
    using System.Threading.Tasks;

    /// <summary>
    /// Message sink
    /// </summary>
    public interface IMessageSink
    {
        /// <summary>
        /// Max message size sink can handle
        /// </summary>
        int MaxMessageSize { get; }

        /// <summary>
        /// Create message
        /// </summary>
        /// <returns></returns>
        IEvent CreateMessage();

        /// <summary>
        /// Send message and dispose
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendAsync(IEvent message);
    }
}
