// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Model extension for Publisher module
    /// </summary>
    public static class PublisherExtensions {

        /// <summary>
        /// Create a service model for an api model
        /// </summary>
        public static PublishedNodesEntryModel ToServiceModel(
            this PublishNodesEndpointApiModel model) {
            if (model == null) {
                return null;
            }

            return new PublishedNodesEntryModel {
                EndpointUrl = !string.IsNullOrEmpty(model.EndpointUrl)
                    ? new Uri(model.EndpointUrl)
                    : null,
                UseSecurity = model.UseSecurity,
                OpcAuthenticationMode = (OpcAuthenticationMode)model.OpcAuthenticationMode,
                OpcAuthenticationPassword = model.Password,
                OpcAuthenticationUsername = model.UserName,
                OpcNodes = model.OpcNodes != null
                    ? model.OpcNodes.Select(n => n.ToServiceModel()).ToList()
                    : null,
                DataSetWriterGroup = model.DataSetWriterGroup,
                DataSetWriterId = model.DataSetWriterId,
                DataSetClassId = model.DataSetClassId,
                DataSetDescription = model.DataSetDescription,
                DataSetKeyFrameCount = model.DataSetKeyFrameCount,
                DataSetMetaDataSendInterval = model.DataSetMetaDataSendInterval,
                DataSetName = model.DataSetName,
                Tag = model.Tag,
                DataSetPublishingIntervalTimespan = model.DataSetPublishingIntervalTimespan,
                // only fill the DataSetPublishingInterval if the DataSetPublishingIntervalTimespan
                // was not provided.
                DataSetPublishingInterval = !model.DataSetPublishingIntervalTimespan.HasValue
                    ? model.DataSetPublishingInterval
                    : null,
            };
        }

        /// <summary>
        /// Create service model for an api model
        /// </summary>
        public static OpcNodeModel ToServiceModel(
            this PublishedNodeApiModel model) {
            if (model == null) {
                return null;
            }
            return new OpcNodeModel {
                Id = model.Id,
                DataSetFieldId = model.DataSetFieldId,
                DataSetClassFieldId = model.DataSetClassFieldId,
                DisplayName = model.DisplayName,
                ExpandedNodeId = model.ExpandedNodeId,

                OpcPublishingIntervalTimespan = model.OpcPublishingIntervalTimespan,
                // only fill the OpcPublishingInterval if the OpcPublishingIntervalTimespan
                // was not provided.
                OpcPublishingInterval = !model.OpcPublishingIntervalTimespan.HasValue
                    ? model.OpcPublishingInterval
                    : null,

                OpcSamplingIntervalTimespan = model.OpcSamplingIntervalTimespan,
                // only fill the OpcSamplingInterval if the OpcSamplingIntervalTimespan
                // was not provided.
                OpcSamplingInterval = !model.OpcSamplingIntervalTimespan.HasValue
                    ? model.OpcSamplingInterval
                    : null,

                HeartbeatIntervalTimespan = model.HeartbeatIntervalTimespan,
                // only fill the HeartbeatInterval if the HeartbeatIntervalTimespan
                // was not provided.
                HeartbeatInterval = !model.HeartbeatIntervalTimespan.HasValue
                    ? model.HeartbeatInterval
                    : null,
                SkipFirst = model.SkipFirst,
                QueueSize = model.QueueSize,
                DiscardNew = model.DiscardNew,
                DeadbandType = (IIoT.OpcUa.Publisher.Models.DeadbandType?)model.DeadbandType,
                DeadbandValue = model.DeadbandValue,
                DataChangeTrigger = (IIoT.OpcUa.Publisher.Models.DataChangeTriggerType?)model.DataChangeTrigger,
                EventFilter = model.EventFilter.ToServiceModel(),
                ConditionHandling = model.ConditionHandling.ToServiceModel(),
            };
        }

        /// <summary>
        /// Create an api model from service model ignoring the password
        /// </summary>
        public static List<PublishNodesEndpointApiModel> ToApiModel(
            this List<PublishedNodesEntryModel> endpoints) {
            if (endpoints == null) {
                return null;
            }
            return endpoints.Select(e => e.ToApiModel()).ToList();
        }

        /// <summary>
        /// Create an api model from service model ignoring the password
        /// </summary>
        public static PublishNodesEndpointApiModel ToApiModel(
            this PublishedNodesEntryModel endpoint) {
            if (endpoint == null) {
                return null;
            }

            return new PublishNodesEndpointApiModel {
                EndpointUrl = endpoint.EndpointUrl.OriginalString,
                UseSecurity = endpoint.UseSecurity,
                OpcAuthenticationMode = (AuthenticationMode)endpoint.OpcAuthenticationMode,
                UserName = endpoint.OpcAuthenticationUsername,
                DataSetWriterGroup = endpoint.DataSetWriterGroup,
                DataSetDescription = endpoint.DataSetDescription,
                DataSetKeyFrameCount = endpoint.DataSetKeyFrameCount,
                DataSetMetaDataSendInterval = endpoint.DataSetMetaDataSendInterval,
                DataSetName = endpoint.DataSetName,
                DataSetClassId = endpoint.DataSetClassId,
                DataSetWriterId = endpoint.DataSetWriterId,
                Tag = endpoint.Tag,
                DataSetPublishingIntervalTimespan = endpoint.DataSetPublishingIntervalTimespan,
                // only fill the DataSetPublishingInterval if the DataSetPublishingIntervalTimespan
                // was not provided.
                DataSetPublishingInterval = !endpoint.DataSetPublishingIntervalTimespan.HasValue
                    ? endpoint.DataSetPublishingInterval
                    : null,
            };
        }

        /// <summary>
        /// Create an api model from service model
        /// </summary>
        public static List<PublishedNodeApiModel> ToApiModel(
            this List<OpcNodeModel> model) {
            if (model == null) {
                return null;
            }

            return model.Select(n => n.ToApiModel()).ToList();
        }

        /// <summary>
        /// Create an api model from service model
        /// </summary>
        public static PublishedNodeApiModel ToApiModel(
            this OpcNodeModel model) {
            if (model == null) {
                return null;
            }
            return new PublishedNodeApiModel {
                Id = model.Id,
                ExpandedNodeId = model.ExpandedNodeId,
                DataSetFieldId = model.DataSetFieldId,
                DisplayName = model.DisplayName,
                DataSetClassFieldId = model.DataSetClassFieldId,
                OpcPublishingIntervalTimespan = model.OpcPublishingIntervalTimespan,
                OpcPublishingInterval = !model.OpcPublishingIntervalTimespan.HasValue
                    ? model.OpcPublishingInterval
                    : null,
                OpcSamplingIntervalTimespan = model.OpcSamplingIntervalTimespan,
                OpcSamplingInterval = !model.OpcSamplingIntervalTimespan.HasValue
                    ? model.OpcSamplingInterval
                    : null,
                HeartbeatIntervalTimespan = model.HeartbeatIntervalTimespan,
                HeartbeatInterval = !model.HeartbeatIntervalTimespan.HasValue
                    ? model.HeartbeatInterval
                    : null,
                SkipFirst = model.SkipFirst,
                QueueSize = model.QueueSize,
                DiscardNew = model.DiscardNew,
                DeadbandType = (DeadbandType?)model.DeadbandType,
                DeadbandValue = model.DeadbandValue,
                DataChangeTrigger = (DataChangeTriggerType?)model.DataChangeTrigger,
                EventFilter = model.EventFilter.ToApiModel(),
                ConditionHandling = model.ConditionHandling.ToApiModel()
            };
        }

        /// <summary>
        /// Create an api model from service model ignoring the password
        /// </summary>
        public static PublishNodesEndpointApiModel ToApiModel(
            this EndpointDiagnosticModel endpoint) {
            if (endpoint == null) {
                return null;
            }

            return new PublishNodesEndpointApiModel {
                EndpointUrl = endpoint.EndpointUrl.OriginalString,
                UseSecurity = endpoint.UseSecurity,
                OpcAuthenticationMode = (AuthenticationMode)endpoint.OpcAuthenticationMode,
                UserName = endpoint.OpcAuthenticationUsername,
                DataSetWriterGroup = endpoint.DataSetWriterGroup,
            };
        }

        /// <summary>
        /// Create an api model from service model
        /// </summary>
        public static List<DiagnosticInfoApiModel> ToApiModel(
            this List<JobDiagnosticInfoModel> model) {
            if (model == null) {
                return null;
            }
            return model.Select(e => e.ToApiModel()).ToList();
        }

        /// <summary>
        /// Create an api model from service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DiagnosticInfoApiModel ToApiModel(this JobDiagnosticInfoModel model) {
            return new DiagnosticInfoApiModel {
                Endpoint = model.Endpoint.ToApiModel(),
                SentMessagesPerSec = model.SentMessagesPerSec,
                IngestionDuration = model.IngestionDuration,
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
                OutgressBatchBlockBufferSize = model.OutgressBatchBlockBufferSize,
                OutgressInputBufferCount = model.OutgressInputBufferCount,
                OutgressInputBufferDropped = model.OutgressInputBufferDropped,
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
