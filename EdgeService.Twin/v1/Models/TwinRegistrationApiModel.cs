// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.EdgeService.v1.Models {
    using Microsoft.Azure.IIoT.OpcTwin.Services.Models;

    /// <summary>
    /// Twin model
    /// </summary>
    public class TwinRegistrationApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public TwinRegistrationApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public TwinRegistrationApiModel(TwinRegistrationModel model) {
            Endpoint = new EndpointApiModel(model?.Endpoint);
            Certificate = model?.Certificate;
            SecurityLevel = model?.SecurityLevel;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public TwinRegistrationModel ToServiceModel() {
            return new TwinRegistrationModel {
                Endpoint = Endpoint.ToServiceModel(),
                SecurityLevel = SecurityLevel,
                Certificate = Certificate
            };
        }

        /// <summary>
        /// Endpoint information in the registration
        /// </summary>
        public EndpointApiModel Endpoint { get; set; }

        /// <summary>
        /// Security assessment of the endpoint
        /// </summary>
        public int? SecurityLevel { get; set; }

        /// <summary>
        /// Endpoint cert
        /// </summary>
        public byte[] Certificate { get; set; }
    }
}
