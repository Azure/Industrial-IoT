// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;

    /// <summary>
    /// Method metadata request model for twin module
    /// </summary>
    public class MethodMetadataRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public MethodMetadataRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public MethodMetadataRequestApiModel(MethodMetadataRequestModel model) {
            MethodId = model.MethodId;
            Elevation = model.Elevation == null ? null :
                new AuthenticationApiModel(model.Elevation);
            Diagnostics = model.Diagnostics == null ? null :
               new DiagnosticsApiModel(model.Diagnostics);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public MethodMetadataRequestModel ToServiceModel() {
            return new MethodMetadataRequestModel {
                MethodId = MethodId,
                Diagnostics = Diagnostics?.ToServiceModel(),
                Elevation = Elevation?.ToServiceModel()
            };
        }

        /// <summary>
        /// Count of input arguments
        /// </summary>
        public string MethodId { get; set; }

        /// <summary>
        /// Optional User elevation
        /// </summary>
        public AuthenticationApiModel Elevation { get; set; }

        /// <summary>
        /// Optional diagnostics configuration
        /// </summary>
        public DiagnosticsApiModel Diagnostics { get; set; }
    }
}
