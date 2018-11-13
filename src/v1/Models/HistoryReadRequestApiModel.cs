// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Request node history read
    /// </summary>
    public class HistoryReadRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public HistoryReadRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public HistoryReadRequestApiModel(HistoryReadRequestModel model) {
            NodeId = model.NodeId;
            IndexRange = model.IndexRange;
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
        public HistoryReadRequestModel ToServiceModel() {
            return new HistoryReadRequestModel {
                NodeId = NodeId,
                IndexRange = IndexRange,
                Request = Request,
                Diagnostics = Diagnostics?.ToServiceModel(),
                Elevation = Elevation?.ToServiceModel()
            };
        }

        /// <summary>
        /// Node to read from (mandatory)
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// The HistoryReadDetailsType extension object
        /// encoded in json and containing the tunneled
        /// Historian reader request.
        /// </summary>
        public JToken Request { get; set; }

        /// <summary>
        /// Index range to read, e.g. 1:2,0:1 for 2 slices
        /// out of a matrix or 0:1 for the first item in
        /// an array, string or bytestring.
        /// See 7.22 of part 4: NumericRange.
        /// </summary>
        public string IndexRange { get; set; }

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
