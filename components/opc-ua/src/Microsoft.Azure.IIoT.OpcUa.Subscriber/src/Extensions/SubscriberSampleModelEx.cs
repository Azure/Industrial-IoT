// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Models {
    using System;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Publisher sample model extensions
    /// </summary>
    public static class SubscriberSampleModelEx {

        /// <summary>
        /// Clone sample
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SubscriberSampleModel Clone(this SubscriberSampleModel model) {
            return new SubscriberSampleModel {
                SubscriptionId = model.SubscriptionId,
                EndpointId = model.EndpointId,
                DataSetId = model.DataSetId,
                NodeId = model.NodeId,
                ServerPicoseconds = model.ServerPicoseconds,
                ServerTimestamp = model.ServerTimestamp,
                SourcePicoseconds = model.SourcePicoseconds,
                SourceTimestamp = model.SourceTimestamp,
                Timestamp = model.Timestamp,
                Value = model.Value
            };
        }

        /// <summary>
        /// Try to convert json to sample model
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static SubscriberSampleModel ToSubscriberSampleModel(this JToken message) {
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

            var result = new SubscriberSampleModel() {
                Timestamp = DateTime.Now
            };

            // check if value comes from the legacy publisher:
            var applicationUri = sampleRoot.GetValueOrDefault<string>("ApplicationUri", 
                StringComparison.InvariantCultureIgnoreCase);                
            if (applicationUri == null || applicationUri == string.Empty) {                
                result.EndpointId = sampleRoot.GetValueOrDefault<string>("EndpointId",
                    StringComparison.InvariantCultureIgnoreCase);
                result.SubscriptionId = sampleRoot.GetValueOrDefault<string>("SubscriptionId",
                    StringComparison.InvariantCultureIgnoreCase);
                result.DataSetId = sampleRoot.GetValueOrDefault<string>("DataSetId",
                    StringComparison.InvariantCultureIgnoreCase);
            }
            else {
                result.EndpointId = applicationUri;
                result.SubscriptionId = "LegacyPublisher";
                result.DataSetId = sampleRoot.GetValueOrDefault<string>("DisplayName",
                    StringComparison.InvariantCultureIgnoreCase);
            }

            result.NodeId = sampleRoot.GetValueOrDefault<string>("NodeId",
                StringComparison.InvariantCultureIgnoreCase);

            // Check if the value is a data value or if the value was flattened into the root.
            var dataValue = sampleRoot;
            if (IsDataValue(value)) {
                dataValue = value as JObject;
                result.Value = dataValue.GetValueOrDefault<JToken>("Value",
                    StringComparison.InvariantCultureIgnoreCase);
            }
            else {
                result.Value = value;
            }

            result.SourcePicoseconds = dataValue.GetValueOrDefault<ushort?>("SourcePicoseconds",
                StringComparison.InvariantCultureIgnoreCase);
            result.ServerPicoseconds = dataValue.GetValueOrDefault<ushort?>("ServerPicoseconds",
                StringComparison.InvariantCultureIgnoreCase);
            result.SourceTimestamp = dataValue.GetValueOrDefault<DateTime?>("SourceTimestamp",
                StringComparison.InvariantCultureIgnoreCase);
            result.ServerTimestamp = dataValue.GetValueOrDefault<DateTime?>("ServerTimestamp",
                StringComparison.InvariantCultureIgnoreCase);
            return result;
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
