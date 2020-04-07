// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Client {
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Utils;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Messaging service client - injects messages on behalf of
    /// a device or module.
    /// </summary>
    public sealed class IoTHubMessagingHttpClient : IoTHubHttpClientBase,
        IIoTHubTelemetryServices {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public IoTHubMessagingHttpClient(IHttpClient httpClient,
            IIoTHubConfig config, IJsonSerializer serializer, ILogger logger) :
            base(httpClient, config, serializer, logger) {
        }

        /// <inheritdoc/>
        public Task SendAsync(string deviceId, string moduleId,
            EventModel message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, async () => {
                var to = $"/devices/{ToResourceId(deviceId, moduleId)}/messages/events";
                var request = NewRequest(to);
                foreach (var property in message.Properties) {
                    if (kHeaderMap.TryGetValue(property.Key, out var header)) {
                        request.AddHeader(header, property.Value);
                    }
                    else {
                        request.AddHeader(kHttpAppPropertyPrefix + property.Key,
                            property.Value);
                    }
                }
                request.AddHeader("iothub-operation", "d2c");
                request.AddHeader("iothub-to", to);
                _serializer.SerializeToRequest(request, message.Payload);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
            });
        }

        private static readonly IDictionary<string, string> kHeaderMap =
            new Dictionary<string, string> {
                { SystemProperties.Ack, "iothub-ack" },
                { SystemProperties.CorrelationId, "iothub-correlationid" },
                { SystemProperties.ExpiryTimeUtc, "iothub-expiry" },
                { SystemProperties.MessageId, "iothub-messageid" },
                { SystemProperties.UserId, "iothub-userid" },
                { SystemProperties.MessageSchema, "iothub-messageschema" },
                { SystemProperties.CreationTimeUtc, "iothub-creationtimeutc" },
                { SystemProperties.ContentType, "iothub-contenttype" },
                { SystemProperties.ContentEncoding, "iothub-contentencoding" },
                { SystemProperties.InterfaceId, "iothub-interface-id"}
            };
        private const string kHttpAppPropertyPrefix = "iothub-app-";
    }
}
