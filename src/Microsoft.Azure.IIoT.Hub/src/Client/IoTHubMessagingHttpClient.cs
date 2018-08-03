// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Client {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Messaging service client - injects messages on behalf of
    /// a device or module.
    /// </summary>
    public class IoTHubMessagingHttpClient : IoTHubHttpClientBase,
        IIoTHubMessagingServices {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public IoTHubMessagingHttpClient(IHttpClient httpClient,
            IIoTHubConfig config, ILogger logger) :
            base(httpClient, config, logger) {
        }

        /// <inheritdoc/>
        public Task SendAsync(string deviceId, string moduleId,
            IEnumerable<DeviceMessageModel> messages) {
            if (messages == null) {
                throw new ArgumentNullException(nameof(messages));
            }
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            if (!messages.Any()) {
                return Task.CompletedTask;
            }
            var body = ToJson(messages);
            return Retry.WithExponentialBackoff(_logger, async () => {
                var to = $"/devices/{ToResourceId(deviceId, moduleId)}/messages/events";
                var request = NewRequest(to);
                request.AddHeader("iothub-operation", "d2c");
                request.AddHeader("iothub-to", to);
                request.AddHeader("Content-Type", "application/vnd.microsoft.iothub.json");
                request.SetContent(body);
                var response = await _httpClient.PostAsync(request);
                response.Validate();
            });
        }

        /// <summary>
        /// Encoding courtesy of HttpTransportHandler.cs in Azure IoT hub SDK.
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        private static string ToJson(IEnumerable<DeviceMessageModel> messages) {
            using (var sw = new StringWriter())
            using (var writer = new JsonTextWriter(sw)) {
                writer.WriteStartArray();
                foreach (var message in messages) {
                    writer.WriteStartObject();
                    writer.WritePropertyName("body");
                    writer.WriteValue(
                        Convert.ToBase64String(
                            Encoding.UTF8.GetBytes(message.Payload.ToString())));
                    if (message.Properties?.Any() ?? false) {
                        writer.WritePropertyName("properties");
                        writer.WriteStartObject();
                        foreach (var property in message.Properties) {
                            if (kHeaderMap.TryGetValue(property.Key, out var header)) {
                                writer.WritePropertyName(header);
                            }
                            else {
                                writer.WritePropertyName(kHttpAppPropertyPrefix +
                                    property.Key);
                            }
                            writer.WriteValue(property.Value);
                        }
                        writer.WriteEndObject();
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                return sw.ToString();
            }
        }

        private static readonly IDictionary<string, string> kHeaderMap =
            new Dictionary<string, string> {
                { SystemPropertyNames.Ack, "iothub-ack" },
                { SystemPropertyNames.CorrelationId, "iothub-correlationid" },
                { SystemPropertyNames.ExpiryTimeUtc, "iothub-expiry" },
                { SystemPropertyNames.MessageId, "iothub-messageid" },
                { SystemPropertyNames.UserId, "iothub-userid" },
                { SystemPropertyNames.MessageSchema, "iothub-messageschema" },
                { SystemPropertyNames.CreationTimeUtc, "iothub-creationtimeutc" },
                { SystemPropertyNames.ContentType, "iothub-contenttype" },
                { SystemPropertyNames.ContentEncoding, "iothub-contentencoding" }
            };
        private const string kHttpAppPropertyPrefix = "iothub-app-";
    }
}
