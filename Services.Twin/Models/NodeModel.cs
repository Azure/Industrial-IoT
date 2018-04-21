// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services.Models {

    public class NodeModel {
        /// <summary>
        /// Id of node and parent id.
        /// </summary>
        public string Id { get; set; }
        public string ParentNode { get; set; }

        /// <summary>
        /// Whether node has children
        /// </summary>
        public bool? HasChildren { get; set; }

        /// <summary>
        /// Whether currently subscribed
        /// </summary>
        public bool? IsPublished { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Type of node
        /// </summary>
        public string NodeClass { get; set; }

        /// <summary>
        /// User access level for node
        /// </summary>
        public string AccessLevel { get; set; }

        /// <summary>
        /// If eventing, event notifier to subscribe to.
        /// </summary>
        public string EventNotifier { get; set; }

        /// <summary>
        /// If method node class, whether method can be called.
        /// </summary>
        public bool? Executable { get; set; }

        /// <summary>
        /// If variable, datatype and value rank
        /// </summary>
        public string DataType { get; set; }
        public int? ValueRank { get; set; }
    }
}
