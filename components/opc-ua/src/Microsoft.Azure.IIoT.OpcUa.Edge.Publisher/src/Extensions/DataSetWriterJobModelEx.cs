// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using System.Linq;

    /// <summary>
    /// Extensions for data set writer
    /// </summary>
    public static class DataSetWriterJobModelEx {

        /// <summary>
        /// Convert to message trigger configuration
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static IPubSubMessageTriggerConfig ToMessageTriggerConfig(
            this DataSetWriterGroupModel model) {
            return new PubSubMessageTriggerConfig {
                DataSets = model.DataSetWriter.DataSets.Select(d => d.Clone()).ToList(),
                KeyframeMessageInterval = model.DataSetWriter.KeyframeMessageInterval,
                MetadataMessageInterval = model.DataSetWriter.MetadataMessageInterval,
                PublishingInterval = model.PublishingInterval,
                SendChangeMessages = model.SendChangeMessages,
                Connection = model.Connection.Clone()
            };
        }

        /// <summary>
        /// Convert to message trigger configuration
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static IPubSubEncodingConfig ToEncodingConfig(
            this DataSetWriterGroupModel model) {
            return new PubSubEncodingConfig {
                ContentType = model.DataSetWriter.ContentEncoding.ToContentType(),
                FieldContentMask = (uint)model.DataSetWriter.FieldContent.ToStackType(),
                DataSetMessageContentMask = model.DataSetWriter.DataSetContent
                    .ToStackType(model.DataSetWriter.ContentEncoding),
                NetworkMessageContentMask = model.DataSetWriter.NetworkMessageContent
                    .ToStackType(model.DataSetWriter.ContentEncoding)
            };
        }

        /// <summary>
        /// Convert to engine configuration
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static IEngineConfiguration ToEngineConfig(
            this DataSetWriterGroupModel model) {
            return new PublisherEngineConfig {
                BatchSize = model.Engine?.BatchSize,
                DiagnosticsInterval = model.Engine?.DiagnosticsInterval
            };
        }
    }
}