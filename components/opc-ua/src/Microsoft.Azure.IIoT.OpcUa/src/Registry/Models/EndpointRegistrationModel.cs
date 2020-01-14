// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint registration
    /// </summary>
    public class EndpointRegistrationModel {

        /// <summary>
        /// Endpoint identifier which is hashed from
        /// the supervisor, site and url.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The reported endpoint url
        /// </summary>
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Site of endpoint
        /// </summary>
        public string SiteId { get; set; }

        /// <summary>
        /// Supervisor that manages the endpoint.
        /// </summary>
        public string SupervisorId { get; set; }

        /// <summary>
        /// Discoverer that registered the endpoint
        /// </summary>
        public string DiscovererId { get; set; }

        /// <summary>
        /// Endpoint information in the registration
        /// </summary>
        public EndpointModel Endpoint { get; set; }

        /// <summary>
        /// Security level of the endpoint as advertised by server.
        /// </summary>
        public int? SecurityLevel { get; set; }

        /// <summary>
        /// Supported credential configurations that can be selected to
        /// obtain a credential and used to interact with the endpoint.
        /// </summary>
        public List<AuthenticationMethodModel> AuthenticationMethods { get; set; }
    }
}
