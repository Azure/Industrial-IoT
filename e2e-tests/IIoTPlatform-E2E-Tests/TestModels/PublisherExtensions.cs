// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models {
    using IIoTPlatform_E2E_Tests.TestModels;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using System.Linq;

    /// <summary>
    /// Model extension for Publisher module
    /// </summary>
    public static class PublisherExtensions {

        /// <summary>
        /// Create an api model
        /// </summary>
        public static PublishNodesEndpointApiModel ToApiModel(
            this PublishedNodesEntryModel model) {
            if (model == null) {
                return null;
            }
            return new PublishNodesEndpointApiModel {
                DataSetWriterGroup = model.DataSetWriterGroup,
                DataSetWriterId = model.DataSetWriterId,
                DataSetPublishingInterval = model.DataSetPublishingInterval,
                EndpointUrl = model.EndpointUrl,
                UseSecurity = model.UseSecurity,
                Password = model.OpcAuthenticationPassword,
                UserName = model.OpcAuthenticationUsername,
                OpcNodes = model.OpcNodes != null
                    ? model.OpcNodes.Select(n => n.ToApiModel()).ToList()
                    : null,
            };
        }


        /// <summary>
        /// Create an api model
        /// </summary>
        public static PublishedNodeApiModel ToApiModel(
            this OpcUaNodesModel model) {
            if (model == null) {
                return null;
            }
            return new PublishedNodeApiModel {
                Id = model.Id,
                DisplayName = model.DisplayName,
                DataSetFieldId = model.DataSetFieldId,
                ExpandedNodeId = model.ExpandedNodeId,
                OpcPublishingInterval = (int?)model.OpcPublishingInterval,
                OpcSamplingInterval = (int?)model.OpcSamplingInterval,
                HeartbeatInterval = (int?)model.HeartbeatInterval,
                SkipFirst = model.SkipFirst,
                QueueSize = model.QueueSize,
            };
        }
    }
}
