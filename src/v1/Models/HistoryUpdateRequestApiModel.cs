// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Request node history update
    /// </summary>
    public class HistoryUpdateRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public HistoryUpdateRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public HistoryUpdateRequestApiModel(HistoryUpdateRequestModel model) {
            Request = model.Request;
            Elevation = model.Elevation == null ? null :
                new CredentialApiModel(model.Elevation);
            Diagnostics = model.Diagnostics == null ? null :
                new DiagnosticsApiModel(model.Diagnostics);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public HistoryUpdateRequestModel ToServiceModel() {
            return new HistoryUpdateRequestModel {
                Request = Request,
                Diagnostics = Diagnostics?.ToServiceModel(),
                Elevation = Elevation?.ToServiceModel()
            };
        }

        /// <summary>
        /// The HistoryUpdateDetailsType extension object
        /// encoded in json and containing the tunneled
        /// update request for the Historian server.
        /// </summary>
        public JToken Request { get; set; }

        /// <summary>
        /// Optional User Elevation
        /// </summary>
        public CredentialApiModel Elevation { get; set; }

        /// <summary>
        /// Optional diagnostics configuration
        /// </summary>
        public DiagnosticsApiModel Diagnostics { get; set; }
    }
}
