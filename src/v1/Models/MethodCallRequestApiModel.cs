// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Call request model for module
    /// </summary>
    public class MethodCallRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public MethodCallRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public MethodCallRequestApiModel(MethodCallRequestModel model) {
            MethodId = model.MethodId;
            ObjectId = model.ObjectId;
            if (model.Arguments != null) {
                Arguments = model.Arguments
                    .Select(s => new MethodCallArgumentApiModel(s))
                    .ToList();
            }
            else {
                Arguments = new List<MethodCallArgumentApiModel>();
            }
            Elevation = model.Elevation == null ? null :
                new CredentialApiModel(model.Elevation);
            Diagnostics = model.Diagnostics == null ? null :
                new DiagnosticsApiModel(model.Diagnostics);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public MethodCallRequestModel ToServiceModel() {
            return new MethodCallRequestModel {
                MethodId = MethodId,
                ObjectId = ObjectId,
                Diagnostics = Diagnostics?.ToServiceModel(),
                Elevation = Elevation?.ToServiceModel(),
                Arguments = Arguments?
                    .Select(s => s.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Method to call
        /// </summary>
        public string MethodId { get; set; }

        /// <summary>
        /// Object scope of the method
        /// </summary>
        public string ObjectId { get; set; }

        /// <summary>
        /// Input Arguments
        /// </summary>
        public List<MethodCallArgumentApiModel> Arguments { get; set; }

        /// <summary>
        /// Elevation
        /// </summary>
        public CredentialApiModel Elevation { get; set; }

        /// <summary>
        /// Optional diagnostics configuration
        /// </summary>
        public DiagnosticsApiModel Diagnostics { get; set; }
    }
}
