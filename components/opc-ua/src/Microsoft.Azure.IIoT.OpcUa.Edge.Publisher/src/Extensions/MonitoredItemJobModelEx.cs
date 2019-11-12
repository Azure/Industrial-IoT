// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using System.Linq;

    /// <summary>
    /// Extensions for monitored item job
    /// </summary>
    public static class MonitoredItemJobModelEx {

        /// <summary>
        /// Convert to message trigger configuration
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static IMonitoredItemSampleTriggerConfig ToMessageTriggerConfig(
            this MonitoredItemJobModel model) {
            return new MonitoredItemSampleTriggerConfig {
                Subscriptions = model.Subscriptions.Select(s => s.Clone()).ToList()
            };
        }

        /// <summary>
        /// Convert to message trigger configuration
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static IMonitoredItemEncodingConfig ToEncodingConfig(
            this MonitoredItemJobModel model) {
            return new MonitoredItemEncodingConfig {
                ContentType = (model.Content?.Encoding).ToContentType(),
                MessageContentMask = (model.Content?.Fields).ToStackType(model.Content?.Encoding)
            };
        }

        /// <summary>
        /// Convert to engine configuration
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static IEngineConfiguration ToEngineConfig(
            this MonitoredItemJobModel model) {
            return new PublisherEngineConfig {
                BatchSize = model.Engine?.BatchSize,
                DiagnosticsInterval = model.Engine?.DiagnosticsInterval
            };
        }
    }
}