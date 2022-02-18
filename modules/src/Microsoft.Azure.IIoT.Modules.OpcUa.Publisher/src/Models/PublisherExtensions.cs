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
                EndpointUrl = new Uri(model.EndpointUrl),
                UseSecurity = model.UseSecurity,
                OpcAuthenticationMode = (OpcAuthenticationMode)model.OpcAuthenticationMode,
                OpcAuthenticationPassword = model.Password,
                OpcAuthenticationUsername = model.UserName,
                OpcNodes = model.OpcNodes != null
                    ? model.OpcNodes.Select(n => n.ToServiceModel()).ToList()
                    : null,
                DataSetWriterGroup = model.DataSetWriterGroup,
                DataSetWriterId = model.DataSetWriterId,
                DataSetPublishingInterval = model.DataSetPublishingInterval,
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
                DisplayName = model.DisplayName,
                ExpandedNodeId = model.ExpandedNodeId,
                OpcPublishingInterval = model.OpcPublishingInterval,
                OpcSamplingInterval = model.OpcSamplingInterval,
                HeartbeatIntervalTimespan = model.HeartbeatIntervalTimespan,
                SkipFirst = model.SkipFirst,
                QueueSize = model.QueueSize,
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
                EndpointUrl = endpoint.EndpointUrl.AbsoluteUri,
                UseSecurity = endpoint.UseSecurity.GetValueOrDefault(false),
                OpcAuthenticationMode = (AuthenticationMode)endpoint.OpcAuthenticationMode,
                UserName = endpoint.OpcAuthenticationUsername,
                DataSetWriterGroup = endpoint.DataSetWriterGroup,
                DataSetWriterId = endpoint.DataSetWriterId,
                DataSetPublishingInterval = endpoint.DataSetPublishingInterval
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
                OpcSamplingInterval = model.OpcSamplingInterval,
                OpcPublishingInterval = model.OpcPublishingInterval,
                DataSetFieldId = model.DataSetFieldId,
                DisplayName = model.DisplayName,
                HeartbeatInterval = model.HeartbeatInterval,
                SkipFirst = model.SkipFirst,
                QueueSize = model.QueueSize,
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
                EndpointUrl = endpoint.EndpointUrl.AbsoluteUri,
                UseSecurity = endpoint.UseSecurity.GetValueOrDefault(false),
                OpcAuthenticationMode = (AuthenticationMode)endpoint.OpcAuthenticationMode,
                UserName = endpoint.OpcAuthenticationUsername,
                DataSetWriterGroup = endpoint.DataSetWriterGroup,
            };
        }

        /// <summary>
        /// Create an api model from service model ignoring the password
        /// </summary>
        public static List<DiagnosticInfoApiModel> ToApiModel(
            this List<JobDiagnosticInfoModel> model) {
            if (model == null) {
                return null;
            }

            return model.Select(e => new DiagnosticInfoApiModel {
                EndpointInfo = e.EndpointInfo.ToApiModel(),
                SentMessagesPerSec = e.SentMessagesPerSec,
                IngestionDuration = e.IngestionDuration,
                IngressDataChanges = e.IngressDataChanges,
                IngressValueChanges = e.IngressValueChanges,
                IngressBatchBlockBufferSize = e.IngressBatchBlockBufferSize,
                EncodingBlockInputSize = e.EncodingBlockInputSize,
                EncodingBlockOutputSize = e.EncodingBlockOutputSize,
                EncoderNotificationsProcessed = e.EncoderNotificationsProcessed,
                EncoderNotificationsDropped = e.EncoderNotificationsDropped,
                EncoderIoTMessagesProcessed = e.EncoderIoTMessagesProcessed,
                EncoderAvgNotificationsMessage = e.EncoderAvgNotificationsMessage,
                EncoderAvgIoTMessageBodySize = e.EncoderAvgIoTMessageBodySize,
                EncoderAvgIoTChunkUsage = e.EncoderAvgIoTChunkUsage,
                EstimatedIoTChunksPerDay = e.EstimatedIoTChunksPerDay,
                OutgressBatchBlockBufferSize = e.OutgressBatchBlockBufferSize,
                OutgressInputBufferCount = e.OutgressInputBufferCount,
                OutgressInputBufferDropped = e.OutgressInputBufferDropped,
                OutgressIoTMessageCount = e.OutgressIoTMessageCount,
                ConnectionRetries = e.ConnectionRetries,
                OpcEndpointConnected = e.OpcEndpointConnected,
                MonitoredOpcNodesSucceededCount = e.MonitoredOpcNodesSucceededCount,
                MonitoredOpcNodesFailedCount = e.MonitoredOpcNodesFailedCount,
            }).ToList();
        }
    }
}
