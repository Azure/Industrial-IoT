// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Models {
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;

    /// <summary>
    /// Publisher sample model extensions
    /// </summary>
    public static class MonitoredItemMessageModelEx {

        /// <summary>
        /// Clone sample
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static MonitoredItemSampleModel Clone(this MonitoredItemSampleModel model) {
            if (model == null) {
                return null;
            }
            return new MonitoredItemSampleModel {
                SubscriptionId = model.SubscriptionId,
                EndpointId = model.EndpointId,
                DataSetId = model.DataSetId,
                NodeId = model.NodeId,
                ServerPicoseconds = model.ServerPicoseconds,
                ServerTimestamp = model.ServerTimestamp,
                SourcePicoseconds = model.SourcePicoseconds,
                SourceTimestamp = model.SourceTimestamp,
                Timestamp = model.Timestamp,
                TypeId = model.TypeId,
                Value = model.Value,
                Status = model.Status
            };
        }

        /// <summary>
        /// Try to convert json to sample model
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static MonitoredItemSampleModel ToServiceModel(this JToken message) {
            if (message.Type != JTokenType.Object || !(message is JObject sampleRoot)) {
                // Not a publisher sample object - not accepted
                return null;
            }

            var value = sampleRoot.Property("Value", StringComparison.InvariantCultureIgnoreCase)?
                .Value;
            if (value == null) {
                // No value
                return null;
            }

            //
            // Check if the value is a data value or if the value was flattened into the root.
            //
            var dataValue = sampleRoot;
            if (IsDataValue(value)) {
                dataValue = value as JObject;
                value = dataValue.GetValueOrDefault<JToken>("Value",
                    StringComparison.InvariantCultureIgnoreCase);
            }

            // check if value comes from the legacy publisher:
            var applicationUri = sampleRoot.GetValueOrDefault<string>("ApplicationUri",
                StringComparison.InvariantCultureIgnoreCase);
            if (applicationUri == null || applicationUri == string.Empty) {
                // this is not a legacy publisher message
                return new MonitoredItemSampleModel {
                    Value = GetValue(value, out var typeId),
                    TypeId = typeId?.ToString(),
                    Status = sampleRoot.GetValueOrDefault<string>(
                        nameof(MonitoredItemSampleModel.Status),
                            StringComparison.InvariantCultureIgnoreCase),
                    DataSetId = sampleRoot.GetValueOrDefault<string>(
                        nameof(MonitoredItemSampleModel.DataSetId),
                            StringComparison.InvariantCultureIgnoreCase),
                    Timestamp = sampleRoot.GetValueOrDefault<DateTime?>(
                        nameof(MonitoredItemSampleModel.Timestamp),
                            StringComparison.InvariantCultureIgnoreCase),
                    SubscriptionId = sampleRoot.GetValueOrDefault<string>(
                        nameof(MonitoredItemSampleModel.SubscriptionId),
                            StringComparison.InvariantCultureIgnoreCase),
                    EndpointId = sampleRoot.GetValueOrDefault<string>(
                        nameof(MonitoredItemSampleModel.EndpointId),
                            StringComparison.InvariantCultureIgnoreCase),
                    NodeId = sampleRoot.GetValueOrDefault<string>(
                        nameof(MonitoredItemSampleModel.NodeId),
                            StringComparison.InvariantCultureIgnoreCase),
                    SourcePicoseconds = dataValue.GetValueOrDefault<ushort?>(
                        nameof(MonitoredItemSampleModel.SourcePicoseconds),
                            StringComparison.InvariantCultureIgnoreCase),
                    ServerPicoseconds = dataValue.GetValueOrDefault<ushort?>(
                        nameof(MonitoredItemSampleModel.ServerPicoseconds),
                            StringComparison.InvariantCultureIgnoreCase),
                    SourceTimestamp = dataValue.GetValueOrDefault<DateTime?>(
                        nameof(MonitoredItemSampleModel.SourceTimestamp),
                            StringComparison.InvariantCultureIgnoreCase),
                    ServerTimestamp = dataValue.GetValueOrDefault<DateTime?>(
                        nameof(MonitoredItemSampleModel.ServerTimestamp),
                            StringComparison.InvariantCultureIgnoreCase),
                };
            }
            else {
                // legacy publisher message
                return new MonitoredItemSampleModel {
                    Value = GetValue(value, out var typeId),
                    TypeId = typeId?.ToString(),
                    DataSetId = sampleRoot.GetValueOrDefault<string>(
                    "DisplayName",
                        StringComparison.InvariantCultureIgnoreCase),
                    Timestamp = sampleRoot.GetValueOrDefault<DateTime?>(
                    nameof(MonitoredItemSampleModel.Timestamp),
                        StringComparison.InvariantCultureIgnoreCase),
                    SubscriptionId = "LegacyPublisher",
                    EndpointId = applicationUri,
                    NodeId = sampleRoot.GetValueOrDefault<string>(
                    nameof(MonitoredItemSampleModel.NodeId),
                        StringComparison.InvariantCultureIgnoreCase),
                    SourcePicoseconds = dataValue.GetValueOrDefault<ushort?>(
                    nameof(MonitoredItemSampleModel.SourcePicoseconds),
                        StringComparison.InvariantCultureIgnoreCase),
                    ServerPicoseconds = dataValue.GetValueOrDefault<ushort?>(
                    nameof(MonitoredItemSampleModel.ServerPicoseconds),
                        StringComparison.InvariantCultureIgnoreCase),
                    SourceTimestamp = dataValue.GetValueOrDefault<DateTime?>(
                    nameof(MonitoredItemSampleModel.SourceTimestamp),
                        StringComparison.InvariantCultureIgnoreCase),
                    ServerTimestamp = dataValue.GetValueOrDefault<DateTime?>(
                    nameof(MonitoredItemSampleModel.ServerTimestamp),
                        StringComparison.InvariantCultureIgnoreCase),
                };
            }
        }

        /// <summary>
        /// Get value from value object
        /// </summary>
        /// <param name="token"></param>
        /// <param name="typeId"></param>
        /// <returns></returns>
        private static JToken GetValue(JToken token, out JToken typeId) {
            if (token.Type != JTokenType.Object || !(token is JObject variant)) {
                typeId = null;
            }
            else if (variant.TryGetValue("Type",
                    StringComparison.InvariantCultureIgnoreCase, out typeId)) {

                variant.TryGetValue("Body",
                    StringComparison.InvariantCultureIgnoreCase, out token);
            }

            return token;
        }

        /// <summary>
        /// Is this a datavalue object?
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static bool IsDataValue(this JToken token) {
            var properties = new[] {
                "SourcePicoseconds", "ServerPicoseconds",
                "ServerTimestamp", "SourceTimestamp",
            };
            if (token.Type != JTokenType.Object || !(token is JObject dataValue)) {
                // Not a publisher sample object - not accepted
                return false;
            }
            return dataValue.Properties().Any(p => p.Name.AnyOf(properties, true));
        }
    }
}
