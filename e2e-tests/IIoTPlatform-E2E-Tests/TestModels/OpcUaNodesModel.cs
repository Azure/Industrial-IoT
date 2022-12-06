// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.TestModels {

    /// <summary>
    /// Describing an OPC UA node to register
    /// </summary>
    public class OpcUaNodesModel {

        /// <summary>
        /// Node Identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Expanded Node identifier
        /// </summary>
        public string ExpandedNodeId { get; set; }

        /// <summary>
        /// Sampling interval
        /// </summary>
        public uint OpcSamplingInterval { get; set; }

        /// <summary>
        /// Publishing interval
        /// </summary>
        public uint OpcPublishingInterval { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// DataSetFieldId
        /// </summary>
        public string DataSetFieldId { get; set; }

        /// <summary>
        /// Heartbeat
        /// </summary>
        public uint? HeartbeatInterval { get; set; }

        /// <summary>
        /// Skip first value
        /// </summary>
        public bool? SkipFirst { get; set; }

        /// <summary>
        /// QueueSize value
        /// </summary>
        public uint QueueSize { get; set; }

        /// <summary>
        /// Data change trigger type
        /// </summary>
        public string DataChangeTrigger { get; set; }
    }
}
