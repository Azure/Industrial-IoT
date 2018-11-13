// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;

    /// <summary>
    /// Request list of published nodes
    /// </summary>
    public class PublishedNodeListRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishedNodeListRequestApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishedNodeListRequestApiModel(PublishedNodeListRequestModel model) {
            ContinuationToken = model.ContinuationToken;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public PublishedNodeListRequestModel ToServiceModel() {
            return new PublishedNodeListRequestModel {
                ContinuationToken = ContinuationToken
             };
        }

        /// <summary>
        /// Continuation token or null to start
        /// </summary>
        public string ContinuationToken { get; set; }
    }
}
