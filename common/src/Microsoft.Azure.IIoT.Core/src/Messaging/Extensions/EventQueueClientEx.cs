// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Event queue client extensions
    /// </summary>
    public static class EventQueueClientEx {

        /// <summary>
        /// Send json payload
        /// </summary>
        /// <param name="client"></param>
        /// <param name="payload"></param>
        /// <param name="contentType"></param>
        /// <param name="partitionKey"></param>
        public static Task SendAsync(this IEventQueueClient client, JToken payload,
            string contentType, string partitionKey) {
            return client.SendAsync(payload,
                new Dictionary<string, string> {
                    [CommonProperties.kContentType] = contentType
                }, partitionKey);
        }

        /// <summary>
        /// Send json payload
        /// </summary>
        /// <param name="client"></param>
        /// <param name="payload"></param>
        /// <param name="properties"></param>
        /// <param name="partitionKey"></param>
        public static Task SendAsync(this IEventQueueClient client, JToken payload,
            IDictionary<string, string> properties, string partitionKey = null) {
            return client.SendAsync(Encoding.UTF8.GetBytes(payload.ToString()),
                properties, partitionKey);
        }
    }
}
