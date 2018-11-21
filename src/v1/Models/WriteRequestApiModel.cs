// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Request node attribute write
    /// </summary>
    public class WriteRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public WriteRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public WriteRequestApiModel(WriteRequestModel model) {
            Attributes = model.Attributes?
                .Select(a => new AttributeWriteRequestApiModel(a)).ToList();
            Elevation = model.Elevation == null ? null :
                new CredentialApiModel(model.Elevation);
            Diagnostics = model.Diagnostics == null ? null :
                new DiagnosticsApiModel(model.Diagnostics);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public WriteRequestModel ToServiceModel() {
            return new WriteRequestModel {
                Attributes = Attributes?.Select(a => a.ToServiceModel()).ToList(),
                Diagnostics = Diagnostics?.ToServiceModel(),
                Elevation = Elevation?.ToServiceModel()
            };
        }

        /// <summary>
        /// Attributes to update
        /// </summary>
        public List<AttributeWriteRequestApiModel> Attributes { get; set; }

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
