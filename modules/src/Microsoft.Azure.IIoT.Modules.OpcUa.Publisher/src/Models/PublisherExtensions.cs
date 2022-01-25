// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Models {
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
                OpcNodes = model.OpcNodes.Select(n => n.ToServiceModel()).ToList(),
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
        /// Create an api model from service model
        /// </summary>
        public static PublishedNodesResponseApiModel ToApiModel(
            this List<string> model) {
            if (model == null) {
                return null;
            }

            return new PublishedNodesResponseApiModel {
                StatusMessage = model
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

            return endpoints.Select(e => new PublishNodesEndpointApiModel {
                EndpointUrl = e.EndpointUrl.AbsoluteUri,
                UseSecurity = (bool)e.UseSecurity,
                OpcAuthenticationMode = (AuthenticationMode)e.OpcAuthenticationMode,
                UserName = e.OpcAuthenticationUsername,
                DataSetWriterGroup = e.DataSetWriterGroup,
                DataSetWriterId = e.DataSetWriterId,
                DataSetPublishingInterval = e.DataSetPublishingInterval
            }).ToList();
        }
    }
}
