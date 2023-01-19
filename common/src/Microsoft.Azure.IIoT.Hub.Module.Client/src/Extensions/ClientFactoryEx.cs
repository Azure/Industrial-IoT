// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using System.Threading.Tasks;

    /// <summary>
    /// Factory extensions
    /// </summary>
    public static class ClientFactoryEx {

        /// <summary>
        /// Create client
        /// </summary>
        /// <returns></returns>
        public static Task<IClient> CreateAsync(this IClientFactory factory) {
            return factory.CreateAsync("Module");
        }

        /// <summary>
        /// Create message
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        /// <param name="contentEncoding"></param>
        /// <param name="contentType"></param>
        /// <param name="messageSchema"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="routingInfo"></param>
        /// <returns></returns>
        public static ITelemetryEvent CreateMessage(this IClient client, byte[] data, string contentEncoding,
            string contentType, string messageSchema, string deviceId = null, string moduleId = null,
            string routingInfo = null) {
            var msg = client.CreateMessage();
            msg.Body = data;
            msg.ContentType = contentType;
            msg.ContentEncoding = contentEncoding;
            msg.MessageSchema = messageSchema;
            msg.DeviceId = deviceId;
            msg.ModuleId = moduleId;
            msg.RoutingInfo = routingInfo;
            return msg;
        }
    }
}
