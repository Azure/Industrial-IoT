// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;

    /// <summary>
    /// For manual discovery requests
    /// </summary>
    public class DiscoveryRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DiscoveryRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public DiscoveryRequestApiModel(DiscoveryRequestModel model) {
            Id = model.Id;
            Discovery = model.Discovery;
            Configuration = model.Configuration == null ? null :
                new DiscoveryConfigApiModel(model.Configuration);
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public DiscoveryRequestModel ToServiceModel() {
            return new DiscoveryRequestModel {
                Id = Id,
                Discovery = Discovery,
                Configuration = Configuration?.ToServiceModel()
            };
        }

        /// <summary>
        /// Id of discovery request
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Simple discovery mode
        /// </summary>
        public DiscoveryMode? Discovery { get; set; }

        /// <summary>
        /// Extended discovery configuration
        /// </summary>
        public DiscoveryConfigApiModel Configuration { get; set; }
    }
}
