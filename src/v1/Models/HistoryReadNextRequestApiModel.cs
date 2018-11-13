// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;

    /// <summary>
    /// Request node history read continuation
    /// </summary>
    public class HistoryReadNextRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public HistoryReadNextRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public HistoryReadNextRequestApiModel(HistoryReadNextRequestModel model) {
            ContinuationToken = model.ContinuationToken;
            Abort = model.Abort;
            Elevation = model.Elevation == null ? null :
                new CredentialApiModel(model.Elevation);
            Diagnostics = model.Diagnostics == null ? null :
                new DiagnosticsApiModel(model.Diagnostics);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public HistoryReadNextRequestModel ToServiceModel() {
            return new HistoryReadNextRequestModel {
                ContinuationToken = ContinuationToken,
                Abort = Abort,
                Diagnostics = Diagnostics?.ToServiceModel(),
                Elevation = Elevation?.ToServiceModel()
            };
        }

        /// <summary>
        /// Continuation token to continue reading more
        /// results.
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Abort reading after this read
        /// </summary>
        public bool? Abort { get; set; }

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
