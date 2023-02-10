// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using Microsoft.Azure.IIoT.Messaging;
    using System.Collections.Generic;

    /// <summary>
    /// Message client extensions
    /// </summary>
    public static class MessageClientEx {

        /// <summary>
        /// Create message
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        /// <param name="contentEncoding"></param>
        /// <param name="contentType"></param>
        /// <param name="messageSchema"></param>
        /// <param name="routingInfo"></param>
        /// <returns></returns>
        public static ITelemetryEvent CreateMessage(this IMessageClient client,
            IReadOnlyList<byte[]> data, string contentEncoding,
            string contentType, string messageSchema,
            string routingInfo = null) {
            var msg = client.CreateTelemetryEvent();
            msg.Buffers = data;
            msg.ContentType = contentType;
            msg.ContentEncoding = contentEncoding;
            msg.MessageSchema = messageSchema;
            msg.RoutingInfo = routingInfo;
            return msg;
        }
    }
}
