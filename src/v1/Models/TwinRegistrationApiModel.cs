// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Twin registration model
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
            Id = model?.Id;
            Endpoint = model?.Endpoint == null ? null :
                new EndpointApiModel(model.Endpoint);
            AuthenticationMethods = model.AuthenticationMethods?
                .Select(p => new AuthenticationMethodApiModel(p)).ToList();
            Certificate = model?.Certificate;
            SiteId = model?.SiteId;
            SecurityLevel = model?.SecurityLevel;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public TwinRegistrationModel ToServiceModel() {
            return new TwinRegistrationModel {
                Id = Id,
                Endpoint = Endpoint?.ToServiceModel(),
                AuthenticationMethods = AuthenticationMethods?
                    .Select(p => p.ToServiceModel()).ToList(),
                SecurityLevel = SecurityLevel,
                SiteId = SiteId,
                Certificate = Certificate
            };
        }

        /// <summary>
        /// Identifier of the twin
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Registered site of the twin
        /// </summary>
        public string SiteId { get; set; }

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

        /// <summary>
        /// Supported authentication methods that can be selected to
        /// obtain a credential and used to interact with the endpoint.
        /// </summary>
        public List<AuthenticationMethodApiModel> AuthenticationMethods { get; set; }
    }
}
