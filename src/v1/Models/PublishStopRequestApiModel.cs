// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;

    /// <summary>
    /// Unpublish request
    /// </summary>
    public class PublishStopRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishStopRequestApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishStopRequestApiModel(PublishStopRequestModel model) {
            NodeId = model.NodeId;
            Diagnostics = model.Diagnostics == null ? null :
                new DiagnosticsApiModel(model.Diagnostics);
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public PublishStopRequestModel ToServiceModel() {
            return new PublishStopRequestModel {
                NodeId = NodeId,
                Diagnostics = Diagnostics?.ToServiceModel()
            };
        }

        /// <summary>
        /// Node to unpublish
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Optional diagnostics configuration
        /// </summary>
        public DiagnosticsApiModel Diagnostics { get; set; }
    }
}
