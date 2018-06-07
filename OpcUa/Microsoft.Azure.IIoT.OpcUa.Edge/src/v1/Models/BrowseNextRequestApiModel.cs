// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge.Module.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;

    /// <summary>
    /// Request node browsing continuation
    /// </summary>
    public class BrowseNextRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public BrowseNextRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public BrowseNextRequestApiModel(BrowseNextRequestModel model) {
            Abort = model.Abort;
            ContinuationToken = model.ContinuationToken;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public BrowseNextRequestModel ToServiceModel() {
            return new BrowseNextRequestModel {
                Abort = Abort,
                ContinuationToken = ContinuationToken
            };
        }

        /// <summary>
        /// Continuation token to use
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Whether to abort browse and release
        /// </summary>
        public bool? Abort { get; set; }
    }
}
