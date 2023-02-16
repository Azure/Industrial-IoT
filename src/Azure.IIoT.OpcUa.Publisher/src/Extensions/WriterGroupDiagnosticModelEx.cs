// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models {
    /// <summary>
    /// Writer group Model extensions
    /// </summary>
    public static class WriterGroupDiagnosticModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static WriterGroupDiagnosticModel Clone(this WriterGroupDiagnosticModel model) {
            if (model == null) {
                return null;
            }
            return new WriterGroupDiagnosticModel {
                SentMessagesPerSec = model.SentMessagesPerSec,
                IngestionStart = model.IngestionStart,
                IngressDataChanges = model.IngressDataChanges,
                IngressValueChanges = model.IngressValueChanges,
                IngressBatchBlockBufferSize = model.IngressBatchBlockBufferSize,
                EncodingBlockInputSize = model.EncodingBlockInputSize,
                EncodingBlockOutputSize = model.EncodingBlockOutputSize,
                EncoderNotificationsProcessed = model.EncoderNotificationsProcessed,
                EncoderNotificationsDropped = model.EncoderNotificationsDropped,
                EncoderMaxMessageSplitRatio = model.EncoderMaxMessageSplitRatio,
                EncoderIoTMessagesProcessed = model.EncoderIoTMessagesProcessed,
                EncoderAvgNotificationsMessage = model.EncoderAvgNotificationsMessage,
                EncoderAvgIoTMessageBodySize = model.EncoderAvgIoTMessageBodySize,
                EncoderAvgIoTChunkUsage = model.EncoderAvgIoTChunkUsage,
                EstimatedIoTChunksPerDay = model.EstimatedIoTChunksPerDay,
                OutgressInputBufferCount = model.OutgressInputBufferCount,
                OutgressInputBufferDropped = model.OutgressInputBufferDropped,
                IngressDataChangesInLastMinute = model.IngressDataChangesInLastMinute,
                IngressValueChangesInLastMinute = model.IngressValueChangesInLastMinute,
                OutgressIoTMessageCount = model.OutgressIoTMessageCount,
                ConnectionRetries = model.ConnectionRetries,
                OpcEndpointConnected = model.OpcEndpointConnected,
                MonitoredOpcNodesSucceededCount = model.MonitoredOpcNodesSucceededCount,
                MonitoredOpcNodesFailedCount = model.MonitoredOpcNodesFailedCount,
                IngressEventNotifications = model.IngressEventNotifications,
                IngressEvents = model.IngressEvents
            };
        }
    }
}