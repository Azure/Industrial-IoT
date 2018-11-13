// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;

    /// <summary>
    /// Publish request
    /// </summary>
    public class PublishStartRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishStartRequestApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishStartRequestApiModel(PublishStartRequestModel model) {
            Node = model.Node == null ? null :
                new PublishedNodeApiModel(model.Node);
            Diagnostics = model.Diagnostics == null ? null :
                new DiagnosticsApiModel(model.Diagnostics);
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public PublishStartRequestModel ToServiceModel() {
            return new PublishStartRequestModel {
                Node = Node?.ToServiceModel(),
                Diagnostics = Diagnostics?.ToServiceModel()
            };
        }

        /// <summary>
        /// Node to publish
        /// </summary>
        public PublishedNodeApiModel Node { get; set; }

        /// <summary>
        /// Optional diagnostics configuration
        /// </summary>
        public DiagnosticsApiModel Diagnostics { get; set; }
    }
}
